using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>Farms, industry, public buildings, parks and building sites. Lots: 1 tile = 10 m.</summary>
    public static partial class BuildingArt
    {
        /// <summary>The same face moved out by `by` metres (to put a sign on the front of a portico or canopy).</summary>
        static Face Out(Face f, float by) { f.LeftBottom += f.Normal * by; return f; }

        // ================================================================== farm (30 x 30)
        static void Farm(MeshKit k, float w, float d, ref uint s, int season)
        {
            string crop = season == 1 ? "field_young" : season == 2 ? "field_green" : season == 3 ? "field_ripe" : "snow";
            float fx1 = w / 2 - 11.5f;
            k.Flat(-w / 2 + 0.2f, -d / 2 + 0.2f, fx1, d / 2 - 0.2f, 0.06f, S(crop), 8f);
            k.Flat(fx1, -d / 2 + 0.2f, w / 2 - 0.2f, d / 2 - 0.2f, 0.06f, season == 0 ? S("snow") : S("gravel"), 4f);
            // the barn: red board-and-batten, a metal roof with its ridge running back from the road, white-trimmed doors
            float bx0 = w / 2 - 10.5f, bx1 = w / 2 - 2.5f, bz0 = 0.5f, bz1 = 11.5f, bh = 5.2f;
            var barnWall = T("wall_board", new Color(0.62f, 0.2f, 0.17f));
            var bf = Faces(bx0, bx1, bz0, bz1);
            foreach (var f in bf) Plain(k, f, 0, bh, barnWall, 2f);
            k.Push(new Vector3((bx0 + bx1) / 2, 0, (bz0 + bz1) / 2), 90);
            k.Gable(-(bz1 - bz0) / 2, -(bx1 - bx0) / 2, (bz1 - bz0) / 2, (bx1 - bx0) / 2, bh, 3.2f, RoofMat("roof_metal", new Color(0.55f, 0.57f, 0.6f), season), barnWall, 0.4f);
            k.Pop();
            var trim = C(Trim, 0.3f);
            var door = T("wall_board", new Color(0.52f, 0.17f, 0.14f));
            var front = bf[South];
            float dc = front.Width / 2;
            Ledge(k, front, dc - 2.4f, dc + 2.4f, 0, 4.0f, 0.06f, door);
            Ledge(k, front, dc - 2.55f, dc + 2.55f, 4.0f, 4.2f, 0.1f, trim);
            Ledge(k, front, dc - 2.55f, dc - 2.4f, 0, 4.0f, 0.1f, trim);
            Ledge(k, front, dc + 2.4f, dc + 2.55f, 0, 4.0f, 0.1f, trim);
            Ledge(k, front, dc - 0.07f, dc + 0.07f, 0, 4.0f, 0.1f, trim);
            Ledge(k, front, dc - 0.9f, dc + 0.9f, 5.0f, 6.4f, 0.06f, door);
            Ledge(k, front, dc - 1.0f, dc + 1.0f, 4.9f, 5.0f, 0.1f, trim);
            // silos behind it
            var steel = T("metal_panel", new Color(0.86f, 0.88f, 0.9f));
            var cap = C(H("#9aa3ab"), 0.5f, 0.7f);
            k.Cylinder(new Vector3(w / 2 - 2.4f, 0.06f, 13.4f), 1.5f, 1.5f, 10f, 14, steel, null, 3f);
            k.Cylinder(new Vector3(w / 2 - 2.4f, 10.06f, 13.4f), 1.5f, 0f, 1.5f, 14, cap, cap);
            k.Cylinder(new Vector3(w / 2 - 6.0f, 0.06f, 13.5f), 1.3f, 1.3f, 8f, 14, steel, null, 3f);
            k.Cylinder(new Vector3(w / 2 - 6.0f, 8.06f, 13.5f), 1.3f, 0f, 1.3f, 14, cap, cap);
            // the farmhouse by the road
            float hx0 = w / 2 - 10.5f, hx1 = hx0 + 6f, hz0 = -d / 2 + 3f, hz1 = hz0 + 5.5f;
            var siding = T("wall_siding", new Color(0.98f, 0.97f, 0.94f));
            HouseWalls(k, hx0, hx1, hz0, hz1, 5.4f, siding);
            k.Gable(hx0, hz0, hx1, hz1, 5.4f, 2.4f, RoofMat("roof_shingle", new Color(0.45f, 0.45f, 0.47f), season), siding, 0.35f);
            var hf = Faces(hx0, hx1, hz0, hz1);
            FrontDoor(k, hf[South], 3f, ref s);
            HouseWindow(k, hf[South], 1.2f, 0.95f, 0.95f, 1.3f, ref s, null);
            HouseWindow(k, hf[South], 4.8f, 0.95f, 0.95f, 1.3f, ref s, null);
            HouseWindow(k, hf[South], 1.6f, 3.6f, 0.9f, 1.2f, ref s, null);
            HouseWindow(k, hf[South], 4.4f, 3.6f, 0.9f, 1.2f, ref s, null);
            HouseWindow(k, hf[East], 2.75f, 0.95f, 0.95f, 1.3f, ref s, null);
            HouseWindow(k, hf[West], 2.75f, 0.95f, 0.95f, 1.3f, ref s, null);
            k.Flat(hx0 + 2.4f, -d / 2 + 0.2f, hx0 + 3.6f, hz0 - 1.0f, 0.08f, season == 0 ? S("snow") : S("gravel"), 3f);
            Tree(k, hx1 + 1.8f, hz1 + 0.5f, 1.0f, season, s);
            // a tractor by the barn
            k.Push(new Vector3(bx0 - 2.2f, 0.06f, 4f), 200);
            Tractor(k);
            k.Pop();
            // a rail fence along the road
            var post = T("wood", new Color(0.85f, 0.8f, 0.72f));
            for (float x = -w / 2 + 0.4f; x < fx1; x += 2.5f)
                k.Box(new Vector3(x - 0.07f, 0.06f, -d / 2 + 0.3f), new Vector3(x + 0.07f, 1.2f, -d / 2 + 0.44f), post, post);
            foreach (float y in new[] { 0.55f, 1.0f })
                k.Box(new Vector3(-w / 2 + 0.3f, y, -d / 2 + 0.33f), new Vector3(fx1, y + 0.1f, -d / 2 + 0.41f), post, post);
        }

        /// <summary>A green farm tractor, facing +z.</summary>
        static void Tractor(MeshKit k)
        {
            var green = C(H("#2e7d32"), 0.45f);
            var yellow = C(H("#f1c40f"), 0.4f);
            var tire = C(H("#1b1b1b"), 0.2f);
            k.Box(new Vector3(-0.55f, 0.6f, 0.2f), new Vector3(0.55f, 1.45f, 2.3f), green, green);
            k.Box(new Vector3(-0.75f, 0.6f, -1.1f), new Vector3(0.75f, 1.5f, 0.3f), green, green);
            k.Box(new Vector3(-0.72f, 1.5f, -1.0f), new Vector3(0.72f, 2.75f, 0.25f), C(H("#3b3e42"), 0.4f, 0.3f), green);
            k.Quad(new Vector3(0.6f, 1.6f, 0.26f), new Vector3(0.6f, 2.6f, 0.26f), new Vector3(-0.6f, 2.6f, 0.26f), new Vector3(-0.6f, 1.6f, 0.26f), new Color32(30, 40, 50, 255), true);
            k.Box(new Vector3(0.25f, 1.45f, 1.6f), new Vector3(0.38f, 2.4f, 1.73f), C(H("#2b2b2b")), C(H("#2b2b2b")));
            k.Box(new Vector3(-1.25f, 0, -1.2f), new Vector3(-0.8f, 1.55f, 0.15f), tire, tire);
            k.Box(new Vector3(0.8f, 0, -1.2f), new Vector3(1.25f, 1.55f, 0.15f), tire, tire);
            k.Box(new Vector3(-1.3f, 0.55f, -0.75f), new Vector3(-1.25f, 1.0f, -0.3f), yellow, yellow);
            k.Box(new Vector3(1.25f, 0.55f, -0.75f), new Vector3(1.3f, 1.0f, -0.3f), yellow, yellow);
            k.Box(new Vector3(-0.95f, 0, 1.4f), new Vector3(-0.65f, 0.85f, 2.2f), tire, tire);
            k.Box(new Vector3(0.65f, 0, 1.4f), new Vector3(0.95f, 0.85f, 2.2f), tire, tire);
        }

        // ================================================================== industry (20 x 20, electronics 30 x 20)
        static void Plant(MeshKit k, float w, float d, ref uint s, bool closed, int season, string cladding, int signRow, bool tanks, bool sawtooth)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            float x0 = -w / 2 + 1f, x1 = w / 2 - 1f, z0 = -d / 2 + 4.5f, z1 = d / 2 - 1f;
            if (tanks) x1 = w / 2 - 6.5f;
            var skin = new Skin { Upper = cladding, Floor = 4.5f, Bay = 3.2f, Ground = "gf_rollup", GroundH = 4.7f, GroundBay = 3.2f, Wall = "metal_panel", Coping = H("#8d969e"), Glow = 1.3f };
            int u = (int)(Next(ref s) % 4);
            float top = Block(k, x0, x1, z0, z1, skin, 1, u, 0, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            Sign(k, faces[South], faces[South].Width / 2, Mathf.Min(8f, faces[South].Width - 2f), top - 2.2f, 0.75f, signRow, closed);
            // a loading dock along the front
            var dock = S("concrete");
            k.Box(new Vector3(x0 + 0.5f, 0, z0 - 1.6f), new Vector3(x1 - 0.5f, 1.1f, z0), dock, dock, 2f);
            if (sawtooth)
            {
                // north-light roof: glazed faces toward the street, slopes falling back
                var glass = closed ? MaterialLibrary.Dark("fac_glass_blue", new Color(0.7f, 0.7f, 0.7f)) : S("fac_glass_blue", 1.4f);
                var slope = season == 0 ? S("snow") : T("metal_panel", new Color(0.78f, 0.8f, 0.82f));
                var end = S("metal_panel");
                float xa = x0 + 0.4f, xb = x1 - 0.4f;
                for (float z = z0 + 0.4f; z < z1 - 3.2f; z += 3.4f)
                {
                    float ze = z + 3.2f, y = top;
                    var gf = new Face { LeftBottom = new Vector3(xa, 0, z), Right = Vector3.right, Normal = Vector3.back, Width = xb - xa };
                    Rect(k, gf, 0, gf.Width, y, y + 2.0f, glass, new Vector2(0, 0.5f), new Vector2(Mathf.Round(gf.Width / 3.2f) * 0.25f, 0.66f));
                    k.Quad(new Vector3(xa, y + 2.0f, z), new Vector3(xa, y, ze), new Vector3(xb, y, ze), new Vector3(xb, y + 2.0f, z), slope, 6f);
                    k.Tri(new Vector3(xa, y, ze), new Vector3(xa, y + 2.0f, z), new Vector3(xa, y, z), end);
                    k.Tri(new Vector3(xb, y, z), new Vector3(xb, y + 2.0f, z), new Vector3(xb, y, ze), end);
                }
            }
            else
            {
                uint rs = s;
                RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 2.2f, S("metal_panel"));
                var brick = S("wall_brick_brown");
                k.Cylinder(new Vector3(x1 - 2.5f, top, z1 - 2.5f), 0.8f, 0.65f, 7f, 10, brick, C(H("#2b2b2b")), 2f);
            }
            if (tanks)
            {
                var tank = T("metal_panel", new Color(0.93f, 0.94f, 0.95f));
                var dome = C(H("#c9ced3"), 0.5f, 0.7f);
                foreach (float z in new[] { -2f, 4.2f })
                {
                    k.Cylinder(new Vector3(w / 2 - 3.4f, 0.06f, z), 2.1f, 2.1f, 8f, 14, tank, null, 3f);
                    k.Cylinder(new Vector3(w / 2 - 3.4f, 8.06f, z), 2.1f, 0.3f, 1.0f, 14, dome, dome);
                }
                var pipe = C(H("#9aa3ab"), 0.5f, 0.7f);
                k.Box(new Vector3(x1, 6.2f, -0.2f), new Vector3(w / 2 - 3.4f, 6.5f, 0.1f), pipe, pipe);
            }
            if (!closed)
            {
                k.Push(new Vector3(x0 + 4.5f, 0.06f, -d / 2 + 1.35f), 90);
                PropArt.Truck(k, H("#ecf0f1"), H("#dfe4e8"));
                k.Pop();
                PropArt.Parked(k, new Vector3(w / 2 - 3f, 0.06f, -d / 2 + 1.5f), 90, s);
            }
        }

        static void Workshop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("gravel"), 0.05f, 4f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 6, z0 = -d / 2 + 4, z1 = d / 2 - 1, h = 5.8f;
            var wall = T("wall_board", new Color(0.62f, 0.45f, 0.3f));
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0, h, wall, 2f);
            k.Gable(x0, z0, x1, z1, h, 3f, RoofMat("roof_metal", new Color(0.4f, 0.42f, 0.44f), season), wall, 0.5f);
            var doors = closed ? MaterialLibrary.Dark("gf_rollup", new Color(0.75f, 0.75f, 0.75f)) : S("gf_rollup", 1.3f);
            Rect(k, faces[South], 2f, 8.5f, 0, 4.7f, doors, new Vector2(0.5f, 0), new Vector2(1.0f, 1), 0.02f);
            Sign(k, faces[South], faces[South].Width / 2, 5.5f, 4.9f, 0.5f, SignRow.Workshop, closed);
            foreach (int side in new[] { East, West })
                for (float a = 2; a < faces[side].Width - 1; a += 3.5f) HouseWindow(k, faces[side], a, 2.0f, 1.4f, 1.6f, ref s, null);
            var timber = T("wood", new Color(0.92f, 0.82f, 0.66f));
            for (int i = 0; i < 3; i++)
                k.Box(new Vector3(w / 2 - 5, 0.06f, -d / 2 + 2 + i * 5), new Vector3(w / 2 - 1, 1.2f + (i % 2) * 0.6f, -d / 2 + 5 + i * 5), timber, timber, 2f);
            if (!closed) PropArt.Parked(k, new Vector3(-w / 2 + 3f, 0.06f, -d / 2 + 1.6f), 90, s);
        }

        static void Yard(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("gravel"), 0.05f, 4f);
            // the site office: a yellow cabin with a row of windows
            var yellow = T("metal_panel", new Color(0.98f, 0.8f, 0.2f));
            float cx0 = -w / 2 + 1, cx1 = -w / 2 + 9, cz0 = -d / 2 + 2, cz1 = -d / 2 + 5;
            var cf = Faces(cx0, cx1, cz0, cz1);
            foreach (var f in cf) Plain(k, f, 0, 3.0f, yellow, 2f);
            k.Box(new Vector3(cx0 - 0.1f, 3.0f, cz0 - 0.1f), new Vector3(cx1 + 0.1f, 3.2f, cz1 + 0.1f), C(H("#9aa3ab")), season == 0 ? S("snow") : C(H("#9aa3ab")));
            k.WindowsFront(cx0 + 0.6f, cx1 - 0.6f, 1.0f, 2.6f, cz0, 3, 1, 1.2f, 0.9f, s, closed ? 0f : 0.8f);
            Sign(k, cf[South], cf[South].Width / 2, 5.5f, 3.25f, 0.5f, SignRow.Builders, closed);
            var lumber = T("wood", new Color(0.92f, 0.82f, 0.66f));
            k.Box(new Vector3(-w / 2 + 1, 0.06f, d / 2 - 7), new Vector3(-w / 2 + 6, 1.6f, d / 2 - 2), lumber, lumber, 2f);
            var blocks = S("concrete");
            k.Box(new Vector3(-w / 2 + 7, 0.06f, d / 2 - 6), new Vector3(-w / 2 + 11, 1.0f, d / 2 - 2), blocks, blocks, 1f);
            k.Cylinder(new Vector3(3.5f, 0.06f, d / 2 - 5), 2.2f, 0, 2.2f, 9, season == 0 ? S("snow") : S("sand"), null, 3f);
            if (!closed)
            {
                // an excavator
                var orange = C(H("#f39c12"), 0.4f);
                var dark = C(H("#2b2b2b"), 0.3f);
                k.Push(new Vector3(w / 2 - 5, 0.06f, -1), 20);
                k.Box(new Vector3(-1.6f, 0, -2.2f), new Vector3(1.6f, 0.7f, 2.2f), dark, dark);
                k.Box(new Vector3(-1.4f, 0.7f, -1.8f), new Vector3(1.4f, 2.0f, 1.8f), orange, orange);
                k.Box(new Vector3(-1.1f, 2.0f, -0.4f), new Vector3(0.9f, 3.4f, 1.6f), orange, orange);
                k.Quad(new Vector3(-0.95f, 2.2f, -0.41f), new Vector3(-0.95f, 3.2f, -0.41f), new Vector3(0.75f, 3.2f, -0.41f), new Vector3(0.75f, 2.2f, -0.41f), new Color32(30, 40, 50, 255), true);
                k.Box(new Vector3(-0.25f, 1.6f, -5.6f), new Vector3(0.25f, 2.1f, -1.8f), orange, orange);
                k.Box(new Vector3(-0.2f, 0.4f, -6.2f), new Vector3(0.2f, 2.1f, -5.6f), orange, orange);
                k.Box(new Vector3(-0.5f, 0.06f, -6.6f), new Vector3(0.5f, 0.7f, -5.9f), dark, dark);
                k.Pop();
                // a tower crane with a load of lumber on the hook
                float tx = w / 2 - 3, tz = d / 2 - 3;
                var crane = C(H("#f1c40f"), 0.4f, 0.3f);
                for (float y = 0; y < 18; y += 2) k.Box(new Vector3(tx - 0.5f, y, tz - 0.5f), new Vector3(tx + 0.5f, y + 1.8f, tz + 0.5f), crane, crane);
                k.Box(new Vector3(tx - 14, 18, tz - 0.4f), new Vector3(tx + 4, 18.8f, tz + 0.4f), crane, crane);
                k.Box(new Vector3(tx + 2, 17, tz - 1), new Vector3(tx + 4, 18, tz + 1), blocks, blocks);
                k.Box(new Vector3(tx - 1, 18.8f, tz - 1), new Vector3(tx + 1, 20.2f, tz + 1), crane, crane);
                var cable = C(H("#2b2b2b"));
                k.Box(new Vector3(tx - 9.03f, 9f, tz - 0.03f), new Vector3(tx - 8.97f, 18f, tz + 0.03f), cable, cable);
                k.Box(new Vector3(tx - 9.6f, 8.4f, tz - 0.6f), new Vector3(tx - 8.4f, 9f, tz + 0.6f), lumber, lumber);
            }
            var fence = C(H("#9aa3ab"), 0.5f, 0.6f);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.06f, d / 2 - 0.4f), new Vector3(w / 2 - 0.3f, 1.8f, d / 2 - 0.3f), fence, fence);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.06f, -d / 2 + 0.3f), new Vector3(-w / 2 + 0.4f, 1.8f, d / 2 - 0.3f), fence, fence);
            k.Box(new Vector3(w / 2 - 0.4f, 0.06f, -d / 2 + 0.3f), new Vector3(w / 2 - 0.3f, 1.8f, d / 2 - 0.3f), fence, fence);
        }

        static void Depot(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = 0f, z1 = d / 2 - 1;
            var skin = new Skin { Upper = "fac_metal_grey", Floor = 4.5f, Bay = 3.2f, Ground = "gf_rollup", GroundH = 4.7f, GroundBay = 3.2f, Wall = "metal_panel", Coping = H("#8d969e"), Glow = 1.3f };
            float top = Block(k, x0, x1, z0, z1, skin, 1, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            var dock = S("concrete");
            k.Box(new Vector3(x0, 0, z0 - 1.6f), new Vector3(x1, 1.1f, z0), dock, dock, 2f);
            Sign(k, faces[South], faces[South].Width / 2, 8f, top - 2.0f, 0.72f, SignRow.Logistics, closed);
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 1.2f, S("metal_panel"));
            // truck bays painted on the yard, trucks backed up to the dock
            var paint = C(H("#f2f2ee"), 0.3f);
            for (int i = 0; i <= 3; i++) k.Flat(x0 + 0.5f + i * 6f, -d / 2 + 0.5f, x0 + 0.62f + i * 6f, z0 - 2f, 0.08f, paint);
            if (!closed)
                for (int i = 0; i < 3; i++)
                {
                    if (i == 2 && (s & 4u) == 0) continue;
                    k.Push(new Vector3(x0 + 3.5f + i * 6f, 0.06f, z0 - 5.85f), 180);
                    PropArt.Truck(k, H("#ecf0f1"), i == 1 ? H("#0a79df") : H("#e1e5e8"));
                    k.Pop();
                }
        }

        // ================================================================== public buildings
        static void TownHall(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, S("pavers"), 0.02f, 3f);
            var skin = new Skin { Upper = "fac_civic_brick", Floor = 3.15f, Wall = "wall_brick_red", Coping = H("#e2dccd"), Glow = 1.3f };
            float x0 = -w / 2 + 2, x1 = w / 2 - 2, z0 = -d / 2 + 6, z1 = d / 2 - 1.5f;
            float top = Block(k, x0, x1, z0, z1, skin, 3, 0, 0, true, false, "roof_membrane", season);
            var faces = Faces(x0, x1, z0, z1);
            var stone = S("wall_stone");
            var stoneTop = C(H("#ddd5c3"), 0.25f);
            var white = C(H("#efeae0"), 0.3f);
            var f = faces[South];
            // a stone base course, then the portico: steps, four columns, an entablature and a pediment
            Ledge(k, f, -0.1f, f.Width + 0.1f, 0, 1.0f, 0.12f, stone);
            float c = f.Width / 2;
            k.Box(f.At(c - 5.2f, 0) + f.Normal * 3.2f, f.At(c + 5.2f, 0.7f), stone, stoneTop, 2f);
            for (int i = 1; i < 3; i++) k.Box(f.At(c - 5.2f, 0) + f.Normal * (3.2f + i * 0.35f), f.At(c + 5.2f, 0.7f - i * 0.22f) + f.Normal * 3.2f, stone, stoneTop, 2f);
            for (int i = 0; i < 4; i++)
                k.Cylinder(f.At(c - 4.2f + i * 2.8f, 0.7f) + f.Normal * 2.4f, 0.42f, 0.38f, 6.5f, 12, white, white);
            Ledge(k, f, c - 5.2f, c + 5.2f, 7.2f, 8.2f, 3.2f, stone, stoneTop);
            Vector3 l = f.At(c - 5.2f, 8.2f) + f.Normal * 3.2f, r = f.At(c + 5.2f, 8.2f) + f.Normal * 3.2f, apex = f.At(c, 10.4f) + f.Normal * 3.2f;
            k.Tri(l, apex, r, stone);
            var copper = T("roof_metal", new Color(0.45f, 0.62f, 0.52f));   // too steep to hold snow
            Vector3 lb = f.At(c - 5.2f, 8.2f), rb = f.At(c + 5.2f, 8.2f), ab = f.At(c, 10.4f);
            k.Quad(lb, ab, apex, l, copper); k.Quad(r, apex, ab, rb, copper);
            Sign(k, Out(f, 3.2f), c, 5f, 7.32f, 0.62f, SignRow.TownHall, false);
            Rect(k, f, c - 1.6f, c + 1.6f, 0.7f, 3.6f, S("gf_lobby", 1.3f), new Vector2(0.25f, 0), new Vector2(0.75f, 0.75f), 0.05f);
            // the clock tower and its dome
            float tx = (x0 + x1) / 2, tz = (z0 + z1) / 2;
            k.Box(new Vector3(tx - 2.2f, top, tz - 2.2f), new Vector3(tx + 2.2f, top + 5.5f, tz + 2.2f), stone, stone, 2f);
            var dark = C(Dark);
            for (int side = 0; side < 4; side++)
            {
                k.Push(new Vector3(tx, 0, tz), side * 90);
                k.Box(new Vector3(-1.1f, top + 2.1f, -2.3f), new Vector3(1.1f, top + 4.3f, -2.2f), white, white);
                k.Box(new Vector3(-0.06f, top + 3.2f, -2.34f), new Vector3(0.06f, top + 4.0f, -2.3f), dark, dark);
                k.Box(new Vector3(-0.06f, top + 3.14f, -2.34f), new Vector3(0.6f, top + 3.26f, -2.3f), dark, dark);
                k.Pop();
            }
            k.Box(new Vector3(tx - 2.4f, top + 5.5f, tz - 2.4f), new Vector3(tx + 2.4f, top + 5.9f, tz + 2.4f), white, white);
            k.Cylinder(new Vector3(tx, top + 5.9f, tz), 2.0f, 1.8f, 1.0f, 16, white, null);
            k.Cylinder(new Vector3(tx, top + 6.9f, tz), 2.0f, 1.6f, 0.9f, 16, copper, null, 2f);
            k.Cylinder(new Vector3(tx, top + 7.8f, tz), 1.6f, 0.9f, 0.9f, 16, copper, null, 2f);
            k.Cylinder(new Vector3(tx, top + 8.7f, tz), 0.9f, 0f, 0.8f, 16, copper, null, 2f);
            k.Box(new Vector3(tx - 0.05f, top + 9.4f, tz - 0.05f), new Vector3(tx + 0.05f, top + 10.6f, tz + 0.05f), dark, dark);
            Flag(k, -w / 2 + 1.5f, -d / 2 + 1.5f, 9f, H("#2e86de"));
            Flag(k, w / 2 - 3.5f, -d / 2 + 1.5f, 9f, H("#c0392b"));
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x0 + 4.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 1f, S(skin.Wall));
            Tree(k, -w / 2 + 1.2f, d / 2 - 1.2f, 0.9f, season, s);
            Tree(k, w / 2 - 1.2f, d / 2 - 1.2f, 0.9f, season, s + 3);
        }

        static void School(MeshKit k, float w, float d, ref uint s, int season, bool high)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 8f);
            var skin = new Skin { Upper = high ? "fac_civic_brown" : "fac_civic_brick", Floor = 3.15f, Wall = high ? "wall_brick_brown" : "wall_brick_red", Coping = H("#e2dccd"), Glow = 1.2f };
            float x0 = -w / 2 + 1.5f, x1 = high ? -w / 2 + 16.5f : w / 2 - 1.5f, z0 = -d / 2 + 4f, z1 = z0 + 8f;
            int floors = high ? 3 : 2;
            Block(k, x0, x1, z0, z1, skin, floors, 0, 1, true, false, "roof_membrane", season);
            // a classroom wing going back
            Block(k, x0, x0 + 8f, z1, d / 2 - 1.5f, skin, floors, 2, 2, true, false, "roof_membrane", season);
            var f = Faces(x0, x1, z0, z1)[South];
            k.Flat(-w / 2 + 0.3f, -d / 2 + 0.3f, x1, z0, 0.07f, S("concrete"), 4f);
            float c = f.Width / 2;
            Ledge(k, f, c - 2.6f, c + 2.6f, 3.0f, 3.3f, 2.2f, C(H("#e9e4d8"), 0.3f));
            Rect(k, f, c - 2.0f, c + 2.0f, 0, 3.0f, S("gf_lobby", 1.2f), new Vector2(0.25f, 0), new Vector2(0.75f, 0.65f), 0.05f);
            Sign(k, f, c, 6.5f, 3.4f, 0.55f, high ? SignRow.HighSchool : SignRow.Elementary, false);
            Flag(k, x0 + 1, -d / 2 + 1.2f, 8f, H("#2e86de"));
            var white = C(H("#f4f4f2"));
            if (high)
            {
                // a football field inside a running track, bleachers on the near side
                float fx0 = x1 + 3.0f, fx1 = w / 2 - 1, fz0 = -d / 2 + 1.5f, fz1 = d / 2 - 1.5f, mz = (fz0 + fz1) / 2, mx = (fx0 + fx1) / 2;
                k.Flat(fx0, fz0, fx1, fz1, 0.08f, season == 0 ? S("snow") : S("track"), 4f);
                k.Flat(fx0 + 1.4f, fz0 + 1.4f, fx1 - 1.4f, fz1 - 1.4f, 0.1f, GrassMat(season), 6f);
                if (season != 0)
                {
                    k.Flat(fx0 + 1.4f, mz - 0.08f, fx1 - 1.4f, mz + 0.08f, 0.11f, white);
                    k.Cylinder(new Vector3(mx, 0.1f, mz), 0.3f, 0.3f, 0.02f, 10, white, white);
                }
                foreach (float gz in new[] { fz0 + 1.6f, fz1 - 1.75f })
                {
                    k.Box(new Vector3(mx - 1.5f, 0.1f, gz), new Vector3(mx - 1.38f, 2.6f, gz + 0.12f), white, white);
                    k.Box(new Vector3(mx + 1.38f, 0.1f, gz), new Vector3(mx + 1.5f, 2.6f, gz + 0.12f), white, white);
                    k.Box(new Vector3(mx - 1.5f, 1.2f, gz), new Vector3(mx + 1.5f, 1.32f, gz + 0.12f), white, white);
                }
                var bench = C(H("#9aa3ab"), 0.4f, 0.5f);
                for (int i = 0; i < 4; i++)
                    k.Box(new Vector3(x1 + 0.4f, 0.06f, -4f), new Vector3(fx0 - 0.2f - i * 0.6f, 0.4f + i * 0.4f, 5f), bench, bench);
            }
            else
            {
                // a playground on rubber surfacing behind the front block
                float px = x0 + 10, pz = z1 + 1.5f;
                k.Flat(px - 1, pz - 0.5f, w / 2 - 1.5f, d / 2 - 1.5f, 0.08f, season == 0 ? S("snow") : S("track"), 4f);
                var red = C(H("#e74c3c"), 0.4f); var yellow = C(H("#f1c40f"), 0.4f); var blue = C(H("#3498db"), 0.4f); var teal = C(H("#1abc9c"), 0.4f);
                // swings
                k.Box(new Vector3(px, 0.08f, pz), new Vector3(px + 0.15f, 2.6f, pz + 0.15f), red, red);
                k.Box(new Vector3(px + 3, 0.08f, pz), new Vector3(px + 3.15f, 2.6f, pz + 0.15f), red, red);
                k.Box(new Vector3(px, 2.5f, pz), new Vector3(px + 3.15f, 2.65f, pz + 0.15f), red, red);
                k.Box(new Vector3(px + 0.8f, 0.6f, pz - 0.2f), new Vector3(px + 1.4f, 0.7f, pz + 0.35f), yellow, yellow);
                k.Box(new Vector3(px + 1.8f, 0.6f, pz - 0.2f), new Vector3(px + 2.4f, 0.7f, pz + 0.35f), blue, blue);
                // a climbing tower with a slide, and a sandpit
                float sx = px + 4.2f;
                k.Box(new Vector3(sx, 0.08f, pz), new Vector3(sx + 1.6f, 1.6f, pz + 1.6f), blue, yellow);
                k.Cylinder(new Vector3(sx + 0.8f, 1.6f, pz + 0.8f), 1.1f, 0f, 0.9f, 4, red, null);
                k.Quad(new Vector3(sx + 0.3f, 1.6f, pz + 1.6f), new Vector3(sx + 0.3f, 0.12f, pz + 4.2f), new Vector3(sx + 1.3f, 0.12f, pz + 4.2f), new Vector3(sx + 1.3f, 1.6f, pz + 1.6f), teal);
                k.Box(new Vector3(px + 0.5f, 0.08f, pz + 3f), new Vector3(px + 2.5f, 0.4f, pz + 5f), S("concrete"), S("sand"));
            }
            Tree(k, -w / 2 + 1, -d / 2 + 1.5f, 0.85f, season, s);
        }

        static void College(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 8f);
            var pavers = S("pavers");
            k.Flat(-1.2f, -d / 2 + 0.2f, 1.2f, d / 2 - 8f, 0.07f, pavers, 3f);
            k.Flat(-w / 2 + 8f, -1.2f, w / 2 - 8f, 1.2f, 0.07f, pavers, 3f);
            var skin = new Skin { Upper = "fac_civic_brown", Floor = 3.15f, Wall = "wall_brick_brown", Coping = H("#d8cfbd"), Glow = 1.2f };
            var roof = RoofMat("roof_shingle", new Color(0.5f, 0.45f, 0.42f), season);
            var wall = S("wall_brick_brown");
            // three halls round a quad, with slate roofs
            float hw = 7f, hl = d - 6;
            float h1 = Block(k, -w / 2 + 1, -w / 2 + 1 + hw, -d / 2 + 3, d / 2 - 3, skin, 3, 0, 0, true, false, "roof_membrane", season);
            k.Push(new Vector3(-w / 2 + 1 + hw / 2, 0, 0), 90);
            k.Gable(-hl / 2, -hw / 2, hl / 2, hw / 2, h1 + 0.7f, 3f, roof, wall, 0.4f);
            k.Pop();
            float h2 = Block(k, w / 2 - 1 - hw, w / 2 - 1, -d / 2 + 3, d / 2 - 3, skin, 3, 1, 1, true, false, "roof_membrane", season);
            k.Push(new Vector3(w / 2 - 1 - hw / 2, 0, 0), 90);
            k.Gable(-hl / 2, -hw / 2, hl / 2, hw / 2, h2 + 0.7f, 3f, roof, wall, 0.4f);
            k.Pop();
            float nx0 = -w / 2 + 1 + hw, nx1 = w / 2 - 1 - hw, nz0 = d / 2 - 8, nz1 = d / 2 - 1;
            float h3 = Block(k, nx0, nx1, nz0, nz1, skin, 3, 2, 2, true, false, "roof_membrane", season);
            k.Gable(nx0, nz0, nx1, nz1, h3 + 0.7f, 3f, roof, wall, 0.2f);
            var front = Faces(nx0, nx1, nz0, nz1)[South];
            Rect(k, front, front.Width / 2 - 1.6f, front.Width / 2 + 1.6f, 0, 3.0f, S("gf_lobby", 1.2f), new Vector2(0.25f, 0), new Vector2(0.75f, 0.65f), 0.05f);
            Sign(k, front, front.Width / 2, 7f, 3.25f, 0.55f, SignRow.College, false);
            // the clock tower over the middle hall
            var stone = S("wall_stone");
            var white = C(H("#f4f4f2")); var dark = C(Dark);
            float tz = (nz0 + nz1) / 2;
            k.Box(new Vector3(-2, h3, tz - 2), new Vector3(2, h3 + 9, tz + 2), stone, stone, 2f);
            k.Box(new Vector3(-1.1f, h3 + 5.4f, tz - 2.08f), new Vector3(1.1f, h3 + 7.6f, tz - 2.0f), white, white);
            k.Box(new Vector3(-0.06f, h3 + 6.5f, tz - 2.12f), new Vector3(0.06f, h3 + 7.3f, tz - 2.08f), dark, dark);
            k.Box(new Vector3(-0.06f, h3 + 6.44f, tz - 2.12f), new Vector3(0.55f, h3 + 6.56f, tz - 2.08f), dark, dark);
            k.Hip(-2, tz - 2, 2, tz + 2, h3 + 9, 3.2f, season == 0 ? S("snow") : T("roof_metal", new Color(0.4f, 0.55f, 0.48f)), 0.3f);
            for (int i = 0; i < 4; i++) Tree(k, i % 2 == 0 ? -4.5f : 4.5f, i < 2 ? -9f : -4f, 1.0f, season, s + (uint)i);
            PropArt.Bench(k, new Vector3(-2.2f, 0.08f, -6.5f), -90);
            PropArt.Bench(k, new Vector3(2.2f, 0.08f, -6.5f), 90);
            PropArt.Lamp(k, new Vector3(1.6f, 0.08f, -d / 2 + 2));
        }

        static void Clinic(MeshKit k, float w, float d, ref uint s, int season, bool hospital)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var skin = new Skin { Upper = "fac_clinic", Floor = 3.15f, Ground = "gf_lobby", GroundH = 4.8f, Wall = "wall_stucco", Coping = H("#dfe3e6"), Glow = 1.4f };
            var lobby = S("gf_lobby", 1.4f);
            if (!hospital)
            {
                // 20 x 10
                float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 3, z1 = d / 2 - 0.6f;
                float top = Block(k, x0, x1, z0, z1, skin, 2, 0, 0, true, false, "roof_membrane", season);
                var f = Faces(x0, x1, z0, z1)[South];
                Ledge(k, f, f.Width / 2 - 2.5f, f.Width / 2 + 2.5f, 3.0f, 3.3f, 2.4f, C(H("#2e86de"), 0.4f), C(H("#e9ecef")));
                Rect(k, f, f.Width / 2 - 2.0f, f.Width / 2 + 2.0f, 0, 3.0f, lobby, new Vector2(0.25f, 0), new Vector2(0.75f, 0.65f), 0.05f);
                Sign(k, f, f.Width / 2, 4f, 3.4f, 0.45f, SignRow.Clinic, false);
                Cross(k, f, f.Width - 1.6f, top - 1.5f, 0.9f);
                uint rs = s;
                RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 1.2f, S(skin.Wall));
                PropArt.Bench(k, new Vector3(-w / 2 + 2.5f, 0.06f, -d / 2 + 1.4f), 0);
            }
            else
            {
                // 30 x 30: a two-storey podium with a ward tower rising out of it, a helipad on top
                float x0 = -w / 2 + 2, x1 = w / 2 - 2, z0 = -d / 2 + 5, z1 = d / 2 - 2;
                float pod = Block(k, x0, x1, z0, z1, skin, 2, 0, 0, true, false, "roof_membrane", season);
                float tx0 = -7, tx1 = 7, tz0 = z0 + 3, tz1 = z1 - 4;
                float top = Block(k, tx0, tx1, tz0, tz1, skin, 6, 1, 2, true, false, "roof_membrane", season);
                var f = Faces(x0, x1, z0, z1)[South];
                Ledge(k, f, f.Width / 2 - 4, f.Width / 2 + 4, 3.2f, 3.5f, 3f, C(H("#c0392b"), 0.4f), C(H("#e9ecef")));
                Rect(k, f, f.Width / 2 - 3.2f, f.Width / 2 + 3.2f, 0, 3.2f, lobby, new Vector2(0, 0), new Vector2(1, 0.7f), 0.05f);
                Sign(k, f, f.Width / 2, 5.5f, 3.6f, 0.55f, SignRow.Hospital, false);
                var tf = Faces(tx0, tx1, tz0, tz1)[South];
                Cross(k, tf, tf.Width / 2, top - 2.2f, 1.6f);
                var pad = C(H("#59626b"), 0.3f); var white = C(H("#f4f4f2"));
                float cz = (tz0 + tz1) / 2;
                k.Cylinder(new Vector3(0, top + 0.8f, cz), 4f, 4f, 0.2f, 16, pad, pad);
                k.Box(new Vector3(-1.2f, top + 1.0f, cz - 1.5f), new Vector3(-0.7f, top + 1.05f, cz + 1.5f), white, white);
                k.Box(new Vector3(0.7f, top + 1.0f, cz - 1.5f), new Vector3(1.2f, top + 1.05f, cz + 1.5f), white, white);
                k.Box(new Vector3(-0.7f, top + 1.0f, cz - 0.25f), new Vector3(0.7f, top + 1.05f, cz + 0.25f), white, white);
                uint rs = s;
                RoofClutter(k, x0 + 0.5f, tx0 - 0.5f, z0 + 0.5f, z1 - 0.5f, pod, ref rs, 1.5f, S(skin.Wall));
                Ambulance(k, new Vector3(-w / 2 + 3.5f, 0.06f, -d / 2 + 2.5f), 90);
                Ambulance(k, new Vector3(w / 2 - 3.5f, 0.06f, -d / 2 + 2.5f), -90);
                Tree(k, -w / 2 + 1.2f, d / 2 - 1.2f, 0.9f, season, s);
            }
        }

        static void Ambulance(MeshKit k, Vector3 p, float yaw)
        {
            k.Push(p, yaw, new Vector3(0.62f, 0.62f, 0.62f));
            PropArt.Truck(k, H("#f4f4f2"), H("#f4f4f2"));
            var red = C(H("#c0392b"), 0.4f);
            k.Box(new Vector3(-1.22f, 1.5f, -4.22f), new Vector3(1.22f, 1.9f, 1.52f), red, red);
            var bar = MaterialLibrary.Glow(new Color(0.8f, 0.2f, 0.2f), new Color(1f, 0.1f, 0.05f), 3f);
            k.Box(new Vector3(-0.9f, 2.4f, 2.0f), new Vector3(0.9f, 2.6f, 2.4f), bar, bar);
            k.Pop();
        }

        static void Cross(MeshKit k, Face f, float along, float y, float size)
        {
            var white = C(H("#ffffff"), 0.4f);
            var red = MaterialLibrary.Glow(new Color(0.85f, 0.15f, 0.12f), new Color(1f, 0.15f, 0.1f), 3f);
            Ledge(k, f, along - 1.1f * size, along + 1.1f * size, y - 1.1f * size, y + 1.1f * size, 0.12f, white);
            Ledge(k, f, along - 0.3f * size, along + 0.3f * size, y - 0.9f * size, y + 0.9f * size, 0.16f, red);
            Ledge(k, f, along - 0.9f * size, along + 0.9f * size, y - 0.3f * size, y + 0.3f * size, 0.16f, red);
        }

        static void Station(MeshKit k, float w, float d, ref uint s, int season, bool fire)
        {
            // 20 x 10
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var skin = new Skin { Upper = "fac_civic_brick", Floor = 3.15f, Wall = "wall_brick_red", Coping = H("#e2dccd"), Glow = 1.2f };
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 2.6f, z1 = d / 2 - 0.6f;
            float top = Block(k, x0, x1, z0, z1, skin, 2, 0, 0, true, false, "roof_membrane", season);
            var f = Faces(x0, x1, z0, z1)[South];
            var accent = C(fire ? H("#c0392b") : H("#1f3a68"), 0.4f);
            Ledge(k, f, -0.05f, f.Width + 0.05f, top - 0.9f, top - 0.4f, 0.12f, accent);
            if (fire)
            {
                // two engine bays with red doors, an engine out front
                var door = T("garage_door", new Color(0.85f, 0.25f, 0.22f));
                for (int i = 0; i < 2; i++)
                {
                    float a = 1.5f + i * 5.2f;
                    Ledge(k, f, a - 0.2f, a + 4.2f, 0, 4.6f, 0.04f, C(H("#e9e4d8"), 0.3f));
                    Rect(k, f, a, a + 4.0f, 0, 4.4f, door, new Vector2(0, 0), new Vector2(1, 1), 0.08f);
                }
                k.Push(new Vector3(x0 + 3.5f, 0.06f, -d / 2 + 1.35f), 90, new Vector3(0.9f, 0.9f, 0.9f));
                PropArt.Truck(k, H("#c0392b"), H("#e74c3c"));
                var bar = MaterialLibrary.Glow(new Color(0.8f, 0.2f, 0.2f), new Color(1f, 0.1f, 0.05f), 3f);
                k.Box(new Vector3(-0.9f, 2.4f, 2.2f), new Vector3(0.9f, 2.6f, 2.6f), bar, bar);
                k.Pop();
                Flag(k, x1 - 2.2f, -d / 2 + 0.6f, 8f, H("#2e86de"));
            }
            else
            {
                Rect(k, f, f.Width / 2 - 1.4f, f.Width / 2 + 1.4f, 0, 3.0f, S("gf_lobby", 1.3f), new Vector2(0.25f, 0), new Vector2(0.75f, 0.65f), 0.05f);
                Ledge(k, f, f.Width / 2 - 1.9f, f.Width / 2 + 1.9f, 3.0f, 3.25f, 1.4f, accent, C(H("#e9ecef")));
                // a police car: white with a blue stripe and a light bar
                k.Push(new Vector3(x1 - 3f, 0.06f, -d / 2 + 1.2f), 90);
                PropArt.Car(k, H("#f4f4f2"));
                var blue = C(H("#1f3a68"), 0.4f);
                k.Box(new Vector3(-0.92f, 0.55f, -1.4f), new Vector3(0.92f, 0.8f, 1.4f), blue, blue);
                var bar = MaterialLibrary.Glow(new Color(0.2f, 0.3f, 0.8f), new Color(0.2f, 0.4f, 1f), 3f);
                k.Box(new Vector3(-0.6f, 1.45f, -0.15f), new Vector3(0.6f, 1.62f, 0.15f), bar, bar);
                k.Pop();
                Flag(k, x0 + 0.4f, -d / 2 + 0.6f, 8f, H("#2e86de"));
            }
            Sign(k, f, fire ? f.Width - 3.0f : f.Width / 2, 4.2f, fire ? 4.7f : 3.45f, 0.45f, fire ? SignRow.Fire : SignRow.Police, false);
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 1f, S(skin.Wall));
        }

        static void Park(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 8f);
            var path = S("pavers");
            k.Flat(-1.1f, -d / 2 + 0.1f, 1.1f, d / 2 - 0.1f, 0.08f, path, 3f);
            k.Flat(-w / 2 + 0.1f, -1.1f, w / 2 - 0.1f, 1.1f, 0.08f, path, 3f);
            k.Cylinder(new Vector3(0, 0.06f, 0), 3.4f, 3.4f, 0.04f, 18, path, path);
            // a fountain: a stone rim round a pool, a column and an upper bowl
            var stone = S("wall_stone");
            var water = season == 0 ? S("snow") : S("water");
            k.Cylinder(new Vector3(0, 0.1f, 0), 2.6f, 2.6f, 0.6f, 18, stone, C(H("#cfc8b8")));
            k.Cylinder(new Vector3(0, 0.1f, 0), 2.3f, 2.3f, 0.63f, 18, stone, water);
            k.Cylinder(new Vector3(0, 0.6f, 0), 0.5f, 0.3f, 1.6f, 10, stone, stone);
            k.Cylinder(new Vector3(0, 2.2f, 0), 0.9f, 0.9f, 0.15f, 12, stone, water);
            for (int i = 0; i < 8; i++)
            {
                float a = i * 0.785f + 0.4f, r = 6.4f + (i % 2) * 1.4f;
                float x = Mathf.Cos(a) * r, z = Mathf.Sin(a) * r;
                if (Mathf.Abs(x) < 2.2f || Mathf.Abs(z) < 2.2f) continue;
                if (i % 3 == 2) PropArt.Pine(k, new Vector3(x, 0.06f, z), 0.9f, season);
                else Tree(k, x, z, 0.9f + (i % 3) * 0.15f, season, s + (uint)i);
            }
            PropArt.Bench(k, new Vector3(4.2f, 0.08f, -1.7f), 180);
            PropArt.Bench(k, new Vector3(-4.2f, 0.08f, 1.7f), 0);
            PropArt.Bench(k, new Vector3(1.7f, 0.08f, 4.2f), 90);
            PropArt.Bench(k, new Vector3(-1.7f, 0.08f, -4.2f), -90);
            PropArt.Lamp(k, new Vector3(1.6f, 0.08f, -d / 2 + 2));
            PropArt.Lamp(k, new Vector3(-1.6f, 0.08f, d / 2 - 2));
            PropArt.Lamp(k, new Vector3(w / 2 - 2, 0.08f, 1.6f));
            for (float x = -w / 2 + 1; x < w / 2 - 1; x += 1.2f) if (Mathf.Abs(x) > 1.8f) PropArt.Bush(k, new Vector3(x, 0.06f, -d / 2 + 0.6f), 0.6f, season);
        }

        /// <summary>Where a big store's front wall is: its car park is in front of it (rows at -4.1 and -14.7 from it, the aisle
        /// at -9.4).</summary>
        public static float StoreFrontZ(float d) { return -d / 2 + 17.6f; }

        /// <summary>The aisle cars drive along to and from their spots (NaN: no aisle, straight out to the street).</summary>
        public static float AisleZ(BuildingType t, float d)
        {
            if (t == BuildingType.ParkingLot) return LotAisleZ(d);
            if (t == BuildingType.DiscountStore || t == BuildingType.Supermarket || t == BuildingType.WarehouseClub) return StoreFrontZ(d) - 9.4f;
            return float.NaN;
        }

        /// <summary>A big grocery store: a long box behind its own car park, the entrance under a canopy in the middle, the
        /// chain's colours and name across the front. The warehouse club is bigger and plainer, with loading docks.</summary>
        static void BigStore(MeshKit k, float w, float d, ref uint s, bool closed, int season, BuildingType t)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            bool club = t == BuildingType.WarehouseClub, nice = t == BuildingType.Supermarket;
            float z0 = StoreFrontZ(d), z1 = d / 2 - 1f, x0 = -w / 2 + 1.5f, x1 = w / 2 - 1.5f, h = club ? 10f : 8f;
            Material wall, band, accent;
            int sign;
            if (club) { wall = T("metal_panel", new Color(0.72f, 0.75f, 0.78f)); band = C(H("#1f2a44"), 0.35f); accent = C(H("#ff7a1a"), 0.35f); sign = SignRow.Bulkworks; }
            else if (nice) { wall = T("wall_stucco", new Color(0.93f, 0.88f, 0.8f)); band = C(H("#b3202a"), 0.4f); accent = C(H("#f5f6fa"), 0.35f); sign = SignRow.Redwood; }
            else { wall = T("metal_panel", new Color(0.9f, 0.91f, 0.93f)); band = C(H("#1d4f91"), 0.35f); accent = C(H("#ffd23f"), 0.35f); sign = SignRow.ValleyValue; }
            var faces = Faces(x0, x1, z0, z1);
            var front = faces[South];
            float W = front.Width, g0 = W * 0.38f, g1 = W * 0.62f;
            var glass = closed ? MaterialLibrary.Dark("gf_store_light", new Color(0.7f, 0.7f, 0.7f)) : S("gf_store_light", 1.8f);
            Rect(k, front, g0, g1, 0, 4.2f, glass, new Vector2(0, 0), new Vector2(Mathf.Round((g1 - g0) / 2.7f) * 0.25f, 1));
            k.Quad(front.At(0, 0), front.At(0, h), front.At(g0, h), front.At(g0, 0), wall, 3f);
            k.Quad(front.At(g1, 0), front.At(g1, h), front.At(W, h), front.At(W, 0), wall, 3f);
            k.Quad(front.At(g0, 4.2f), front.At(g0, h), front.At(g1, h), front.At(g1, 4.2f), wall, 3f);
            for (int i = 0; i < 4; i++) if (i != South) Plain(k, faces[i], 0, h, wall, 2f);
            FlatRoof(k, x0, x1, z0, z1, h, season == 0 ? S("snow") : S("roof_membrane"), wall, band, 0.9f);
            // the chain's colours across the top, its name over the door, a canopy over the entrance
            Ledge(k, front, -0.1f, W + 0.1f, h - 1.6f, h + 0.9f, 0.25f, band);
            Ledge(k, front, -0.1f, W + 0.1f, h - 1.9f, h - 1.6f, 0.3f, accent);
            Sign(k, front, W / 2, Mathf.Min(16f, W * 0.55f), h - 1.45f, 1.3f, sign, closed);
            Ledge(k, front, g0 - 0.6f, g1 + 0.6f, 4.0f, 4.35f, 2.6f, band, accent);
            if (nice)
                foreach (float a in new[] { g0 - 0.3f, g1 + 0.3f })
                    k.Cylinder(front.At(a, 0) + front.Normal * 2.3f, 0.22f, 0.22f, 4.0f, 10, band, band);
            if (club)
            {
                // loading docks down the side
                var dock = closed ? MaterialLibrary.Dark("gf_rollup", new Color(0.75f, 0.75f, 0.75f)) : S("gf_rollup", 1.2f);
                var side = faces[East];
                for (float a = 4f; a < side.Width - 3f; a += 5f) Rect(k, side, a, a + 3.2f, 0.9f, 4.2f, dock, new Vector2(0, 0), new Vector2(1, 1), 0.04f);
            }
            uint rs = s;
            RoofClutter(k, x0 + 1f, x1 - 1f, z0 + 1f, z1 - 1f, h, ref rs, club ? 3f : 2.5f, wall);
            // the car park: bays in two rows, the way in through the front row's middle
            var paint = C(H("#f2f2ee"), 0.3f);
            float rb = z0 - 4.1f, rf = z0 - 14.7f;
            for (float x = -w / 2 + 3.0f; x < w / 2 - 1.6f; x += 2.6f)
            {
                k.Flat(x - 0.06f, rb - 2.5f, x + 0.06f, rb + 2.5f, 0.08f, paint);
                k.Flat(x - 0.06f, rf - 2.5f, x + 0.06f, rf + 2.5f, 0.08f, paint);
            }
            k.Flat(-w / 2 + 3.0f, rb - 2.56f, w / 2 - 3.0f, rb - 2.44f, 0.08f, paint);
            k.Flat(-w / 2 + 3.0f, rf + 2.44f, w / 2 - 3.0f, rf + 2.56f, 0.08f, paint);
            // a walkway along the front of the store, trolley bays, and trees at the street corners
            k.Flat(x0, rb + 2.6f, x1, z0, 0.1f, S("concrete"), 4f);
            var trolley = C(H("#9aa3ab"), 0.4f, 0.5f);
            // (at the ends of the aisle, past the last bays, where no car turns)
            foreach (float tx in new[] { -w / 2 + 2.6f, w / 2 - 2.6f })
                k.Box(new Vector3(tx - 0.35f, 0.06f, z0 - 10.6f), new Vector3(tx + 0.35f, 1.0f, z0 - 8.2f), trolley, accent);
            Tree(k, -w / 2 + 1.2f, -d / 2 + 1.2f, 0.85f, season, s);
            Tree(k, w / 2 - 1.2f, -d / 2 + 1.2f, 0.85f, season, s + 5);
            if (club)
            {
                // a pylon sign by the street
                float px = w / 2 - 3f, pz = -d / 2 + 1.5f;
                k.Box(new Vector3(px - 0.25f, 0, pz - 0.25f), new Vector3(px + 0.25f, 6f, pz + 0.25f), band, band);
                k.Box(new Vector3(px - 1.6f, 6f, pz - 0.3f), new Vector3(px + 1.6f, 7.6f, pz + 0.3f), accent, band);
            }
        }

        // a public car park: where its rows are, front (by the street) and back, and the aisle between
        public static float LotFrontZ(float d) { return -d / 2 + 3.2f; }
        public static float LotBackZ(float d) { return d / 2 - 5.8f; }
        public static float LotAisleZ(float d) { return -d / 2 + 8.7f; }

        /// <summary>A public car park: two rows of bays either side of an aisle, the way in through the front row, trees at
        /// the back, a P sign by the street. The cars are parked in it by the view (ParkingSpots).</summary>
        static void ParkingLot(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var paint = C(H("#f2f2ee"), 0.3f);
            float f0 = LotFrontZ(d) - 2.5f, f1 = LotFrontZ(d) + 2.5f, b0 = LotBackZ(d) - 2.5f, b1 = LotBackZ(d) + 2.5f;
            for (int i = 0; i <= 7; i++)
            {
                float x = -9.1f + i * 2.6f;
                k.Flat(x - 0.06f, f0, x + 0.06f, f1, 0.08f, paint);
                k.Flat(x - 0.06f, b0, x + 0.06f, b1, 0.08f, paint);
            }
            // the back of each row, and arrows down the way in
            k.Flat(-9.1f, f1 - 0.06f, -1.3f, f1 + 0.06f, 0.08f, paint);
            k.Flat(1.3f, f1 - 0.06f, 9.1f, f1 + 0.06f, 0.08f, paint);
            k.Flat(-9.1f, b0 - 0.06f, 9.1f, b0 + 0.06f, 0.08f, paint);
            k.Flat(-0.08f, f0 + 0.6f, 0.08f, f1 - 0.6f, 0.08f, paint);
            // a planted strip along the back with a few trees, and lamps
            float g0 = b1 + 0.3f;
            if (g0 < d / 2 - 0.4f)
            {
                k.Box(new Vector3(-w / 2 + 0.3f, 0, g0), new Vector3(w / 2 - 0.3f, 0.18f, d / 2 - 0.3f), C(H("#9aa3ab"), 0.2f), GrassMat(season), 2f);
                for (int i = 0; i < 3; i++) Tree(k, -6f + i * 6f, (g0 + d / 2) / 2, 0.7f, season, s + (uint)i * 17);
                PropArt.Lamp(k, new Vector3(-3f, 0.18f, (g0 + d / 2) / 2));
                PropArt.Lamp(k, new Vector3(3f, 0.18f, (g0 + d / 2) / 2));
            }
            // the sign by the street
            var pole = C(H("#3d4248"), 0.45f, 0.5f);
            var blue = C(H("#2e86de"), 0.4f);
            var white = C(H("#f5f6fa"), 0.3f);
            float sx = w / 2 - 1.2f, sz = -d / 2 + 0.7f;
            k.Box(new Vector3(sx - 0.06f, 0, sz - 0.06f), new Vector3(sx + 0.06f, 2.9f, sz + 0.06f), pole, pole);
            k.Box(new Vector3(sx - 0.45f, 2.1f, sz - 0.05f), new Vector3(sx + 0.45f, 3.0f, sz + 0.05f), blue, blue);
            k.Box(new Vector3(sx - 0.18f, 2.3f, sz - 0.07f), new Vector3(sx - 0.06f, 2.8f, sz + 0.07f), white, white);
            k.Box(new Vector3(sx - 0.06f, 2.56f, sz - 0.07f), new Vector3(sx + 0.16f, 2.8f, sz + 0.07f), white, white);
        }

        static void BusDepot(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var skin = new Skin { Upper = "fac_metal_grey", Floor = 4.5f, Bay = 3.2f, Ground = "gf_rollup", GroundH = 4.7f, GroundBay = 3.2f, Wall = "metal_panel", Coping = H("#8d969e"), Glow = 1.2f };
            float top = Block(k, -w / 2 + 1, w / 2 - 1, d / 2 - 7, d / 2 - 1, skin, 1, 0, 0, true, true, "roof_membrane", season);
            var f = Faces(-w / 2 + 1, w / 2 - 1, d / 2 - 7, d / 2 - 1)[South];
            Sign(k, f, f.Width / 2, 6f, top - 2.0f, 0.55f, SignRow.BusDepot, false);
            // a canopy over the bays, lit from below
            var blue = C(H("#2980b9"), 0.4f);
            var post = C(H("#9aa3ab"), 0.5f, 0.6f);
            float cz0 = -d / 2 + 1, cz1 = 1;
            k.Box(new Vector3(-w / 2 + 1, 4.4f, cz0), new Vector3(w / 2 - 1, 4.8f, cz1), blue, season == 0 ? S("snow") : C(H("#e1e8ed")));
            for (float x = -w / 2 + 1.5f; x < w / 2; x += 6)
                foreach (float z in new[] { cz0, cz1 - 0.25f })
                    k.Box(new Vector3(x, 0.06f, z), new Vector3(x + 0.25f, 4.4f, z + 0.25f), post, post);
            var under = MaterialLibrary.Glow(new Color(0.9f, 0.9f, 0.9f), new Color(1f, 0.95f, 0.85f), 1.8f);
            k.Quad(new Vector3(-w / 2 + 1.1f, 4.39f, cz1 - 0.1f), new Vector3(-w / 2 + 1.1f, 4.39f, cz0 + 0.1f), new Vector3(w / 2 - 1.1f, 4.39f, cz0 + 0.1f), new Vector3(w / 2 - 1.1f, 4.39f, cz1 - 0.1f), under);
            for (int i = 0; i < 3; i++) { k.Push(new Vector3(-6 + i * 6, 0.06f, -d / 2 + 6.5f), 0); PropArt.Bus(k, i == 1 ? H("#f1c40f") : H("#2980b9")); k.Pop(); }
        }

        static void Terminal(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var skin = new Skin { Upper = "fac_metal_grey", Floor = 4.5f, Bay = 3.2f, Ground = "gf_rollup", GroundH = 4.7f, GroundBay = 3.2f, Wall = "metal_panel", Coping = H("#8d969e"), Glow = 1.2f };
            float top = Block(k, -w / 2 + 1, w / 2 - 1, 0, d / 2 - 1, skin, 1, 0, 0, true, true, "roof_membrane", season);
            var f = Faces(-w / 2 + 1, w / 2 - 1, 0, d / 2 - 1)[South];
            Sign(k, f, f.Width / 2, 8f, top - 2.0f, 0.72f, SignRow.Logistics, false);
            // stacked shipping containers
            Color[] c = { new Color(0.75f, 0.22f, 0.17f), new Color(0.16f, 0.5f, 0.73f), new Color(0.15f, 0.68f, 0.38f), new Color(0.95f, 0.6f, 0.07f), new Color(0.55f, 0.27f, 0.68f) };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 1 + i % 2; j++)
                {
                    var m = T("metal_panel", c[(i + j) % 5]);
                    k.Box(new Vector3(-w / 2 + 1.5f + i * 3.2f, 0.06f + j * 2.6f, -d / 2 + 1.5f), new Vector3(-w / 2 + 4.0f + i * 3.2f, 2.66f + j * 2.6f, -d / 2 + 8.0f), m, m, 1f);
                }
            // the highway sign
            var pole = C(H("#9aa3ab"), 0.5f, 0.6f);
            k.Box(new Vector3(w / 2 - 1.3f, 0.06f, -d / 2 + 1), new Vector3(w / 2 - 1.1f, 5.5f, -d / 2 + 1.2f), pole, pole);
            k.Box(new Vector3(w / 2 - 5.6f, 0.06f, -d / 2 + 1), new Vector3(w / 2 - 5.4f, 5.5f, -d / 2 + 1.2f), pole, pole);
            var green = C(H("#1e7a3c"), 0.4f);
            k.Box(new Vector3(w / 2 - 6.2f, 4.2f, -d / 2 + 0.85f), new Vector3(w / 2 - 0.5f, 6.4f, -d / 2 + 1.0f), green, green);
            var white = C(H("#f4f4f2"));
            k.Box(new Vector3(w / 2 - 6.0f, 4.35f, -d / 2 + 0.83f), new Vector3(w / 2 - 0.7f, 4.42f, -d / 2 + 0.85f), white, white);
            k.Box(new Vector3(w / 2 - 6.0f, 6.18f, -d / 2 + 0.83f), new Vector3(w / 2 - 0.7f, 6.25f, -d / 2 + 0.85f), white, white);
            k.Box(new Vector3(w / 2 - 5.0f, 5.0f, -d / 2 + 0.83f), new Vector3(w / 2 - 1.7f, 5.5f, -d / 2 + 0.85f), white, white);
            if ((s & 2u) == 0) { k.Push(new Vector3(w / 2 - 3.5f, 0.06f, -4.5f), 180); PropArt.Truck(k, H("#c0392b"), H("#dfe4e8")); k.Pop(); }
        }

        // ================================================================== building sites
        /// <summary>A site under construction: footings, a frame rising with the work done, scaffolding, a fence, a crane on big jobs.</summary>
        public static Model Site(float w, float d, float progress, float height, bool started)
        {
            var k = new MeshKit();
            Lot(k, w, d, S("dirt"), 0.1f, 6f);
            var concrete = S("concrete");
            var steel = C(H("#9aa3ab"), 0.4f, 0.5f);
            var plank = T("wood", new Color(0.9f, 0.8f, 0.6f));
            if (started)
            {
                k.Box(new Vector3(-w / 2 + 1, 0.06f, -d / 2 + 1.5f), new Vector3(w / 2 - 1, 0.5f, d / 2 - 1), concrete, concrete, 2f);
                float top = Mathf.Max(0.6f, height * progress);
                const float fh = 3.2f;
                float stepX = Mathf.Max(2.5f, (w - 2.4f) / 4), stepZ = Mathf.Max(2.5f, (d - 2.6f) / 3);
                for (float x = -w / 2 + 1.2f; x <= w / 2 - 1.1f; x += stepX)
                    for (float z = -d / 2 + 1.7f; z <= d / 2 - 1.1f; z += stepZ)
                        k.Box(new Vector3(x - 0.18f, 0.5f, z - 0.18f), new Vector3(x + 0.18f, top, z + 0.18f), concrete, concrete);
                for (float y = fh; y < top; y += fh)
                    k.Box(new Vector3(-w / 2 + 1, y, -d / 2 + 1.5f), new Vector3(w / 2 - 1, y + 0.25f, d / 2 - 1), concrete, concrete, 2f);
                if (progress > 0.6f)
                    k.Box(new Vector3(-w / 2 + 1.05f, 0.5f, -d / 2 + 1.55f), new Vector3(w / 2 - 1.05f, Mathf.Min(top, height * (progress - 0.5f) * 2), d / 2 - 1.05f), S("wall_brick_tan"), concrete, 2f);
                // scaffolding on the front
                if (top > 3)
                {
                    for (float x = -w / 2 + 1; x <= w / 2 - 1; x += 2f)
                        k.Box(new Vector3(x - 0.04f, 0.06f, -d / 2 + 0.6f), new Vector3(x + 0.04f, top + 1, -d / 2 + 0.68f), steel, steel);
                    for (float y = 2; y < top + 1; y += 2f)
                        k.Box(new Vector3(-w / 2 + 1, y, -d / 2 + 0.6f), new Vector3(w / 2 - 1, y + 0.08f, -d / 2 + 1.4f), plank, plank);
                }
                if (w * d >= 350)
                {
                    var yellow = C(H("#f1c40f"), 0.4f, 0.3f);
                    float cx = w / 2 - 1.5f, cz = d / 2 - 1.5f, ch = height + 8;
                    for (float y = 0; y < ch; y += 2) k.Box(new Vector3(cx - 0.45f, y, cz - 0.45f), new Vector3(cx + 0.45f, y + 1.8f, cz + 0.45f), yellow, yellow);
                    k.Box(new Vector3(cx - w * 0.9f, ch, cz - 0.35f), new Vector3(cx + 3, ch + 0.7f, cz + 0.35f), yellow, yellow);
                    k.Box(new Vector3(cx + 1.2f, ch - 1, cz - 0.9f), new Vector3(cx + 3, ch, cz + 0.9f), concrete, concrete);
                }
            }
            else
            {
                // surveyed and waiting for a builder: corner stakes, a pile of lumber, a portable toilet
                for (int i = 0; i < 3; i++) k.Box(new Vector3(-3 + i * 2.2f, 0.06f, -1), new Vector3(-1.6f + i * 2.2f, 0.9f, 1.5f), plank, plank, 2f);
                var stake = C(H("#e67e22"), 0.3f);
                foreach (var p in new[] { new Vector2(-w / 2 + 1.5f, -d / 2 + 1.5f), new Vector2(w / 2 - 1.5f, -d / 2 + 1.5f), new Vector2(-w / 2 + 1.5f, d / 2 - 1.5f), new Vector2(w / 2 - 1.5f, d / 2 - 1.5f) })
                    k.Box(new Vector3(p.x - 0.05f, 0.06f, p.y - 0.05f), new Vector3(p.x + 0.05f, 1.0f, p.y + 0.05f), stake, stake);
                var blue = C(H("#2471a3"), 0.3f);
                k.Box(new Vector3(-w / 2 + 1.0f, 0.06f, d / 2 - 2.6f), new Vector3(-w / 2 + 2.2f, 2.4f, d / 2 - 1.4f), blue, blue);
            }
            // a site fence, open at the gate
            var f = C(H("#e67e22"), 0.3f);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.06f, -d / 2 + 0.3f), new Vector3(-1.5f, 1.3f, -d / 2 + 0.4f), f, f);
            k.Box(new Vector3(1.5f, 0.06f, -d / 2 + 0.3f), new Vector3(w / 2 - 0.3f, 1.3f, -d / 2 + 0.4f), f, f);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.06f, d / 2 - 0.4f), new Vector3(w / 2 - 0.3f, 1.3f, d / 2 - 0.3f), f, f);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.06f, -d / 2 + 0.3f), new Vector3(-w / 2 + 0.4f, 1.3f, d / 2 - 0.3f), f, f);
            k.Box(new Vector3(w / 2 - 0.4f, 0.06f, -d / 2 + 0.3f), new Vector3(w / 2 - 0.3f, 1.3f, d / 2 - 0.3f), f, f);
            return k.ToModel("site");
        }

        /// <summary>About how tall a finished building of this type stands, for the frame on its site.</summary>
        public static float Height(BuildingType t, int floors)
        {
            switch (t)
            {
                case BuildingType.House: return 6;
                case BuildingType.Apartments: return 17;
                case BuildingType.Office: return 4.8f + (floors + 1) * 3.15f;
                case BuildingType.Hospital: return 19;
                case BuildingType.College:
                case BuildingType.TownHall:
                case BuildingType.HighSchool: return 9.5f;
                case BuildingType.Park: return 0.6f;
                case BuildingType.ParkingLot: return 0.5f;
                case BuildingType.Farm: return 5;
                case BuildingType.FoodPlant:
                case BuildingType.GarmentFactory:
                case BuildingType.ElectronicsPlant:
                case BuildingType.Trucking:
                case BuildingType.BusDepot: return 9.2f;
                default: return 6.3f;
            }
        }
    }
}
