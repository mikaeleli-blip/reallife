using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>
    /// Parts for the many kinds of business added later (BuildingArt.Industry, .Shops, .Food, .Services): shopfronts, sheds,
    /// barns, silos, tanks, stacks, piles, fences, pylon signs, neon, little props. Each kind's model puts them together its own
    /// way. All in the building's own frame: the lot is w by d metres centred on the origin, the street side along -z.
    /// </summary>
    public static partial class BuildingArt
    {
        // ================================================================== materials
        static Material Glass(bool closed) { return closed ? MaterialLibrary.Dark("gf_store_dark", new Color(0.6f, 0.6f, 0.6f)) : S("gf_store_light", 1.8f); }
        static Material Lit(Color32 c, float intensity = 2.2f) { return MaterialLibrary.Glow((Color)c * 0.85f, (Color)c, intensity); }
        static Material Snowy(Material m, int season) { return season == 0 ? S("snow") : m; }

        // ================================================================== buildings
        /// <summary>A one-storey shop: a shopfront of glass across most of the street side, a sign over it, plain walls round
        /// the rest and a flat roof with a coping in the trim colour. Returns the street face.</summary>
        static Face Storefront(MeshKit k, float x0, float x1, float z0, float z1, float h, Material wall, string ground, bool closed, int season, int signRow,
                               Material trim = null, float g0 = 0.08f, float g1 = 0.92f, float signW = -1f, float glassH = 3.6f, bool backside = true)
        {
            var faces = Faces(x0, x1, z0, z1);
            var front = faces[South];
            float W = front.Width, a0 = W * g0, a1 = W * g1;
            var glass = closed ? MaterialLibrary.Dark(ground, new Color(0.65f, 0.65f, 0.65f)) : S(ground, 1.8f);
            Rect(k, front, a0, a1, 0, glassH, glass, new Vector2(0, 0), new Vector2(Mathf.Max(1, Mathf.Round((a1 - a0) / 2.7f)) * 0.25f, 1));
            if (a0 > 0.01f) k.Quad(front.At(0, 0), front.At(0, h), front.At(a0, h), front.At(a0, 0), wall, 3f);
            if (a1 < W - 0.01f) k.Quad(front.At(a1, 0), front.At(a1, h), front.At(W, h), front.At(W, 0), wall, 3f);
            k.Quad(front.At(a0, glassH), front.At(a0, h), front.At(a1, h), front.At(a1, glassH), wall, 3f);
            for (int i = 0; i < 4; i++) if (i != South) Plain(k, faces[i], 0, h, wall, 2.5f);
            // a cornice band round the top and pilasters at the front corners give the box a face
            if (backside && h > 3.8f)
            {
                var band = C(H("#d9d3c7"), 0.3f);
                for (int i = 0; i < 4; i++) Ledge(k, faces[i], -0.06f, faces[i].Width + 0.06f, h - 0.5f, h - 0.22f, 0.1f, band);
                if (a0 > 0.45f) Ledge(k, front, 0, 0.38f, 0, h - 0.5f, 0.08f, wall);
                if (a1 < W - 0.45f) Ledge(k, front, W - 0.38f, W, 0, h - 0.5f, 0.08f, wall);
            }
            trim = trim ?? C(H("#d8d4ca"), 0.3f);
            FlatRoof(k, x0, x1, z0, z1, h, Snowy(S("roof_membrane"), season), wall, trim, 0.5f);
            float sw = signW > 0 ? signW : Mathf.Min(W - 1.2f, 7.5f);
            if (signRow >= 0) Sign(k, front, W / 2, sw, glassH + 0.25f, Mathf.Min(h - glassH - 0.45f, sw / 9f), signRow, closed);
            if (closed)
            {
                var boards = T("wood", new Color(0.75f, 0.65f, 0.52f));
                for (float a = a0 + 0.4f; a < a1 - 1f; a += 2.6f) Ledge(k, front, a, a + 2.0f, 0.5f, glassH - 0.4f, 0.06f, boards);
            }
            if (backside)
            {
                uint ls = Seed(x0, z1, h);
                Backside(k, faces, h, ref ls, closed);
                BackRoof(k, x0, x1, z1, h, ref ls);
            }
            return front;
        }

        static uint Seed(float a, float b, float c) { return (uint)(Mathf.Abs(a * 73.1f + b * 131.7f + c * 977.3f) * 16f) | 1u; }

        // ================================================================== backs, sides and roofs
        /// <summary>What goes round the back of a plain-walled building, so it isn't a blank box from behind: on the back
        /// wall a service door with a lamp over it, a window or two, a vent and downspouts at the corners; small windows
        /// along the sides, clear of the front corners. A tall building (a plant hall) gets a roller door and a strip of
        /// high windows instead.</summary>
        static void Backside(MeshKit k, Face[] faces, float h, ref uint s, bool closed, bool sides = true, int style = 0)
        {
            var back = faces[North];
            float W = back.Width;
            if (W < 3 || h < 2.6f) return;
            // style: 0 judged by height, 1 a shop, 2 a plant hall, 3 a building of storeys (offices)
            bool big = style == 2 || (style == 0 && h > 6.5f);
            if (style == 3)
            {
                OfficeBack(k, faces, h, ref s, closed, sides);
                return;
            }
            var steel = C(H("#5f666e"), 0.35f, 0.4f);
            var pipe = C(H("#8d949b"), 0.35f, 0.5f);
            // downspouts at the back corners
            Ledge(k, back, 0.15f, 0.3f, 0, h, 0.14f, pipe);
            Ledge(k, back, W - 0.3f, W - 0.15f, 0, h, 0.14f, pipe);
            // the service door with a lamp over it
            float da = Mathf.Clamp(W * Range(ref s, 0.16f, 0.3f), 1.2f, W - 1.2f);
            Door(k, back, da, H("#6b7178"), 1.0f, 2.2f);
            Ledge(k, back, da - 0.2f, da + 0.2f, 2.55f, 2.72f, 0.24f, steel);
            Ledge(k, back, da - 0.12f, da + 0.12f, 2.49f, 2.55f, 0.2f, closed ? C(H("#55585c")) : Lit(H("#ffe2a8"), 2.4f));
            if (big)
            {
                if (W > 11)
                {
                    float ra = Mathf.Min(W - 3f, da + Range(ref s, 4.5f, 7f));
                    var roll = closed ? MaterialLibrary.Dark("gf_rollup", new Color(0.75f, 0.75f, 0.75f)) : S("gf_rollup", 1.2f);
                    Rect(k, back, ra - 1.7f, ra + 1.7f, 0, 4.2f, roll, new Vector2(0.5f, 0), new Vector2(1f, 1), 0.03f);
                    Ledge(k, back, ra - 1.85f, ra + 1.85f, 4.2f, 4.4f, 0.12f, steel);
                }
                WindowRow(k, back, 1.2f, W - 1.2f, Mathf.Min(h - 1.8f, 5.2f), 2.2f, 0.9f, 3.6f, ref s, closed);
            }
            else
            {
                if (W - da > 4.2f) WindowRow(k, back, da + 1.6f, Mathf.Min(W - 1.0f, da + 1.6f + 2.6f * 2), 1.2f, 1.0f, 1.2f, 2.6f, ref s, closed);
                // an exhaust vent up by the roof
                float va = Mathf.Lerp(da + 1.5f, W - 1.0f, R(ref s));
                if (h > 3.8f && va < W - 0.8f)
                {
                    Ledge(k, back, va - 0.4f, va + 0.4f, h - 1.3f, h - 0.6f, 0.08f, steel);
                    for (float y = h - 1.2f; y < h - 0.65f; y += 0.15f) Ledge(k, back, va - 0.34f, va + 0.34f, y, y + 0.05f, 0.11f, pipe);
                }
            }
            if (!sides) return;
            for (int i = 2; i < 4; i++)
            {
                var f = faces[i];
                // east runs back from the street corner, west toward it: keep the windows off the front corner
                float a0 = i == East ? 3.2f : 1.2f, a1 = i == East ? f.Width - 1.2f : f.Width - 3.2f;
                if (a1 - a0 < 2.2f) continue;
                if (big) WindowRow(k, f, a0, a1, Mathf.Min(h - 1.8f, 5.2f), 2.2f, 0.9f, 3.6f, ref s, closed);
                else WindowRow(k, f, a0, a1, 1.3f, 1.0f, 1.2f, Mathf.Max(2.6f, (a1 - a0) / 2), ref s, closed);
            }
        }

        /// <summary>The back and sides of a building of storeys: a window to each bay on every floor, a back door, downspouts.</summary>
        static void OfficeBack(MeshKit k, Face[] faces, float h, ref uint s, bool closed, bool sides)
        {
            var pipe = C(H("#8d949b"), 0.35f, 0.5f);
            int floors = Mathf.Max(1, Mathf.RoundToInt(h / 3.6f));
            float fh = h / floors;
            for (int i = 0; i < 4; i++)
            {
                if (i == South || (!sides && i != North)) continue;
                var f = faces[i];
                if (f.Width < 3) continue;
                for (int fl = 0; fl < floors; fl++)
                {
                    float y = fl * fh + Mathf.Min(1.0f, fh * 0.3f);
                    if (i == North && fl == 0) WindowRow(k, f, 2.6f, f.Width - 0.6f, y, 1.1f, Mathf.Min(1.6f, fh - 1.5f), 2.4f, ref s, closed);
                    else WindowRow(k, f, 0.6f, f.Width - 0.6f, y, 1.1f, Mathf.Min(1.6f, fh - 1.5f), 2.4f, ref s, closed);
                }
            }
            Door(k, faces[North], 1.4f, H("#6b7178"), 1.0f, 2.2f);
            Ledge(k, faces[North], 0.15f, 0.3f, 0, h, 0.14f, pipe);
            Ledge(k, faces[North], faces[North].Width - 0.3f, faces[North].Width - 0.15f, 0, h, 0.14f, pipe);
        }

        /// <summary>A small flat roof's plant, kept to the back so it clears anything stood on the front of the roof: a
        /// condenser or two, a vent pipe and a roof hatch.</summary>
        static void BackRoof(MeshKit k, float x0, float x1, float z1, float y, ref uint s)
        {
            float span = x1 - x0;
            if (span < 4) return;
            var metal = C(H("#babec2"), 0.45f, 0.6f); var dark = C(H("#34373c"), 0.3f, 0.4f);
            float zc = z1 - 1.25f;
            int n = span > 12 ? 2 : 1;
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(x0 + 1.4f, x1 - 1.4f, n == 1 ? Range(ref s, 0.55f, 0.85f) : (i + 0.5f) / n);
                k.Box(new Vector3(x - 0.75f, y, zc - 0.5f), new Vector3(x + 0.75f, y + 0.85f, zc + 0.5f), metal, metal);
                k.Cylinder(new Vector3(x + 0.32f, y + 0.85f, zc), 0.34f, 0.34f, 0.05f, 10, dark, dark);
                k.Box(new Vector3(x - 0.7f, y + 0.2f, zc - 0.52f), new Vector3(x - 0.08f, y + 0.72f, zc - 0.5f), dark, dark);
            }
            float vx = Mathf.Lerp(x0 + 1.0f, x0 + span * 0.35f, R(ref s));
            k.Cylinder(new Vector3(vx, y, zc + 0.2f), 0.12f, 0.12f, 0.9f, 8, metal, metal);
            k.Cylinder(new Vector3(vx, y + 0.9f, zc + 0.2f), 0.2f, 0.0f, 0.18f, 8, metal, metal);
            k.Box(new Vector3(vx + 0.6f, y, zc - 0.4f), new Vector3(vx + 1.5f, y + 0.35f, zc + 0.5f), C(H("#9aa0a6"), 0.3f), C(H("#7d838a"), 0.3f));
        }

        /// <summary>Walls and a gable roof (ridge along x). Returns the faces.</summary>
        static Face[] GableBox(MeshKit k, float x0, float x1, float z0, float z1, float h, float rise, Material wall, Material roof, int season, float overhang = 0.4f, bool backside = true)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2.5f);
            k.Gable(x0, z0, x1, z1, h, rise, Snowy(roof, season), wall, overhang);
            if (backside) { uint ls = Seed(x0, z1, h); Backside(k, faces, h, ref ls, false); }
            return faces;
        }

        /// <summary>Walls and a gable roof with its ridge running back from the street (gable end to the street).</summary>
        static Face[] GableFront(MeshKit k, float x0, float x1, float z0, float z1, float h, float rise, Material wall, Material roof, int season, float overhang = 0.4f, bool backside = true)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2.5f);
            float cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
            k.Push(new Vector3(cx, 0, cz), 90);
            k.Gable(-(z1 - z0) / 2, -(x1 - x0) / 2, (z1 - z0) / 2, (x1 - x0) / 2, h, rise, Snowy(roof, season), wall, overhang);
            k.Pop();
            if (backside) { uint ls = Seed(x0, z1, h); Backside(k, faces, h, ref ls, false); }
            return faces;
        }

        /// <summary>A box with a flat roof and a parapet. Returns the faces.</summary>
        static Face[] FlatBox(MeshKit k, float x0, float x1, float z0, float z1, float h, Material wall, Material coping, int season, float parapet = 0.6f, bool backside = true, int backStyle = 0)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2.5f);
            FlatRoof(k, x0, x1, z0, z1, h, Snowy(S("roof_membrane"), season), wall, coping ?? wall, parapet);
            if (backside)
            {
                uint ls = Seed(x0, z1, h);
                Backside(k, faces, h, ref ls, false, true, backStyle);
                // plant on the back half of the roof, clear of anything stood on the front
                if ((x1 - x0) * (z1 - z0) > 160) RoofClutter(k, x0 + 0.8f, x1 - 0.8f, (z0 + z1) / 2 + 0.5f, z1 - 0.8f, h, ref ls, 0.9f, wall);
                else BackRoof(k, x0, x1, z1, h, ref ls);
            }
            return faces;
        }

        /// <summary>A plant hall: metal walls, a roller door or two on the street side, a low-pitched roof.</summary>
        static Face[] Hall(MeshKit k, float x0, float x1, float z0, float z1, float h, Color wallTint, Color roofTint, bool closed, int season, int doors = 2, float rise = 1.6f, bool backside = true)
        {
            var wall = T("metal_panel", wallTint);
            var faces = GableBox(k, x0, x1, z0, z1, h, rise, wall, T("roof_metal", roofTint), season, 0.3f, backside);
            var door = closed ? MaterialLibrary.Dark("gf_rollup", new Color(0.75f, 0.75f, 0.75f)) : S("gf_rollup", 1.2f);
            var f = faces[South];
            for (int i = 0; i < doors; i++)
            {
                float a = f.Width * (i + 1) / (doors + 1);
                Rect(k, f, a - 1.7f, a + 1.7f, 0, 4.2f, door, new Vector2(0.5f, 0), new Vector2(1f, 1), 0.03f);
            }
            return faces;
        }

        /// <summary>A row of windows along a face.</summary>
        static void WindowRow(MeshKit k, Face f, float a0, float a1, float y, float ww, float wh, float step, ref uint s, bool closed)
        {
            var frame = C(H("#3a3e44"), 0.3f);
            for (float a = a0 + step / 2; a < a1; a += step)
            {
                Next(ref s);
                bool lit = !closed && R(ref s) < 0.6f;
                var pane = MaterialLibrary.Pane(lit ? new Color32(255, 222, 160, 255) : new Color32(26, 30, 38, 255));
                Ledge(k, f, a - ww / 2 - 0.08f, a + ww / 2 + 0.08f, y - 0.08f, y + wh + 0.08f, 0.04f, frame);
                Rect(k, f, a - ww / 2, a + ww / 2, y, y + wh, pane, Vector2.zero, Vector2.one, 0.05f);
            }
        }

        /// <summary>A door on a face.</summary>
        static void Door(MeshKit k, Face f, float along, Color32 col, float w = 1.1f, float h = 2.3f)
        {
            Ledge(k, f, along - w / 2 - 0.1f, along + w / 2 + 0.1f, 0, h + 0.12f, 0.05f, C(H("#2f3338"), 0.3f));
            Ledge(k, f, along - w / 2, along + w / 2, 0.02f, h, 0.07f, C(col, 0.35f));
        }

        /// <summary>A flat canopy sticking out of a face, on two posts.</summary>
        static void Canopy(MeshKit k, Face f, float a0, float a1, float y, float depth, Material top, bool posts = true)
        {
            Ledge(k, f, a0, a1, y, y + 0.3f, depth, top);
            if (!posts) return;
            var post = C(H("#3a3e44"), 0.4f, 0.4f);
            foreach (float a in new[] { a0 + 0.2f, a1 - 0.2f })
            {
                var p = f.At(a, 0) + f.Normal * (depth - 0.2f);
                k.Box(p + new Vector3(-0.08f, 0, -0.08f), p + new Vector3(0.08f, y, 0.08f), post, post);
            }
        }

        /// <summary>A sloped awning in one colour.</summary>
        static void PlainAwning(MeshKit k, Face f, float a0, float a1, float y, float depth, Color32 col)
        {
            var m = C(col, 0.15f);
            Vector3 p0 = f.At(a0, y), p1 = f.At(a1, y), q0 = f.At(a0, y - 0.8f) + f.Normal * depth, q1 = f.At(a1, y - 0.8f) + f.Normal * depth;
            k.Quad(q0, p0, p1, q1, m); k.Quad(q1, p1, p0, q0, m);
            Vector3 d0 = q0 + Vector3.down * 0.35f, d1 = q1 + Vector3.down * 0.35f;
            k.Quad(d0, q0, q1, d1, m); k.Quad(d1, q1, q0, d0, m);
        }

        /// <summary>A strip of neon along a face.</summary>
        static void Neon(MeshKit k, Face f, float a0, float a1, float y, Color32 col, bool closed, float t = 0.12f)
        {
            Ledge(k, f, a0, a1, y, y + t, 0.08f, closed ? C(H("#555555")) : Lit(col, 3f));
        }

        // ================================================================== yards and industry
        /// <summary>An upright cylinder with a cap.</summary>
        static void Silo(MeshKit k, float x, float z, float r, float h, Material body, Material cap, float capH = -1f)
        {
            k.Cylinder(new Vector3(x, 0.06f, z), r, r, h, 12, body, null, 3f);
            k.Cylinder(new Vector3(x, 0.06f + h, z), r, 0.15f, capH > 0 ? capH : r * 0.6f, 12, cap, cap);
        }

        /// <summary>A squat tank with a flat top and a band.</summary>
        static void Tank(MeshKit k, float x, float z, float r, float h, Material body, Material top)
        {
            k.Cylinder(new Vector3(x, 0.06f, z), r, r, h, 14, body, top, 3f);
            k.Cylinder(new Vector3(x, 0.06f + h * 0.55f, z), r + 0.05f, r + 0.05f, 0.25f, 14, top, top);
        }

        /// <summary>A chimney stack with a dark cap and a band near the top.</summary>
        static void Stack(MeshKit k, float x, float z, float baseY, float r, float h, Material body)
        {
            var dark = C(H("#2b2b2b"), 0.3f);
            k.Cylinder(new Vector3(x, baseY, z), r * 1.15f, r, h, 10, body, null, 2f);
            k.Cylinder(new Vector3(x, baseY + h * 0.85f, z), r * 1.05f, r * 1.05f, h * 0.06f, 10, C(H("#c0392b"), 0.3f), null);
            k.Cylinder(new Vector3(x, baseY + h, z), r, r * 0.95f, 0.4f, 10, dark, dark);
        }

        /// <summary>A heap: gravel, sand, logs' bark, coal.</summary>
        static void Pile(MeshKit k, float x, float z, float r, float h, Material m)
        {
            k.Cylinder(new Vector3(x, 0.06f, z), r, r * 0.25f, h * 0.85f, 9, m, null, 3f);
            k.Cylinder(new Vector3(x, 0.06f + h * 0.85f, z), r * 0.25f, 0f, h * 0.15f, 9, m, null, 3f);
        }

        /// <summary>A fence round the lot, open to the street in the middle (or not at all).</summary>
        static void Perimeter(MeshKit k, float w, float d, float h, Material m, float gate = 6f)
        {
            float x0 = -w / 2 + 0.3f, x1 = w / 2 - 0.3f, z0 = -d / 2 + 0.3f, z1 = d / 2 - 0.3f;
            k.Box(new Vector3(x0, 0.06f, z1 - 0.06f), new Vector3(x1, h, z1), m, m);
            k.Box(new Vector3(x0, 0.06f, z0), new Vector3(x0 + 0.06f, h, z1), m, m);
            k.Box(new Vector3(x1 - 0.06f, 0.06f, z0), new Vector3(x1, h, z1), m, m);
            if (gate < w - 1)
            {
                k.Box(new Vector3(x0, 0.06f, z0), new Vector3(-gate / 2, h, z0 + 0.06f), m, m);
                k.Box(new Vector3(gate / 2, 0.06f, z0), new Vector3(x1, h, z0 + 0.06f), m, m);
            }
        }

        /// <summary>A stack of crates or pallets of goods.</summary>
        static void Crates(MeshKit k, float x, float z, int n, Material m, ref uint s)
        {
            for (int i = 0; i < n; i++)
            {
                float ox = (i % 3) * 1.25f, oz = (i / 3 % 2) * 1.25f, oy = (i / 6) * 1.05f;
                k.Box(new Vector3(x + ox, 0.06f + oy, z + oz), new Vector3(x + ox + 1.1f, 1.06f + oy, z + oz + 1.1f), m, m);
            }
        }

        /// <summary>Barrels in a row.</summary>
        static void Barrels(MeshKit k, float x, float z, int n, Material m)
        {
            for (int i = 0; i < n; i++) k.Cylinder(new Vector3(x + i * 0.9f, 0.06f, z), 0.38f, 0.38f, 1.0f, 8, m, m);
        }

        /// <summary>A freestanding sign on two posts by the street, facing it.</summary>
        static void Pylon(MeshKit k, float x, float z, float h, float width, int row, bool closed, Material post = null)
        {
            post = post ?? C(H("#3a3e44"), 0.4f, 0.4f);
            k.Box(new Vector3(x - width / 2 + 0.2f, 0.06f, z - 0.1f), new Vector3(x - width / 2 + 0.4f, h, z + 0.1f), post, post);
            k.Box(new Vector3(x + width / 2 - 0.4f, 0.06f, z - 0.1f), new Vector3(x + width / 2 - 0.2f, h, z + 0.1f), post, post);
            var f = new Face { LeftBottom = new Vector3(x - width / 2, 0, z - 0.15f), Right = Vector3.right, Normal = Vector3.back, Width = width };
            Sign(k, f, width / 2, width, h, width / 8f, row, closed);
        }

        /// <summary>A sign board standing on a roof, facing the street.</summary>
        static void RoofSign(MeshKit k, float cx, float z, float y, float width, int row, bool closed)
        {
            var post = C(H("#3a3e44"), 0.4f, 0.4f);
            for (float a = -width / 2 + 0.5f; a <= width / 2 - 0.5f; a += Mathf.Max(1f, (width - 1f) / 2))
                k.Box(new Vector3(cx + a - 0.06f, y, z + 0.1f), new Vector3(cx + a + 0.06f, y + 0.8f, z + 0.25f), post, post);
            var f = new Face { LeftBottom = new Vector3(cx - width / 2, 0, z), Right = Vector3.right, Normal = Vector3.back, Width = width };
            Sign(k, f, width / 2, width, y + 0.7f, width / 7.5f, row, closed);
        }

        /// <summary>A small planter box with a bush in it.</summary>
        static void Planter(MeshKit k, float x, float z, int season)
        {
            var box = C(H("#7a6a58"), 0.2f);
            k.Box(new Vector3(x - 0.5f, 0.06f, z - 0.5f), new Vector3(x + 0.5f, 0.6f, z + 0.5f), box, T("dirt", Color.white));
            PropArt.Bush(k, new Vector3(x, 0.6f, z), 0.55f, season);
        }

        /// <summary>A cow, standing.</summary>
        static void Cow(MeshKit k, float x, float z, float yaw, Color32 hide)
        {
            k.Push(new Vector3(x, 0.06f, z), yaw);
            var body = C(hide, 0.2f); var dark = C(H("#2b2b2b"), 0.2f);
            k.Box(new Vector3(-0.35f, 0.7f, -0.9f), new Vector3(0.35f, 1.4f, 0.9f), body, body);
            k.Box(new Vector3(-0.22f, 0.95f, 0.9f), new Vector3(0.22f, 1.45f, 1.35f), body, body);
            foreach (var p in new[] { new Vector2(-0.25f, -0.7f), new Vector2(0.25f, -0.7f), new Vector2(-0.25f, 0.7f), new Vector2(0.25f, 0.7f) })
                k.Box(new Vector3(p.x - 0.08f, 0, p.y - 0.08f), new Vector3(p.x + 0.08f, 0.7f, p.y + 0.08f), dark, dark);
            k.Pop();
        }

        /// <summary>A greenhouse: glass walls and a glass gable roof.</summary>
        static void Greenhouse(MeshKit k, float x0, float x1, float z0, float z1, float h, bool closed)
        {
            var glass = closed ? C(H("#7d8b86"), 0.6f) : MaterialLibrary.Glow(new Color(0.62f, 0.78f, 0.72f), new Color(1f, 0.85f, 0.6f), 0.8f);
            var frame = C(H("#e8e8e4"), 0.4f, 0.3f);
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, glass, 2f);
            k.Gable(x0, z0, x1, z1, h, (z1 - z0) * 0.3f, glass, glass, 0.05f);
            for (float x = x0; x <= x1 + 0.01f; x += Mathf.Max(1.5f, (x1 - x0) / 6))
            {
                k.Box(new Vector3(x - 0.05f, 0, z0 - 0.05f), new Vector3(x + 0.05f, h, z0 + 0.05f), frame, frame);
                k.Box(new Vector3(x - 0.05f, 0, z1 - 0.05f), new Vector3(x + 0.05f, h, z1 + 0.05f), frame, frame);
            }
        }

        /// <summary>A conveyor on legs from one point to another (both at the same height), for quarries and mills.</summary>
        static void Conveyor(MeshKit k, Vector3 a, Vector3 b, float y0, float y1)
        {
            var steel = C(H("#5d6670"), 0.4f, 0.5f); var belt = C(H("#2b2b2b"), 0.2f);
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                var p0 = Vector3.Lerp(a, b, t0) + Vector3.up * Mathf.Lerp(y0, y1, t0);
                var p1 = Vector3.Lerp(a, b, t1) + Vector3.up * Mathf.Lerp(y0, y1, t1);
                var mid = (p0 + p1) / 2;
                var min = Vector3.Min(p0, p1) - new Vector3(0.45f, 0.15f, 0.45f); var max = Vector3.Max(p0, p1) + new Vector3(0.45f, 0.15f, 0.45f);
                k.Box(min, max, steel, belt);
                if (i % 2 == 1) k.Box(new Vector3(mid.x - 0.1f, 0.06f, mid.z - 0.1f), new Vector3(mid.x + 0.1f, mid.y, mid.z + 0.1f), steel, steel);
            }
        }

        /// <summary>A stack of timber or logs.</summary>
        static void Timber(MeshKit k, float x0, float z0, float x1, float z1, float h)
        {
            var wood = T("wood", new Color(0.82f, 0.68f, 0.5f));
            var ends = C(H("#c8a878"), 0.2f);
            k.Box(new Vector3(x0, 0.06f, z0), new Vector3(x1, h, z1), wood, wood, 1.5f);
            // log ends showing on the street side
            for (float x = x0 + 0.3f; x < x1 - 0.2f; x += 0.6f)
                for (float y = 0.35f; y < h - 0.1f; y += 0.55f)
                    k.Box(new Vector3(x - 0.22f, y - 0.22f, z0 - 0.04f), new Vector3(x + 0.22f, y + 0.22f, z0), ends, ends);
        }

        /// <summary>Rows of a crop or vines across a field.</summary>
        static void Rows(MeshKit k, float x0, float z0, float x1, float z1, float spacing, float h, Color32 col, int season, bool vines)
        {
            var leaf = C(season == 0 ? H("#7a6e5a") : season == 3 ? H("#b88a3a") : col, 0.2f);
            var post = C(H("#6d5a44"), 0.2f);
            for (float z = z0 + spacing / 2; z < z1; z += spacing)
            {
                k.Box(new Vector3(x0, 0.06f, z - 0.3f), new Vector3(x1, h, z + 0.3f), leaf, leaf);
                if (vines) for (float x = x0; x <= x1; x += 4f) k.Box(new Vector3(x - 0.05f, 0.06f, z - 0.05f), new Vector3(x + 0.05f, h + 0.2f, z + 0.05f), post, post);
            }
        }

        /// <summary>A small fruit tree.</summary>
        static void FruitTree(MeshKit k, float x, float z, int season, Color32 fruit)
        {
            var trunk = C(H("#6b4f3a"));
            k.Box(new Vector3(x - 0.12f, 0.06f, z - 0.12f), new Vector3(x + 0.12f, 1.3f, z + 0.12f), trunk, trunk);
            var crown = season == 0 ? H("#8a7a66") : season == 3 ? H("#c9822e") : H("#4f8a3a");
            k.Blob(new Vector3(x, 2.0f, z), new Vector3(1.2f, 0.9f, 1.2f), crown);
            if (season == 2 || season == 3) k.Blob(new Vector3(x + 0.6f, 1.9f, z - 0.6f), new Vector3(0.18f, 0.18f, 0.18f), fruit);
        }

        /// <summary>Outdoor tables with umbrellas along a face.</summary>
        static void Patio(MeshKit k, Face f, float a0, float a1, float out_, Color32 col)
        {
            for (float a = a0 + 1.3f; a < a1 - 0.8f; a += 2.8f) CafeTable(k, f.At(a, 0.06f) + f.Normal * out_, col);
        }
    }
}
