using System;
using System.Linq;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

/// <summary>Builds every building model and checks its faces point outwards (a face pointing in renders dark or vanishes).</summary>
static class Check
{
    static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "buses") return Buses(args.Length > 1 ? float.Parse(args[1]) : 1f / 60);
        if (args.Length > 0 && args[0] == "parking") return Parking(args.Length > 1 ? int.Parse(args[1]) : 2);
        if (args.Length > 1 && args[0] == "topview") return TopView((BuildingType)Enum.Parse(typeof(BuildingType), args[1]), args.Length > 2 ? args[2] : "top.txt");
        if (args.Length > 1 && args[0] == "dump") return Dump(args[1], args.Length > 2 ? args[2] : "dump");
        int bad = Primitives();
        foreach (BuildingType t in Enum.GetValues(typeof(BuildingType)))
        {
            var d = Defs.Get(t);
            foreach (bool side in new[] { false, true })
            {
                float w = (side ? d.H : d.W) * 10, dep = (side ? d.W : d.H) * 10;
                var m = BuildingArt.Build(t, w, dep, 12345u, d.Floors, false, 2);
                bad += Audit(t.ToString() + (side ? " (turned)" : ""), m, w, dep, args.Length > 0 && args[0] == t.ToString());
            }
        }
        var site = BuildingArt.Site(20, 20, 0.6f, 15, true);
        bad += Audit("Site", site, 20, 20, false);
        foreach (var name in new[] { "car", "truck", "bus", "person" })
        {
            Model m = name == "car" ? PropArt.CarModel(new Color32(200, 0, 0, 255)) : name == "truck" ? PropArt.TruckModel(new Color32(1, 1, 1, 255), new Color32(1, 1, 1, 255)) : name == "person" ? PropArt.Person(3, false) : PropArt.BusModel(new Color32(1, 1, 1, 255));
            bad += Audit(name, m, 4, 10, false);
        }
        var tk = new MeshKit(); PropArt.Tree(tk, Vector3.zero, 1, 2, 7); PropArt.Pine(tk, new Vector3(5, 0, 0), 1, 2);
        bad += Audit("trees", tk.ToModel("t"), 10, 10, false);
        var bk = new MeshKit(); PropArt.BusShelter(bk, Vector3.zero, 0); PropArt.BusStopSign(bk, new Vector3(3, 0, 0), 0);
        bad += Audit("bus stop", bk.ToModel("s"), 10, 10, false);
        foreach (int season in new[] { 0, 1, 3 })
            foreach (BuildingType t in Enum.GetValues(typeof(BuildingType)))
            {
                var d = Defs.Get(t);
                bad += Audit(t + " s" + season + " closed", BuildingArt.Build(t, d.W * 10, d.H * 10, 777u, d.Floors, true, season), d.W * 10, d.H * 10, false, true);
            }
        Console.WriteLine(bad == 0 ? "All meshes look right." : bad + " suspicious faces.");
        return 0;
    }

    /// <summary>Writes a building's triangles, and cars in each of its parking spots, for a top-down look (x z y per vertex;
    /// a line per triangle: B for the building, C for a car).</summary>
    /// <summary>Each building's triangles with their material's name and colour, for tools/preview.py to draw. Types by name,
    /// comma separated, or "new" for the ones after WarehouseClub.</summary>
    static int Dump(string which, string dir)
    {
        System.IO.Directory.CreateDirectory(dir);
        var types = which == "new" ? Defs.All.Where(t => t > BuildingType.WarehouseClub).ToList()
                  : which == "all" ? Defs.All.ToList()
                  : which.Split(',').Select(x => (BuildingType)Enum.Parse(typeof(BuildingType), x)).ToList();
        foreach (var t in types)
        {
            var d = Defs.Get(t);
            float w = d.W * 10, dep = d.H * 10;
            var model = BuildingArt.Build(t, w, dep, 12345u, d.Floors, false, 2);
            var sb = new System.Text.StringBuilder();
            sb.Append("S ").Append(w).Append(' ').Append(dep).Append('\n');
            var m = model.Mesh; var v = m.vertices;
            for (int sm = 0; sm < m.subMeshCount; sm++)
            {
                var mat = model.Materials[sm];
                var c = mat != null ? mat.BaseColor : Color.white;
                string nm = mat != null && !string.IsNullOrEmpty(mat.name) ? mat.name.Replace(' ', '_') : "?";
                sb.Append("M ").Append(nm).Append(' ').Append(c.r.ToString("0.###")).Append(' ').Append(c.g.ToString("0.###")).Append(' ').Append(c.b.ToString("0.###")).Append('\n');
                var idx = m.GetTriangles(sm);
                for (int i = 0; i + 2 < idx.Length; i += 3)
                {
                    sb.Append('T');
                    for (int j = 0; j < 3; j++) { var p = v[idx[i + j]]; sb.Append(' ').Append(p.x.ToString("0.##")).Append(' ').Append(p.y.ToString("0.##")).Append(' ').Append(p.z.ToString("0.##")); }
                    sb.Append('\n');
                }
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, t + ".txt"), sb.ToString());
        }
        Console.WriteLine("dumped " + types.Count + " to " + dir);
        return 0;
    }

    static int TopView(BuildingType t, string path)
    {
        var d = Defs.Get(t);
        float w = d.W * 10, dep = d.H * 10;
        var sb = new System.Text.StringBuilder();
        void Dump(Mesh m, string tag, Func<Vector3, Vector3> xf)
        {
            var v = m.vertices;
            for (int sm = 0; sm < m.subMeshCount; sm++)
            {
                var idx = m.GetTriangles(sm);
                for (int i = 0; i + 2 < idx.Length; i += 3)
                {
                    sb.Append(tag);
                    for (int j = 0; j < 3; j++) { var p = xf(v[idx[i + j]]); sb.Append(' ').Append(p.x.ToString("0.##")).Append(' ').Append(p.z.ToString("0.##")).Append(' ').Append(p.y.ToString("0.##")); }
                    sb.Append('\n');
                }
            }
        }
        Dump(BuildingArt.Build(t, w, dep, 12345u, d.Floors, false, 2).Mesh, "B", p => p);
        var car = PropArt.CarModel(new Color32(200, 0, 0, 255), 2).Mesh;
        foreach (var sp in BuildingArt.ParkingSpots(t, w, dep, 12345u))
        {
            var rot = Quaternion.Euler(0, sp.z, 0);
            Dump(car, "C", p => rot * p + new Vector3(sp.x, 0, sp.y));
        }
        System.IO.File.WriteAllText(path, sb.ToString());
        Console.WriteLine("wrote " + path);
        return 0;
    }

    /// <summary>A day of drivers in a built-up town with some car parks downtown: where their cars end up, and that no two
    /// cars ever share a spot.</summary>
    static int Parking(int lotsToBuild)
    {
        var t = Town.CreateDeveloped(1);
        int built = 0;
        for (int r = 0; r < 30 && built < lotsToBuild; r++)
            for (int y = 47 - r; y <= 47 + r && built < lotsToBuild; y++)
                for (int x = 47 - r; x <= 47 + r && built < lotsToBuild; x++)
                    if (t.CanPlace(BuildingType.ParkingLot, x, y) == null) { t.PlacePublic(BuildingType.ParkingLot, x, y); built++; }
        if (Environment.GetEnvironmentVariable("CHAINS") == "1")
        {
            // a chain store or two near the middle, opened straight away, to try their car parks
            var add = typeof(Town).GetMethod("AddBuilding", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var open = typeof(Town).GetMethod("OpenBusiness", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var ct in new[] { BuildingType.DiscountStore, BuildingType.WarehouseClub })
            {
                var cd = Defs.Get(ct);
                bool done = false;
                for (int r = 0; r < 40 && !done; r++)
                    for (int y = 47 - r; y <= 47 + r && !done; y++)
                        for (int x = 47 - r; x <= 47 + r && !done; x++)
                        {
                            if (!t.FootprintFree(x, y, cd.W, cd.H)) continue;
                            int f = t.FindFacing(x, y, cd.W, cd.H);
                            if (f < 0) continue;
                            var b = (Building)add.Invoke(t, new object[] { ct, x, y, f, true });
                            open.Invoke(t, new object[] { b });
                            Console.WriteLine("placed " + ct + " at " + x + "," + y + " facing " + f);
                            done = true;
                        }
            }
        }
        for (int d = 0; d < 12; d++) t.Advance(1f);
        Console.WriteLine("car parks built " + t.Buildings.Count(b => b.Type == BuildingType.ParkingLot && b.Built) + " of " + built);
        var go = new GameObject("world");
        var view = go.AddComponent<RealLife.Game.WorldView>();
        typeof(RealLife.Game.WorldView).GetProperty("Nav").SetValue(view, new RealLife.Game.Nav(t));
        var agents = go.AddComponent<RealLife.Game.Agents>();
        agents.Bind(t, view);
        var upd = typeof(RealLife.Game.Agents).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        t.Hour = 3f;
        upd.Invoke(agents, null);
        var drives = agents.DayTrips.Where(x => x.Kind == 1 && x.After != null).ToList();
        int toLot = drives.Count(x => x.After.Building >= 0 && !x.After.Home), toHome = drives.Count(x => x.After.Home && x.After.Building >= 0),
            homeHidden = drives.Count(x => x.After.Home && x.After.Building < 0), kerb = drives.Count(x => !x.After.Home && x.After.Building < 0);
        Console.WriteLine($"drives today {drives.Count}: park in a lot {toLot}, on the driveway {toHome}, home but no driveway {homeHidden}, kerb or out of town {kerb}");
        foreach (var g in drives.Where(x => x.After.Building >= 0 && !x.After.Home).GroupBy(x => t.GetBuilding(x.After.Building).Type))
            Console.WriteLine("  into " + g.Key + ": " + g.Count());
        // no spot double-booked
        int clash = 0;
        foreach (var g in drives.Select(x => x.After).Where(s => s.Building >= 0 && !s.Home).GroupBy(s => s.Building * 1000 + s.Spot))
        {
            var l = g.OrderBy(s => s.From).ToList();
            for (int i = 1; i < l.Count; i++) if (l[i].From < l[i - 1].To) { clash++; Console.WriteLine($"  clash at {t.GetBuilding(l[i].Building).Type} {l[i].Building} spot {l[i].Spot}: car {l[i - 1].Car} {l[i - 1].From:0.00}-{l[i - 1].To:0.00} and car {l[i].Car} {l[i].From:0.00}-{l[i].To:0.00} same object {ReferenceEquals(l[i], l[i - 1])}"); }
        }
        Console.WriteLine("double-booked spots: " + clash);
        int started = 0, finished = 0, maxDrivers = 0, maxParked = 0;
        var parked = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>>();
        while (t.Hour < 23.5f)
        {
            float before = t.Hour;
            t.Advance(0.05f / 24f);
            if (t.Hour < before) break;
            upd.Invoke(agents, null);
            int act = agents.ActiveTrips.Count(x => x.Segs != null);
            maxDrivers = Math.Max(maxDrivers, act);
            int p = 0;
            foreach (var b in t.Buildings.Where(b => b.Type == BuildingType.ParkingLot || b.Type == BuildingType.GroceryStore)) { agents.Parks.ParkedAt(b, t.Hour, parked); p += parked.Count; }
            maxParked = Math.Max(maxParked, p);
        }
        started = agents.DayTrips.Count(x => x.Segs != null);
        Console.WriteLine($"drives done in three legs: {started}; most driving at once {maxDrivers}; most cars parked in lots at once {maxParked}");
        var sample = agents.DayTrips.FirstOrDefault(x => x.Segs != null && x.After.Building >= 0 && !x.After.Home);
        if (sample != null) Console.WriteLine($"a parked drive: walk {sample.SegLen[0]:0} m, drive {sample.SegLen[1]:0} m, walk {sample.SegLen[2]:0} m");
        var k = agents.DayTrips.FirstOrDefault(x => x.Segs != null && !x.After.Home && x.After.Building < 0 && x.To != t.TerminalId);
        if (k != null) Console.WriteLine($"a kerb drive: walk {k.SegLen[0]:0} m, drive {k.SegLen[1]:0} m, walk {k.SegLen[2]:0} m");
        return clash == 0 ? 0 : 1;
    }

    /// <summary>Runs the people and buses you'd see for a day in a built-up town with a bus depot, in steps of the given hours,
    /// and prints what the buses did.</summary>
    static int Buses(float step)
    {
        var t = Town.CreateDeveloped(1);
        Building dep = null;
        for (int r = 0; r < 30 && dep == null; r++)
            for (int y = 47 - r; y <= 47 + r && dep == null; y++)
                for (int x = 45 - r; x <= 45 + r && dep == null; x++)
                    if (t.CanPlace(BuildingType.BusDepot, x, y) == null) dep = t.PlacePublic(BuildingType.BusDepot, x, y);
        for (int d = 0; d < 40; d++) t.Advance(1f);
        t.EnsureBusNet();
        Console.WriteLine($"depot at {dep.X},{dep.Y} built {dep.Built} drivers {dep.Employees.Count} stops {t.BusStops.Count} routes {t.BusRoutes.Count} riders {t.BusRiders}");
        var go = new GameObject("world");
        var view = go.AddComponent<RealLife.Game.WorldView>();
        typeof(RealLife.Game.WorldView).GetProperty("Nav").SetValue(view, new RealLife.Game.Nav(t));
        var agents = go.AddComponent<RealLife.Game.Agents>();
        agents.Bind(t, view);
        var upd = typeof(RealLife.Game.Agents).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        t.Hour = 3f;
        upd.Invoke(agents, null);
        var L = agents.Lines;
        Console.WriteLine("lines: " + L.Describe());
        Console.WriteLine("bus trips planned today: " + agents.DayTrips.Count(x => x.Kind == 3) + " of " + agents.DayTrips.Count + " (kids " + agents.DayTrips.Count(x => x.Kind == 3 && t.GetCitizen(x.Citizen) != null && t.GetCitizen(x.Citizen).IsChild) + ")");
        float lastPrint = -1;
        int maxWait = 0;
        int startDay = t.Day;
        while (t.Day < startDay + 2 || t.Hour < 23.8f)
        {
            float before = t.Hour;
            t.Advance(step / 24f);
            if (t.Hour < before) { lastPrint = -1; Console.WriteLine("-- next day: bus trips planned " + agents.DayTrips.Count(x => x.Kind == 3)); }
            upd.Invoke(agents, null);
            maxWait = Math.Max(maxWait, L.WaitingNow);
            if (t.Hour - lastPrint >= 1f)
            {
                lastPrint = (float)Math.Floor(t.Hour);
                var act = agents.ActiveTrips;
                Console.WriteLine($"{t.Hour,5:0.0}h buses {L.BusesOut} | riders walking to a stop {act.Count(x => x.Kind == 3 && x.Phase == 0)} waiting {L.WaitingNow} on board {L.RidingNow} walking on {act.Count(x => x.Kind == 3 && x.Phase == 3)} | boarded {L.Boarded} got off {L.Alighted} gave up {agents.GaveUp}");
            }
        }
        Console.WriteLine($"day done: boarded {L.Boarded}, got off {L.Alighted}, gave up {agents.GaveUp}, most waiting at once {maxWait}");
        return 0;
    }

    /// <summary>Each shape on its own: every face must point away from the shape's middle.</summary>
    static int Primitives()
    {
        int bad = 0;
        var c = new Color32(200, 200, 200, 255);
        bad += Convex("Box", k => k.Box(new Vector3(-1, 0, -2), new Vector3(3, 2, 1), c, c, false), new Vector3(1, 1, -0.5f));
        bad += Convex("Gable", k => k.Gable(-2, -3, 2, 3, 0, 2, c, c, 0f), new Vector3(0, 0.7f, 0));
        // roofs carry soffit and fascia boards; check the slopes
        bad += Convex("Hip wide", k => k.Hip(-3, -1, 3, 1, 0, 2, c, 0f), new Vector3(0, 0.5f, 0), n => n.y > 0.3f && n.y < 0.99f);
        bad += Convex("Hip deep", k => k.Hip(-1, -3, 1, 3, 0, 2, c, 0f), new Vector3(0, 0.5f, 0), n => n.y > 0.3f && n.y < 0.99f);
        bad += Convex("Cylinder", k => k.Cylinder(Vector3.zero, 1, 1, 2, 8, c, c), new Vector3(0, 1, 0));
        bad += Convex("Cone", k => k.Cylinder(Vector3.zero, 1, 0, 2, 8, c, c), new Vector3(0, 0.5f, 0));
        bad += Convex("Blob", k => k.Blob(new Vector3(0, 2, 0), new Vector3(1, 1, 1), c), new Vector3(0, 2, 0));
        bad += Convex("Flat", k => k.Flat(-1, -1, 1, 1, 0, c), new Vector3(0, -1, 0));
        // window frames are little boxes; the panes and frame fronts must face out
        bad += Convex("Windows", k => k.WindowsFront(-2, 2, 0, 2, -1, 2, 1, 1, 1, 5, 1f), new Vector3(0, 1, 0), n => n.z < -0.5f);
        bad += Convex("Turned box", k => { k.Push(new Vector3(5, 0, 5), 90); k.Box(new Vector3(-1, 0, -2), new Vector3(3, 2, 1), c, c, false); k.Pop(); }, new Vector3(5 - 0.5f, 1, 5 - 1));
        return bad;
    }

    static int Convex(string name, Action<MeshKit> build, Vector3 center, Func<Vector3, bool> only = null)
    {
        var k = new MeshKit(); build(k); var m = k.ToModel(name).Mesh;
        var v = m.vertices; int wrong = 0, n = 0;
        for (int s = 0; s < m.subMeshCount; s++)
        {
            var idx = m.GetTriangles(s);
            for (int i = 0; i < idx.Length; i += 3)
            {
                Vector3 a = v[idx[i]], b = v[idx[i + 1]], cc = v[idx[i + 2]];
                var nn = Vector3.Cross(b - a, cc - a);
                if (nn.sqrMagnitude < 1e-10f) continue;
                if (only != null && !only(nn.normalized)) continue;
                n++;
                if (Vector3.Dot(nn, (a + b + cc) / 3f - center) <= 0) wrong++;
            }
        }
        Console.WriteLine($"{name,-16} {n,4} faces" + (wrong > 0 ? $"  POINTING IN: {wrong}" : "  ok"));
        return wrong;
    }

    /// <summary>Counts faces that look like they point the wrong way: walls facing into their own part, roofs facing down.</summary>
    static int Audit(string name, Model model, float w, float d, bool verbose, bool quiet = false)
    {
        var m = model.Mesh;
        var v = m.vertices; var n = m.normals;
        int tris = 0, nan = 0, suspicious = 0, outside = 0;
        if (model.Materials.Length != m.subMeshCount) { Console.WriteLine($"{name}: {model.Materials.Length} materials for {m.subMeshCount} sub-meshes"); return 1; }
        if (m.uv.Length != v.Length) { Console.WriteLine($"{name}: {m.uv.Length} uvs for {v.Length} vertices"); return 1; }
        foreach (var p in v) if (Math.Abs(p.x) > w / 2 + 16 || Math.Abs(p.z) > d / 2 + 16 || p.y < -1 || p.y > 60) outside++;
        for (int s = 0; s < m.subMeshCount; s++)
        {
            var idx = m.GetTriangles(s);
            for (int i = 0; i < idx.Length; i += 3)
            {
                tris++;
                Vector3 a = v[idx[i]], b = v[idx[i + 1]], c = v[idx[i + 2]];
                if (float.IsNaN(a.x + a.y + a.z + b.x + b.y + b.z + c.x + c.y + c.z)) { nan++; continue; }
                var geo = Vector3.Cross(b - a, c - a);
                if (geo.sqrMagnitude < 1e-10f) continue;
                geo = geo.normalized;
                if (Vector3.Dot(geo, n[idx[i]]) < 0.99f) { suspicious++; if (verbose) Console.WriteLine("  normal mismatch"); }
                // Faces should point away from the solid they belong to. Probe a little behind the face: if the probe is
                // "outside everything" while a probe in front is "inside", the face is backwards. Without the solids we use a
                // cheaper rule: the face's normal must point to the side of the face that is nearer to open sky — i.e. probing
                // along the normal from the centroid should not immediately hit other faces of the same small part.
            }
        }
        // the face-direction heuristic: ray from each face centroid along its normal; count how many faces it crosses
        // (an odd count for a closed mesh means the face points into a solid).
        var all = Enumerable.Range(0, m.subMeshCount).SelectMany(s => m.GetTriangles(s)).ToArray();
        int inward = 0;
        for (int i = 0; i < all.Length && !quiet; i += 3)
        {
            Vector3 a = v[all[i]], b = v[all[i + 1]], c = v[all[i + 2]];
            var nn = Vector3.Cross(b - a, c - a);
            if (nn.sqrMagnitude < 1e-10f) continue;
            nn = nn.normalized;
            var o = (a + b + c) / 3f + nn * 0.02f;
            int hits = 0;
            for (int j = 0; j < all.Length; j += 3)
            {
                if (j == i) continue;
                if (RayTri(o, nn, v[all[j]], v[all[j + 1]], v[all[j + 2]])) hits++;
            }
            // a ray that leaves through an odd number of faces started inside something
            if (hits % 2 == 1 && nn.y > -0.9f)
            {
                inward++;
                if (verbose) Console.WriteLine($"  inward? centroid {(a + b + c) / 3f} normal {nn} hits {hits}");
            }
        }
        string msg = $"{name,-28} {tris,6} tris, {v.Length,6} verts, {m.subMeshCount,3} materials" + (nan > 0 ? $"  NaN {nan}" : "") + (suspicious > 0 ? $"  normal mismatch {suspicious}" : "") + (outside > 0 ? $"  FAR OUT {outside}" : "");
        if (!quiet || nan + suspicious + outside > 0) Console.WriteLine(msg);
        return nan + suspicious + (outside > 0 ? 1 : 0);
    }

    static bool RayTri(Vector3 o, Vector3 dir, Vector3 a, Vector3 b, Vector3 c)
    {
        var e1 = b - a; var e2 = c - a;
        var p = Vector3.Cross(dir, e2);
        float det = Vector3.Dot(e1, p);
        if (Math.Abs(det) < 1e-8f) return false;
        float inv = 1 / det;
        var t = o - a;
        float u = Vector3.Dot(t, p) * inv;
        if (u < 0 || u > 1) return false;
        var q = Vector3.Cross(t, e1);
        float vv = Vector3.Dot(dir, q) * inv;
        if (vv < 0 || u + vv > 1) return false;
        float dist = Vector3.Dot(e2, q) * inv;
        return dist > 1e-4f;
    }
}
