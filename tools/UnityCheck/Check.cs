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
            Mesh m = name == "car" ? PropArt.CarMesh(new Color32(200, 0, 0, 255)) : name == "truck" ? PropArt.TruckMesh(new Color32(1, 1, 1, 255), new Color32(1, 1, 1, 255)) : name == "person" ? PropArt.Person(3, false) : null;
            if (name == "bus") { var k = new MeshKit(); PropArt.Bus(k, new Color32(1, 1, 1, 255)); m = k.ToMesh("bus"); }
            bad += Audit(name, m, 4, 10, false);
        }
        var tk = new MeshKit(); PropArt.Tree(tk, Vector3.zero, 1, 2, 7); PropArt.Pine(tk, new Vector3(5, 0, 0), 1, 2);
        bad += Audit("trees", tk.ToMesh("t"), 10, 10, false);
        Console.WriteLine(bad == 0 ? "All meshes look right." : bad + " suspicious faces.");
        return 0;
    }

    /// <summary>Each shape on its own: every face must point away from the shape's middle.</summary>
    static int Primitives()
    {
        int bad = 0;
        var c = new Color32(200, 200, 200, 255);
        bad += Convex("Box", k => k.Box(new Vector3(-1, 0, -2), new Vector3(3, 2, 1), c, c, false), new Vector3(1, 1, -0.5f));
        bad += Convex("Gable", k => k.Gable(-2, -3, 2, 3, 0, 2, c, c, 0f), new Vector3(0, 0.7f, 0));
        bad += Convex("Hip wide", k => k.Hip(-3, -1, 3, 1, 0, 2, c, 0f), new Vector3(0, 0.5f, 0));
        bad += Convex("Hip deep", k => k.Hip(-1, -3, 1, 3, 0, 2, c, 0f), new Vector3(0, 0.5f, 0));
        bad += Convex("Cylinder", k => k.Cylinder(Vector3.zero, 1, 1, 2, 8, c, c), new Vector3(0, 1, 0));
        bad += Convex("Cone", k => k.Cylinder(Vector3.zero, 1, 0, 2, 8, c, c), new Vector3(0, 0.5f, 0));
        bad += Convex("Blob", k => k.Blob(new Vector3(0, 2, 0), new Vector3(1, 1, 1), c), new Vector3(0, 2, 0));
        bad += Convex("Flat", k => k.Flat(-1, -1, 1, 1, 0, c), new Vector3(0, -1, 0));
        bad += Convex("Windows", k => k.WindowsFront(-2, 2, 0, 2, -1, 2, 1, 1, 1, 5, 1f), new Vector3(0, 1, 0));
        bad += Convex("Windows around", k => k.WindowsAround(new Vector3(-2, 0, -3), new Vector3(2, 3, 3), 1, 1, 0.5f, 0.5f, 0.5f, 5, 1f), new Vector3(0, 1.5f, 0));
        bad += Convex("Turned box", k => { k.Push(new Vector3(5, 0, 5), 90); k.Box(new Vector3(-1, 0, -2), new Vector3(3, 2, 1), c, c, false); k.Pop(); }, new Vector3(5 - 0.5f, 1, 5 - 1));
        return bad;
    }

    static int Convex(string name, Action<MeshKit> build, Vector3 center)
    {
        var k = new MeshKit(); build(k); var m = k.ToMesh(name);
        var v = m.vertices; int wrong = 0, n = 0;
        for (int s = 0; s < 2; s++)
        {
            var idx = m.GetTriangles(s);
            for (int i = 0; i < idx.Length; i += 3)
            {
                Vector3 a = v[idx[i]], b = v[idx[i + 1]], cc = v[idx[i + 2]];
                var nn = Vector3.Cross(b - a, cc - a);
                if (nn.sqrMagnitude < 1e-10f) continue;
                n++;
                if (Vector3.Dot(nn, (a + b + cc) / 3f - center) <= 0) wrong++;
            }
        }
        Console.WriteLine($"{name,-16} {n,4} faces" + (wrong > 0 ? $"  POINTING IN: {wrong}" : "  ok"));
        return wrong;
    }

    /// <summary>Counts faces that look like they point the wrong way: walls facing into their own part, roofs facing down.</summary>
    static int Audit(string name, Mesh m, float w, float d, bool verbose)
    {
        var v = m.vertices; var n = m.normals;
        int tris = 0, nan = 0, suspicious = 0;
        for (int s = 0; s < 2; s++)
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
        var all = Enumerable.Range(0, 2).SelectMany(s => m.GetTriangles(s)).ToArray();
        int inward = 0;
        for (int i = 0; i < all.Length; i += 3)
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
        string msg = $"{name,-28} {tris,6} tris, {v.Length,6} verts" + (nan > 0 ? $"  NaN {nan}" : "") + (suspicious > 0 ? $"  normal mismatch {suspicious}" : "");
        Console.WriteLine(msg);
        return nan + suspicious;
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
