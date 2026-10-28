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
                               Material trim = null, float g0 = 0.08f, float g1 = 0.92f, float signW = -1f, float glassH = 3.6f)
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
            trim = trim ?? C(H("#d8d4ca"), 0.3f);
            FlatRoof(k, x0, x1, z0, z1, h, Snowy(S("roof_membrane"), season), wall, trim, 0.5f);
            float sw = signW > 0 ? signW : Mathf.Min(W - 1.2f, 7.5f);
            if (signRow >= 0) Sign(k, front, W / 2, sw, glassH + 0.25f, Mathf.Min(h - glassH - 0.45f, sw / 9f), signRow, closed);
            if (closed)
            {
                var boards = T("wood", new Color(0.75f, 0.65f, 0.52f));
                for (float a = a0 + 0.4f; a < a1 - 1f; a += 2.6f) Ledge(k, front, a, a + 2.0f, 0.5f, glassH - 0.4f, 0.06f, boards);
            }
            return front;
        }

        /// <summary>Walls and a gable roof (ridge along x). Returns the faces.</summary>
        static Face[] GableBox(MeshKit k, float x0, float x1, float z0, float z1, float h, float rise, Material wall, Material roof, int season, float overhang = 0.4f)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2.5f);
            k.Gable(x0, z0, x1, z1, h, rise, Snowy(roof, season), wall, overhang);
            return faces;
        }

        /// <summary>Walls and a gable roof with its ridge running back from the street (gable end to the street).</summary>
        static Face[] GableFront(MeshKit k, float x0, float x1, float z0, float z1, float h, float rise, Material wall, Material roof, int season, float overhang = 0.4f)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2.5f);
            float cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
            k.Push(new Vector3(cx, 0, cz), 90);
            k.Gable(-(z1 - z0) / 2, -(x1 - x0) / 2, (z1 - z0) / 2, (x1 - x0) / 2, h, rise, Snowy(roof, season), wall, overhang);
            k.Pop();
            return faces;
        }

        /// <summary>A box with a flat roof and a parapet. Returns the faces.</summary>
        static Face[] FlatBox(MeshKit k, float x0, float x1, float z0, float z1, float h, Material wall, Material coping, int season, float parapet = 0.6f)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2.5f);
            FlatRoof(k, x0, x1, z0, z1, h, Snowy(S("roof_membrane"), season), wall, coping ?? wall, parapet);
            return faces;
        }

        /// <summary>A plant hall: metal walls, a roller door or two on the street side, a low-pitched roof.</summary>
        static Face[] Hall(MeshKit k, float x0, float x1, float z0, float z1, float h, Color wallTint, Color roofTint, bool closed, int season, int doors = 2, float rise = 1.6f)
        {
            var wall = T("metal_panel", wallTint);
            var faces = GableBox(k, x0, x1, z0, z1, h, rise, wall, T("roof_metal", roofTint), season, 0.3f);
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
