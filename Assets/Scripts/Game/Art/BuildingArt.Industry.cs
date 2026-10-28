using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>The farms, mills, plants, factories and workshops added later, each its own model.</summary>
    public static partial class BuildingArt
    {
        static int Row(BuildingType t) { return SignRows.For(t); }

        static void Truck(MeshKit k, float x, float z, float yaw, Color32 box, bool closed)
        {
            if (closed) return;
            k.Push(new Vector3(x, 0.06f, z), yaw);
            PropArt.Truck(k, H("#ecf0f1"), box);
            k.Pop();
        }

        // ================================================================== farms
        static void DairyFarm(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            k.Flat(-w / 2 + 1, -d / 2 + 0.5f, 3, 2, 0.07f, S("gravel"), 4f);
            // the long milking barn: white board walls, a dark roof
            var wall = T("wall_board", new Color(0.95f, 0.95f, 0.93f));
            var f = GableBox(k, -w / 2 + 2, 4, -d / 2 + 2.5f, -d / 2 + 11.5f, 4.2f, 2.6f, wall, T("roof_metal", new Color(0.32f, 0.36f, 0.42f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 2.0f, 1.0f, 0.8f, 3.2f, ref s, closed);
            Door(k, f[East], f[East].Width / 2, H("#1f3a68"), 2.6f, 3.2f);
            Sign(k, f[South], f[South].Width / 2, 6f, 4.4f, 0.7f, Row(BuildingType.DairyFarm), closed);
            // tall blue silos and the milk tank
            var blue = C(H("#1f4e79"), 0.5f, 0.3f); var cap = C(H("#cfd5db"), 0.6f, 0.6f);
            Silo(k, 7.5f, -d / 2 + 4.5f, 2.0f, 12f, blue, cap);
            Silo(k, 11.5f, -d / 2 + 4.5f, 2.0f, 12f, blue, cap);
            var steel = C(H("#d7dde2"), 0.7f, 0.8f);
            Tank(k, 7.5f, -d / 2 + 9.5f, 1.3f, 2.6f, steel, steel);
            // the pasture and the herd
            var fence = C(H("#e8e2d4"), 0.2f);
            float pz0 = -1f, pz1 = d / 2 - 1f;
            k.Box(new Vector3(-w / 2 + 1, 0.06f, pz0), new Vector3(w / 2 - 1, 1.1f, pz0 + 0.12f), fence, fence);
            k.Box(new Vector3(-w / 2 + 1, 0.06f, pz1 - 0.12f), new Vector3(w / 2 - 1, 1.1f, pz1), fence, fence);
            k.Box(new Vector3(-w / 2 + 1, 0.06f, pz0), new Vector3(-w / 2 + 1.12f, 1.1f, pz1), fence, fence);
            k.Box(new Vector3(w / 2 - 1.12f, 0.06f, pz0), new Vector3(w / 2 - 1, 1.1f, pz1), fence, fence);
            if (season != 0)
                for (int i = 0; i < 7; i++)
                {
                    float x = Range(ref s, -w / 2 + 3, w / 2 - 3), z = Range(ref s, pz0 + 2, pz1 - 2);
                    Cow(k, x, z, Range(ref s, 0, 360), i % 3 == 0 ? H("#2b2b2b") : H("#f2f2ee"));
                }
            Tree(k, w / 2 - 2, -d / 2 + 2, 1f, season, s);
        }

        static void CattleRanch(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            // the corral: trampled dirt in a timber fence
            float cx0 = -w / 2 + 2, cx1 = w / 2 - 2, cz0 = -2, cz1 = d / 2 - 2;
            k.Flat(cx0, cz0, cx1, cz1, 0.07f, season == 0 ? S("snow") : S("dirt"), 5f);
            var rail = T("wood", new Color(0.55f, 0.42f, 0.3f));
            foreach (float y in new[] { 0.5f, 1.1f })
            {
                k.Box(new Vector3(cx0, y, cz0), new Vector3(cx1, y + 0.15f, cz0 + 0.12f), rail, rail);
                k.Box(new Vector3(cx0, y, cz1 - 0.12f), new Vector3(cx1, y + 0.15f, cz1), rail, rail);
                k.Box(new Vector3(cx0, y, cz0), new Vector3(cx0 + 0.12f, y + 0.15f, cz1), rail, rail);
                k.Box(new Vector3(cx1 - 0.12f, y, cz0), new Vector3(cx1, y + 0.15f, cz1), rail, rail);
            }
            for (float x = cx0; x <= cx1; x += 3f)
            {
                k.Box(new Vector3(x - 0.1f, 0.06f, cz0 - 0.1f), new Vector3(x + 0.1f, 1.4f, cz0 + 0.1f), rail, rail);
                k.Box(new Vector3(x - 0.1f, 0.06f, cz1 - 0.1f), new Vector3(x + 0.1f, 1.4f, cz1 + 0.1f), rail, rail);
            }
            for (int i = 0; i < 9; i++)
                Cow(k, Range(ref s, cx0 + 2, cx1 - 2), Range(ref s, cz0 + 2, cz1 - 2), Range(ref s, 0, 360), i % 2 == 0 ? H("#5b3a29") : H("#8a5a3b"));
            // a pole barn of hay, a trough, the ranch house
            var post = T("wood", new Color(0.5f, 0.38f, 0.28f));
            float bx0 = -w / 2 + 2, bx1 = -w / 2 + 12, bz0 = -d / 2 + 2, bz1 = -d / 2 + 9;
            foreach (var p in new[] { new Vector2(bx0, bz0), new Vector2(bx1, bz0), new Vector2(bx0, bz1), new Vector2(bx1, bz1) })
                k.Box(new Vector3(p.x - 0.15f, 0.06f, p.y - 0.15f), new Vector3(p.x + 0.15f, 4.2f, p.y + 0.15f), post, post);
            k.Gable(bx0, bz0, bx1, bz1, 4.2f, 1.6f, Snowy(T("roof_metal", new Color(0.62f, 0.3f, 0.24f)), season), post, 0.4f);
            var hay = C(H("#d9b65a"), 0.1f);
            for (int i = 0; i < 4; i++) k.Box(new Vector3(bx0 + 1 + i * 2.2f, 0.06f, bz0 + 1.5f), new Vector3(bx0 + 2.8f + i * 2.2f, 1.6f, bz1 - 1.5f), hay, hay);
            k.Box(new Vector3(cx1 - 6, 0.06f, cz0 + 1), new Vector3(cx1 - 2, 0.7f, cz0 + 1.8f), C(H("#7d8b94"), 0.5f, 0.5f), C(H("#3d6b8c"), 0.8f));
            var hf = GableBox(k, 3, 11, -d / 2 + 2, -d / 2 + 8, 3.2f, 2f, T("wall_board", new Color(0.78f, 0.62f, 0.45f)), T("roof_shingle", new Color(0.45f, 0.42f, 0.4f)), season);
            WindowRow(k, hf[South], 1, hf[South].Width - 1, 1.2f, 1.0f, 1.1f, 2.4f, ref s, closed);
            // the gate arch with the brand
            var arch = T("wood", new Color(0.45f, 0.34f, 0.25f));
            k.Box(new Vector3(-w / 2 + 13, 0.06f, -d / 2 + 0.8f), new Vector3(-w / 2 + 13.3f, 4.6f, -d / 2 + 1.1f), arch, arch);
            k.Box(new Vector3(-w / 2 + 19.7f, 0.06f, -d / 2 + 0.8f), new Vector3(-w / 2 + 20, 4.6f, -d / 2 + 1.1f), arch, arch);
            var gf = new Face { LeftBottom = new Vector3(-w / 2 + 13, 0, -d / 2 + 0.75f), Right = Vector3.right, Normal = Vector3.back, Width = 7f };
            Sign(k, gf, 3.5f, 6.4f, 3.7f, 0.8f, Row(BuildingType.CattleRanch), closed);
        }

        static void PoultryFarm(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            var wall = T("metal_panel", new Color(0.95f, 0.95f, 0.93f));
            var roof = T("roof_metal", new Color(0.3f, 0.5f, 0.36f));
            var steel = C(H("#c9ced3"), 0.6f, 0.7f);
            for (int i = 0; i < 2; i++)
            {
                float z0 = -d / 2 + 4 + i * 11, z1 = z0 + 7;
                var f = GableBox(k, -w / 2 + 2, w / 2 - 6, z0, z1, 2.8f, 1.4f, wall, roof, season, 0.3f);
                // vents along the side
                for (float a = 1.5f; a < f[South].Width - 1; a += 3f) Ledge(k, f[South], a, a + 0.9f, 1.0f, 1.9f, 0.08f, C(H("#4a4f55"), 0.3f));
                // feed bins by each house
                Silo(k, w / 2 - 3.5f, (z0 + z1) / 2, 1.1f, 5f, steel, steel, 1.2f);
                k.Box(new Vector3(w / 2 - 6, 3.6f, (z0 + z1) / 2 - 0.15f), new Vector3(w / 2 - 3.5f, 3.9f, (z0 + z1) / 2 + 0.15f), steel, steel);
            }
            Pylon(k, -w / 2 + 6, -d / 2 + 1.5f, 1.6f, 6f, Row(BuildingType.PoultryFarm), closed);
            Truck(k, 4, -d / 2 + 1.6f, 90, H("#f2e3b3"), closed);
            Tree(k, w / 2 - 2, d / 2 - 2, 1f, season, s);
            Tree(k, -w / 2 + 2, d / 2 - 2, 0.9f, season, s + 7);
        }

        static void TreeFarm(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            // rows of young and grown pines
            for (float z = -2; z < d / 2 - 1.5f; z += 3.4f)
                for (float x = -w / 2 + 2; x < w / 2 - 1; x += 3.2f)
                {
                    float sc = 0.6f + 0.6f * R(ref s);
                    PropArt.Pine(k, new Vector3(x + Range(ref s, -0.4f, 0.4f), 0.06f, z), sc, season);
                }
            // the landing: logs stacked, a loader, a cabin
            k.Flat(-w / 2 + 1, -d / 2 + 0.5f, w / 2 - 1, -4, 0.07f, season == 0 ? S("snow") : S("dirt"), 5f);
            Timber(k, -w / 2 + 2, -d / 2 + 5, -w / 2 + 12, -d / 2 + 8, 2.2f);
            Timber(k, -w / 2 + 2, -d / 2 + 9, -w / 2 + 10, -d / 2 + 11.5f, 1.6f);
            var cab = GableBox(k, 4, 10, -d / 2 + 4, -d / 2 + 9, 3f, 2f, T("wood", new Color(0.62f, 0.45f, 0.3f)), T("roof_metal", new Color(0.35f, 0.42f, 0.36f)), season);
            WindowRow(k, cab[South], 1, cab[South].Width - 1, 1.2f, 0.9f, 1f, 2.4f, ref s, closed);
            Sign(k, cab[South], cab[South].Width / 2, 4.6f, 3.1f, 0.55f, Row(BuildingType.TreeFarm), closed);
            if (!closed)
            {
                // a log loader
                var yel = C(H("#f1c40f"), 0.4f); var dark = C(H("#2b2b2b"), 0.3f);
                k.Push(new Vector3(-2, 0.06f, -d / 2 + 4), 30);
                k.Box(new Vector3(-1.2f, 0, -2f), new Vector3(1.2f, 1.2f, 2f), dark, dark);
                k.Box(new Vector3(-1f, 1.2f, -1.2f), new Vector3(1f, 2.6f, 1f), yel, yel);
                k.Box(new Vector3(-0.2f, 1.8f, -4.5f), new Vector3(0.2f, 2.2f, -1.2f), yel, yel);
                k.Pop();
            }
        }

        static void Orchard(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            var fruit = (s & 1u) == 0 ? H("#c0392b") : H("#e2a33a");
            for (float z = -d / 2 + 11; z < d / 2 - 1; z += 4f)
                for (float x = -w / 2 + 2.5f; x < w / 2 - 1; x += 4f)
                    FruitTree(k, x + ((int)(z) % 2 == 0 ? 0 : 1.5f), z, season, fruit);
            // the packing shed and crates of fruit
            var f = GableFront(k, -w / 2 + 2, -w / 2 + 11, -d / 2 + 2, -d / 2 + 9, 3.6f, 2.4f, T("wall_board", new Color(0.72f, 0.3f, 0.24f)), T("roof_metal", new Color(0.8f, 0.8f, 0.78f)), season);
            Door(k, f[South], f[South].Width / 2, H("#f2efe6"), 2.6f, 3f);
            var crate = T("wood", new Color(0.9f, 0.78f, 0.58f));
            Crates(k, -w / 2 + 12.5f, -d / 2 + 2.5f, 6, crate, ref s);
            if (season >= 2) for (int i = 0; i < 3; i++) k.Flat(-w / 2 + 12.7f + i * 1.25f, -d / 2 + 2.7f, -w / 2 + 13.4f + i * 1.25f, -d / 2 + 3.4f, 1.08f, fruit);
            Pylon(k, 6, -d / 2 + 1.5f, 1.6f, 6f, Row(BuildingType.Orchard), closed);
        }

        static void Vineyard(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("dirt"), 0.05f, 6f);
            Rows(k, -w / 2 + 1.5f, -2, w / 2 - 1.5f, d / 2 - 1, 2.4f, 1.6f, H("#4f7f2f"), season, true);
            // the tasting house: stucco, a terracotta roof, cypresses
            var wall = T("wall_stucco", new Color(0.95f, 0.88f, 0.74f));
            var f = GableBox(k, -w / 2 + 2, -w / 2 + 14, -d / 2 + 2.5f, -d / 2 + 9.5f, 3.6f, 2.2f, wall, T("roof_shingle", new Color(0.8f, 0.45f, 0.3f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.3f, 1.0f, 1.4f, 2.8f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 5f, 3.7f, 0.6f, Row(BuildingType.Vineyard), closed);
            Barrels(k, -w / 2 + 15, -d / 2 + 3, 4, T("wood", new Color(0.6f, 0.4f, 0.26f)));
            for (int i = 0; i < 4; i++) PropArt.Pine(k, new Vector3(w / 2 - 2.5f - i * 3f, 0.06f, -d / 2 + 2f), 0.9f, season == 0 ? 0 : 2);
        }

        static void PlantNursery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            Greenhouse(k, -w / 2 + 2, w / 2 - 2, 2, 9, 3.2f, closed);
            Greenhouse(k, -w / 2 + 2, w / 2 - 2, 10.5f, d / 2 - 1.5f, 3.2f, closed);
            // rows of pots out front
            var pot = C(H("#b5651d"), 0.2f);
            Color32[] bloom = { H("#e74c3c"), H("#f1c40f"), H("#9b59b6"), H("#e84393"), H("#ecf0f1") };
            for (float z = -d / 2 + 6; z < 0.5f; z += 1.6f)
                for (float x = 1; x < w / 2 - 2; x += 1.3f)
                {
                    k.Cylinder(new Vector3(x, 0.06f, z), 0.3f, 0.36f, 0.4f, 6, pot, pot);
                    if (season != 0) k.Blob(new Vector3(x, 0.65f, z), new Vector3(0.32f, 0.25f, 0.32f), bloom[(int)(Next(ref s) % 5)]);
                }
            var shed = Storefront(k, -w / 2 + 2, -w / 2 + 11, -d / 2 + 4, -d / 2 + 9.5f, 4f, T("wall_board", new Color(0.36f, 0.55f, 0.38f)), "gf_store_wood", closed, season, Row(BuildingType.PlantNursery), C(H("#e8f5e0")));
            Truck(k, w / 2 - 4, -d / 2 + 1.6f, 90, H("#2e7d32"), closed);
        }

        // ================================================================== mills and plants
        static void Sawmill(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("dirt"), 0.05f, 5f);
            // the saw shed: open sides, a long roof on posts, the head rig inside
            var post = C(H("#5d4c3c"), 0.3f);
            float x0 = -w / 2 + 2, x1 = 6, z0 = -2, z1 = d / 2 - 2;
            for (float x = x0; x <= x1 + 0.01f; x += (x1 - x0) / 4)
                foreach (float z in new[] { z0, z1 })
                    k.Box(new Vector3(x - 0.15f, 0.06f, z - 0.15f), new Vector3(x + 0.15f, 5f, z + 0.15f), post, post);
            k.Gable(x0, z0, x1, z1, 5f, 2.2f, Snowy(T("roof_metal", new Color(0.55f, 0.3f, 0.24f)), season), post, 0.5f);
            k.Box(new Vector3(x0 + 2, 0.06f, z0 + 2.5f), new Vector3(x1 - 2, 1.2f, z1 - 2.5f), C(H("#7d8b94"), 0.4f, 0.4f), C(H("#5b6670"), 0.4f, 0.4f));
            k.Cylinder(new Vector3(-3, 1.2f, (z0 + z1) / 2), 1.1f, 1.1f, 0.12f, 14, C(H("#cfd5db"), 0.7f, 0.9f), C(H("#cfd5db"), 0.7f, 0.9f));
            // the office in front with the sign, the log deck and the lumber
            var off = Storefront(k, -w / 2 + 2, -w / 2 + 10, -d / 2 + 1.5f, -d / 2 + 6.5f, 3.8f, T("wood", new Color(0.7f, 0.52f, 0.34f)), "gf_store_wood", closed, season, Row(BuildingType.Sawmill), C(H("#6d4c2f")));
            Timber(k, 8, 0, w / 2 - 2, 3, 2.4f);
            Timber(k, 8, 4.5f, w / 2 - 2, 7.5f, 1.8f);
            var lumber = T("wood", new Color(0.95f, 0.84f, 0.64f));
            for (int i = 0; i < 3; i++) k.Box(new Vector3(-1 + i * 3.4f, 0.06f, -d / 2 + 2), new Vector3(1.6f + i * 3.4f, 1.4f + 0.3f * i, -d / 2 + 6), lumber, lumber, 1f);
            Pile(k, w / 2 - 4, -d / 2 + 4, 2.2f, 2.4f, C(H("#c8a878"), 0.1f));
            Conveyor(k, new Vector3(x1, 0, (z0 + z1) / 2), new Vector3(w / 2 - 4, 0, -d / 2 + 4), 2.0f, 3.2f);
        }

        static void PaperMill(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var skin = new Skin { Upper = "fac_tiltup", Floor = 4.5f, Bay = 3.2f, Ground = "gf_rollup", GroundH = 4.7f, GroundBay = 3.2f, Wall = "metal_panel", Coping = H("#7a8590"), Glow = 1.3f };
            float x0 = -w / 2 + 2, x1 = w / 2 - 8, z0 = -d / 2 + 6, z1 = d / 2 - 2;
            float top = Block(k, x0, x1, z0, z1, skin, 2, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            Sign(k, faces[South], faces[South].Width / 2, 9f, top - 2.4f, 0.9f, Row(BuildingType.PaperMill), closed);
            // the machine hall's high bay and its stacks
            k.Box(new Vector3(x0 + 3, top, z0 + 4), new Vector3(x1 - 3, top + 4f, z1 - 4), T("metal_panel", new Color(0.82f, 0.85f, 0.88f)), Snowy(S("roof_membrane"), season));
            var brick = S("wall_brick_red");
            Stack(k, x1 - 4, z1 - 4, top, 0.9f, 14f, brick);
            Stack(k, x1 - 7, z1 - 4, top, 0.7f, 10f, brick);
            // wood chips and the pulp tanks
            Pile(k, w / 2 - 4, 4, 3.4f, 4.2f, C(H("#c8a878"), 0.1f));
            var tank = T("metal_panel", new Color(0.92f, 0.93f, 0.94f));
            Tank(k, w / 2 - 4, -4, 2.2f, 7f, tank, C(H("#9aa3ab"), 0.5f, 0.6f));
            Tank(k, w / 2 - 4, d / 2 - 4, 2.0f, 6f, tank, C(H("#9aa3ab"), 0.5f, 0.6f));
            var roll = C(H("#f4f2ec"), 0.3f);
            for (int i = 0; i < 4; i++) k.Cylinder(new Vector3(x0 + 2 + i * 2.6f, 0.06f, -d / 2 + 3), 1.0f, 1.0f, 2.2f, 12, roll, C(H("#d9d6cc")));
            Truck(k, x1 - 4, -d / 2 + 2.5f, 90, H("#2f5d8a"), closed);
        }

        static void SteelMill(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 2, w / 2 - 9, -d / 2 + 6, d / 2 - 2, 9f, new Color(0.38f, 0.4f, 0.42f), new Color(0.3f, 0.3f, 0.32f), closed, season, 3, 2.4f);
            Sign(k, f[South], f[South].Width / 2, 7f, 6.0f, 0.9f, Row(BuildingType.SteelMill), closed);
            // the furnace: a tall shell with its downcomer, three thin stacks, an ore heap
            var shell = C(H("#5a5f66"), 0.4f, 0.6f);
            var rust = C(H("#8a4b2a"), 0.3f, 0.3f);
            k.Cylinder(new Vector3(w / 2 - 5, 0.06f, 2), 3.2f, 2.4f, 16f, 12, shell, shell);
            k.Cylinder(new Vector3(w / 2 - 5, 16f, 2), 1.2f, 1.2f, 4f, 10, rust, rust);
            for (int i = 0; i < 3; i++) Stack(k, -w / 2 + 6 + i * 5, d / 2 - 4, 11.4f, 0.6f, 14f, C(H("#4d5258"), 0.3f, 0.4f));
            Pile(k, w / 2 - 5, d / 2 - 5, 3.2f, 3.8f, C(H("#8e4a2c"), 0.1f));
            // glowing at the open doors
            if (!closed) Ledge(k, f[South], f[South].Width * 0.5f - 1.5f, f[South].Width * 0.5f + 1.5f, 0.2f, 3.8f, 0.05f, Lit(H("#ff7a1a"), 3f));
            k.Box(new Vector3(-w / 2 + 2, 0.06f, -d / 2 + 2), new Vector3(-w / 2 + 12, 0.8f, -d / 2 + 4), C(H("#6b7077"), 0.5f, 0.7f), C(H("#7b828a"), 0.5f, 0.7f));
            Truck(k, 2, -d / 2 + 2.8f, 90, H("#2b2b2b"), closed);
        }

        static void MetalShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 5.6f, new Color(0.46f, 0.56f, 0.66f), new Color(0.36f, 0.38f, 0.42f), closed, season, 2, 1.6f);
            Sign(k, f[South], f[South].Width / 2, 6.4f, 4.5f, 0.75f, Row(BuildingType.MetalShop), closed);
            WindowRow(k, f[East], 1, f[East].Width - 1, 2.2f, 1.6f, 1.0f, 2.6f, ref s, closed);
            // a gantry crane over the yard and racks of bar stock
            var yel = C(H("#f2b134"), 0.4f, 0.3f);
            k.Box(new Vector3(-w / 2 + 1.5f, 0.06f, -d / 2 + 1), new Vector3(-w / 2 + 1.9f, 4.4f, -d / 2 + 1.4f), yel, yel);
            k.Box(new Vector3(w / 2 - 1.9f, 0.06f, -d / 2 + 1), new Vector3(w / 2 - 1.5f, 4.4f, -d / 2 + 1.4f), yel, yel);
            k.Box(new Vector3(-w / 2 + 1.5f, 4.4f, -d / 2 + 1), new Vector3(w / 2 - 1.5f, 4.9f, -d / 2 + 1.4f), yel, yel);
            var steel = C(H("#8f99a3"), 0.5f, 0.8f);
            for (int i = 0; i < 3; i++) k.Box(new Vector3(-6 + i * 4.2f, 0.06f, -d / 2 + 2.4f), new Vector3(-3 + i * 4.2f, 0.4f + i * 0.2f, -d / 2 + 4.4f), steel, steel, 0.6f);
        }

        static void AutoPartsPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 1.5f, x1 = w / 2 - 1.5f, z0 = -d / 2 + 7, z1 = d / 2 - 1;
            var skin = new Skin { Upper = "fac_metal_blue", Floor = 4.5f, Bay = 3.2f, Ground = "gf_rollup", GroundH = 4.7f, GroundBay = 3.2f, Wall = "metal_panel", Coping = H("#0f3d6e"), Glow = 1.3f };
            float top = Block(k, x0, x1, z0, z1, skin, 1, 1, 0, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            Ledge(k, faces[South], -0.1f, faces[South].Width + 0.1f, top - 1.2f, top - 0.4f, 0.2f, C(H("#0f3d6e"), 0.4f));
            Sign(k, faces[South], 6, 8f, top - 1.15f, 0.7f, Row(BuildingType.AutoPartsPlant), closed);
            uint rs = s;
            RoofClutter(k, x0 + 1, x1 - 1, z0 + 1, z1 - 1, top, ref rs, 2f, S("metal_panel"));
            var dock = S("concrete");
            k.Box(new Vector3(0, 0, z0 - 1.6f), new Vector3(x1 - 0.5f, 1.1f, z0), dock, dock, 2f);
            for (int i = 0; i < 2; i++) Truck(k, 3 + i * 5.5f, z0 - 5.6f, 180, H("#0a79df"), closed);
            Parking(k, x0, -d / 2 + 0.5f, -1, z0 - 0.5f, ref s, closed ? 0.05f : 0.6f);
        }

        static void ChemicalPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            // the control building in front
            var f = FlatBox(k, -w / 2 + 2, -w / 2 + 12, -d / 2 + 2, -d / 2 + 8, 4.2f, T("metal_panel", new Color(0.95f, 0.95f, 0.94f)), C(H("#00897b")), season);
            WindowRow(k, f[South], 0.5f, f[South].Width - 0.5f, 1.4f, 1.6f, 1.2f, 2.2f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 7f, 2.9f, 0.7f, Row(BuildingType.ChemicalPlant), closed);
            // the tank farm and its bund wall, two columns, a pipe rack
            var white = T("metal_panel", new Color(0.96f, 0.96f, 0.95f)); var teal = C(H("#00897b"), 0.5f, 0.4f);
            k.Box(new Vector3(-w / 2 + 2, 0.06f, 0), new Vector3(2, 0.8f, d / 2 - 1.5f), S("concrete"), S("concrete"));
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 2; j++) Tank(k, -w / 2 + 5 + i * 5.5f, 3.5f + j * 5.2f, 2.0f, 4.5f, white, teal);
            var steel = C(H("#b8c0c8"), 0.6f, 0.8f);
            k.Cylinder(new Vector3(7, 0.06f, 2), 0.9f, 0.9f, 18f, 10, steel, steel);
            k.Cylinder(new Vector3(10, 0.06f, 4), 0.7f, 0.7f, 14f, 10, steel, steel);
            for (float y = 4; y < 17; y += 4) k.Cylinder(new Vector3(7, y, 2), 1.15f, 1.15f, 0.2f, 10, teal, teal);
            var pipe = C(H("#d9d9d4"), 0.5f, 0.5f);
            k.Box(new Vector3(-3, 5.5f, -d / 2 + 9.6f), new Vector3(w / 2 - 2, 6.0f, -d / 2 + 10.2f), pipe, pipe);
            for (float x = -3; x < w / 2 - 1; x += 4) k.Box(new Vector3(x - 0.12f, 0.06f, -d / 2 + 9.7f), new Vector3(x + 0.12f, 5.5f, -d / 2 + 10.1f), steel, steel);
            Perimeter(k, w, d, 2f, C(H("#9aa3ab"), 0.5f, 0.6f), 8f);
        }

        static void PlasticsPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 6, -d / 2 + 5, d / 2 - 1, 6f, new Color(0.88f, 0.92f, 0.96f), new Color(0.6f, 0.65f, 0.72f), closed, season, 2, 1.4f);
            Sign(k, f[South], f[South].Width / 2, 6.4f, 4.6f, 0.75f, Row(BuildingType.PlasticsPlant), closed);
            // pellet silos on legs, piped into the hall
            var white = C(H("#f2f5f8"), 0.5f, 0.3f); var blue = C(H("#1565c0"), 0.4f, 0.3f); var leg = C(H("#5d6670"), 0.4f, 0.5f);
            for (int i = 0; i < 3; i++)
            {
                float z = -d / 2 + 5 + i * 5;
                foreach (var o in new[] { new Vector2(-0.9f, -0.9f), new Vector2(0.9f, -0.9f), new Vector2(-0.9f, 0.9f), new Vector2(0.9f, 0.9f) })
                    k.Box(new Vector3(w / 2 - 3 + o.x - 0.1f, 0.06f, z + o.y - 0.1f), new Vector3(w / 2 - 3 + o.x + 0.1f, 3f, z + o.y + 0.1f), leg, leg);
                k.Cylinder(new Vector3(w / 2 - 3, 2.2f, z), 0.4f, 1.4f, 1.0f, 12, white, null);
                k.Cylinder(new Vector3(w / 2 - 3, 3.2f, z), 1.4f, 1.4f, 7f, 12, i == 1 ? blue : white, null);
                k.Cylinder(new Vector3(w / 2 - 3, 10.2f, z), 1.4f, 0.2f, 0.8f, 12, white, white);
            }
            k.Box(new Vector3(w / 2 - 6, 8.2f, -d / 2 + 4.8f), new Vector3(w / 2 - 3, 8.5f, -d / 2 + 15.2f), leg, leg);
            Truck(k, -2, -d / 2 + 1.8f, 90, H("#e3f2fd"), closed);
        }

        static void TextileMill(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            // an old three-storey brick mill with rows of tall windows, a stair tower and a water tank on the roof
            var skin = new Skin { Upper = "fac_civic_brick", Floor = 4f, Bay = 2.7f, Ground = "gf_apt_base", GroundH = 4f, Wall = "wall_brick_red", Coping = H("#cbbfae") };
            float x0 = -w / 2 + 1.5f, x1 = w / 2 - 6, z0 = -d / 2 + 4, z1 = d / 2 - 2;
            float top = Block(k, x0, x1, z0, z1, skin, 2, 0, 0, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            Sign(k, faces[South], faces[South].Width / 2, 9f, top + 0.9f, 0.9f, Row(BuildingType.TextileMill), closed);
            var brick = S("wall_brick_red");
            k.Box(new Vector3(x1 - 4, 0, z0 - 2.5f), new Vector3(x1 - 0.5f, top + 5, z0), brick, C(H("#cbbfae")));
            var wood = T("wood", new Color(0.55f, 0.42f, 0.3f));
            k.Cylinder(new Vector3(x0 + 4, top, z1 - 4), 1.8f, 1.8f, 0.2f, 8, wood, wood);
            for (int i = 0; i < 4; i++) k.Box(new Vector3(x0 + 3 + (i % 2) * 2 - 0.1f, top, z1 - 5 + (i / 2) * 2 - 0.1f), new Vector3(x0 + 3 + (i % 2) * 2 + 0.1f, top + 3, z1 - 5 + (i / 2) * 2 + 0.1f), wood, wood);
            k.Cylinder(new Vector3(x0 + 4, top + 3, z1 - 4), 1.7f, 1.7f, 3f, 10, wood, wood);
            k.Cylinder(new Vector3(x0 + 4, top + 6, z1 - 4), 1.8f, 0.1f, 1.2f, 10, C(H("#3b3e42")), C(H("#3b3e42")));
            Stack(k, w / 2 - 3, d / 2 - 3, 0.06f, 1.0f, 18f, brick);
            Crates(k, w / 2 - 5, -d / 2 + 2, 6, T("wood", new Color(0.86f, 0.74f, 0.55f)), ref s);
        }

        static void Tannery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("dirt"), 0.05f, 5f);
            var f = GableBox(k, -w / 2 + 1, w / 2 - 1, 1, d / 2 - 1, 4.4f, 2.2f, S("wall_brick_brown"), T("roof_metal", new Color(0.45f, 0.4f, 0.36f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.6f, 1.2f, 1.3f, 3f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 6f, 3.6f, 0.65f, Row(BuildingType.Tannery), closed);
            Door(k, f[South], 3, H("#5b3a29"), 2.6f, 3f);
            // hides drying on racks, and the soaking vats
            var hide = C(H("#a0703f"), 0.15f); var rack = T("wood", new Color(0.5f, 0.4f, 0.3f));
            for (int i = 0; i < 4; i++)
            {
                float x = -w / 2 + 2 + i * 2.6f;
                k.Box(new Vector3(x, 0.06f, -d / 2 + 2), new Vector3(x + 0.1f, 2.4f, -d / 2 + 2.1f), rack, rack);
                k.Box(new Vector3(x, 0.06f, -d / 2 + 6), new Vector3(x + 0.1f, 2.4f, -d / 2 + 6.1f), rack, rack);
                k.Box(new Vector3(x + 0.1f, 0.8f, -d / 2 + 2.1f), new Vector3(x + 0.18f, 2.3f, -d / 2 + 6), hide, hide);
            }
            var vat = T("wood", new Color(0.4f, 0.3f, 0.22f));
            for (int i = 0; i < 3; i++) k.Cylinder(new Vector3(3 + i * 2.4f, 0.06f, -d / 2 + 4), 1.0f, 1.0f, 1.3f, 10, vat, C(H("#6b4a2a"), 0.4f));
        }

        static void Glassworks(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 6.5f, new Color(0.7f, 0.86f, 0.88f), new Color(0.35f, 0.42f, 0.46f), closed, season, 1, 1.8f);
            Sign(k, f[South], f[South].Width * 0.7f, 5.6f, 4.8f, 0.7f, Row(BuildingType.Glassworks), closed);
            // the furnace cone through the roof, glowing doors, sand
            var brick = S("wall_brick_brown");
            k.Cylinder(new Vector3(-2, 6.5f, 4), 3.2f, 1.2f, 7f, 12, brick, null);
            k.Cylinder(new Vector3(-2, 13.5f, 4), 1.2f, 1.0f, 2f, 10, brick, C(H("#2b2b2b")));
            if (!closed) for (int i = 0; i < 3; i++) Ledge(k, f[South], 2 + i * 1.6f, 3 + i * 1.6f, 0.6f, 1.6f, 0.05f, Lit(H("#ff8c2a"), 3.5f));
            Pile(k, w / 2 - 4, -d / 2 + 3, 2.6f, 2.4f, season == 0 ? S("snow") : S("sand"));
            var glass = MaterialLibrary.Pane(new Color32(120, 200, 210, 255));
            for (int i = 0; i < 3; i++) k.Box(new Vector3(-w / 2 + 2 + i * 1.6f, 0.06f, -d / 2 + 2), new Vector3(-w / 2 + 3.2f + i * 1.6f, 1.2f, -d / 2 + 3.6f), glass, T("wood", new Color(0.85f, 0.75f, 0.6f)));
        }

        static void Quarry(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("gravel"), 0.05f, 4f);
            // the cut rock face in terraces at the back
            var rock = T("wall_stone", new Color(0.85f, 0.83f, 0.8f));
            var bench = season == 0 ? S("snow") : S("gravel");
            for (int i = 0; i < 4; i++)
            {
                float z0 = 2 + i * 3.2f;
                k.Box(new Vector3(-w / 2 + 1, 0.06f, z0), new Vector3(w / 2 - 1, 3f + i * 3f, d / 2 - 1), rock, bench, 3f);
            }
            // the crusher on its legs, conveyors, heaps of stone
            var steel = C(H("#5d6670"), 0.4f, 0.5f); var yel = C(H("#f2b134"), 0.4f, 0.3f);
            for (int i = 0; i < 4; i++) k.Box(new Vector3(-6 + (i % 2) * 3.6f, 0.06f, -6 + (i / 2) * 3.6f), new Vector3(-5.6f + (i % 2) * 3.6f, 5f, -5.6f + (i / 2) * 3.6f), steel, steel);
            k.Box(new Vector3(-6.4f, 5f, -6.4f), new Vector3(-1.6f, 8f, -1.6f), yel, yel);
            Conveyor(k, new Vector3(-4, 0, -4), new Vector3(6, 0, -9), 7f, 3f);
            Conveyor(k, new Vector3(-4, 0, -4), new Vector3(-11, 0, -9), 7f, 3f);
            var stone = C(H("#9e9a92"), 0.1f);
            Pile(k, 6, -9, 3.0f, 3.2f, stone);
            Pile(k, -11, -9, 3.0f, 3.2f, C(H("#b8b2a6"), 0.1f));
            Pylon(k, 0, -d / 2 + 1.2f, 1.5f, 5.5f, Row(BuildingType.Quarry), closed);
            if (!closed)
            {
                // a dump truck
                k.Push(new Vector3(8, 0.06f, -1), 60);
                k.Box(new Vector3(-1.4f, 0.6f, -2.8f), new Vector3(1.4f, 2.2f, 1.6f), yel, yel);
                k.Box(new Vector3(-1.2f, 0.6f, 1.6f), new Vector3(1.2f, 2.6f, 3.0f), yel, yel);
                foreach (var p in new[] { new Vector2(-1.4f, -1.8f), new Vector2(1.4f, -1.8f), new Vector2(-1.4f, 2.2f), new Vector2(1.4f, 2.2f) })
                    k.Cylinder(new Vector3(p.x, 0, p.y), 0.65f, 0.65f, 0.6f, 8, C(H("#2b2b2b")), C(H("#2b2b2b")));
                k.Pop();
            }
        }

        static void Brickworks(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("dirt"), 0.05f, 5f);
            // the long kiln with its round roof, the brick chimney
            var brick = S("wall_brick_red");
            float x0 = -w / 2 + 2, x1 = w / 2 - 6, z0 = 0, z1 = 7;
            k.Box(new Vector3(x0, 0, z0), new Vector3(x1, 3.2f, z1), brick, brick);
            for (int i = 0; i < 4; i++)
            {
                float a0 = i / 4f * Mathf.PI, a1 = (i + 1) / 4f * Mathf.PI;
                float r = (z1 - z0) / 2, zc = (z0 + z1) / 2;
                var p0 = new Vector3(0, 3.2f + Mathf.Sin(a0) * 2.2f, zc - Mathf.Cos(a0) * r);
                var p1 = new Vector3(0, 3.2f + Mathf.Sin(a1) * 2.2f, zc - Mathf.Cos(a1) * r);
                k.Quad(new Vector3(x0, p0.y, p0.z), new Vector3(x0, p1.y, p1.z), new Vector3(x1, p1.y, p1.z), new Vector3(x1, p0.y, p0.z), Snowy(brick, season));
                k.Tri(new Vector3(x0, 3.2f, zc), new Vector3(x0, p1.y, p1.z), new Vector3(x0, p0.y, p0.z), brick);
                k.Tri(new Vector3(x1, 3.2f, zc), new Vector3(x1, p0.y, p0.z), new Vector3(x1, p1.y, p1.z), brick);
            }
            var faces = Faces(x0, x1, z0, z1);
            for (int i = 0; i < 6; i++) Ledge(k, faces[South], 1.5f + i * 3.4f, 2.7f + i * 3.4f, 0, 2.2f, 0.05f, C(H("#2b2b2b")));
            Stack(k, w / 2 - 3, 3.5f, 0.06f, 1.1f, 20f, brick);
            // pallets of bricks, clay, the office
            var red = C(H("#a5533c"), 0.15f);
            for (int i = 0; i < 8; i++) k.Box(new Vector3(-w / 2 + 2 + (i % 4) * 2.8f, 0.06f, -d / 2 + 2 + (i / 4) * 2.6f), new Vector3(-w / 2 + 4.2f + (i % 4) * 2.8f, 1.5f, -d / 2 + 4.0f + (i / 4) * 2.6f), red, red, 0.5f);
            Pile(k, w / 2 - 4, d / 2 - 4, 2.4f, 2.4f, C(H("#9c6b4a"), 0.1f));
            var off = Storefront(k, 3, w / 2 - 2, -d / 2 + 2, -d / 2 + 6.5f, 3.6f, brick, "gf_apt_base", closed, season, Row(BuildingType.Brickworks), C(H("#e0d4c0")));
        }

        static void ConcretePlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var grey = C(H("#c9ccce"), 0.4f, 0.3f); var leg = C(H("#5d6670"), 0.4f, 0.5f);
            // two cement silos on legs, the batching tower, aggregate bins
            for (int i = 0; i < 2; i++)
            {
                float x = -w / 2 + 3 + i * 3.6f, z = 4;
                foreach (var o in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                    k.Box(new Vector3(x + o.x - 0.1f, 0.06f, z + o.y - 0.1f), new Vector3(x + o.x + 0.1f, 4f, z + o.y + 0.1f), leg, leg);
                k.Cylinder(new Vector3(x, 3f, z), 0.4f, 1.5f, 1.2f, 12, grey, null);
                k.Cylinder(new Vector3(x, 4.2f, z), 1.5f, 1.5f, 9f, 12, grey, null);
                k.Cylinder(new Vector3(x, 13.2f, z), 1.5f, 1.4f, 0.4f, 12, grey, grey);
            }
            k.Box(new Vector3(1, 0.06f, 1), new Vector3(5, 3.8f, 7), leg, leg);
            k.Box(new Vector3(1, 3.8f, 1), new Vector3(5, 11f, 7), T("metal_panel", new Color(0.95f, 0.95f, 0.94f)), C(H("#263238")));
            var face = Faces(1, 5, 1, 7)[South];
            Sign(k, face, face.Width / 2, 3.6f, 9.6f, 0.55f, Row(BuildingType.ConcretePlant), closed);
            var stone = C(H("#a39e93"), 0.1f);
            for (int i = 0; i < 3; i++)
            {
                k.Box(new Vector3(-w / 2 + 1.5f + i * 4f, 0.06f, d / 2 - 5), new Vector3(-w / 2 + 1.7f + i * 4f, 2.2f, d / 2 - 1), S("concrete"), S("concrete"));
                Pile(k, -w / 2 + 3.6f + i * 4f, d / 2 - 3, 1.6f, 1.8f, i == 1 ? S("sand") : stone);
            }
            if (!closed)
                for (int i = 0; i < 2; i++)
                {
                    // mixer trucks, drums tilted up
                    k.Push(new Vector3(-w / 2 + 4 + i * 6, 0.06f, -d / 2 + 2.5f), 90);
                    var white = C(H("#f2f2ee"), 0.4f); var orange = C(H("#e67e22"), 0.4f);
                    k.Box(new Vector3(-1.1f, 0.6f, 2.0f), new Vector3(1.1f, 2.7f, 3.4f), white, white);
                    k.Box(new Vector3(-1.1f, 0.6f, -3.0f), new Vector3(1.1f, 1.1f, 2.0f), C(H("#3a3e44")), C(H("#3a3e44")));
                    k.Cylinder(new Vector3(0, 1.1f, -0.5f), 1.1f, 0.9f, 2.4f, 10, orange, orange);
                    k.Pop();
                }
        }

        static void FlourMill(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            // a row of tall concrete grain silos with the head house on top, the mill beside them
            var conc = T("wall_stucco", new Color(0.88f, 0.86f, 0.82f));
            for (int i = 0; i < 4; i++) k.Cylinder(new Vector3(-w / 2 + 3 + i * 3.3f, 0.06f, d / 2 - 4), 1.65f, 1.65f, 16f, 12, conc, conc);
            k.Box(new Vector3(-w / 2 + 1.5f, 16f, d / 2 - 5.2f), new Vector3(-w / 2 + 14f, 19f, d / 2 - 2.8f), conc, Snowy(C(H("#9aa3ab")), season));
            var f = GableBox(k, -w / 2 + 1.5f, w / 2 - 1.5f, -d / 2 + 4, 4, 7f, 2.6f, T("wall_board", new Color(0.93f, 0.88f, 0.8f)), T("roof_metal", new Color(0.55f, 0.45f, 0.35f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 4.4f, 1.0f, 1.2f, 2.4f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 7f, 2.6f, 0.75f, Row(BuildingType.FlourMill), closed);
            Door(k, f[East], f[East].Width / 2, H("#7b5e3b"), 2.4f, 3f);
            var sack = C(H("#efe6d0"), 0.1f);
            for (int i = 0; i < 6; i++) k.Box(new Vector3(w / 2 - 6 + (i % 3) * 1.2f, 0.06f + (i / 3) * 0.5f, -d / 2 + 1.5f), new Vector3(w / 2 - 5.1f + (i % 3) * 1.2f, 0.56f + (i / 3) * 0.5f, -d / 2 + 2.3f), sack, sack);
        }

        static void BreadFactory(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var wall = T("wall_stucco", new Color(0.98f, 0.9f, 0.72f));
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 6.5f, wall, C(H("#c0392b")), season);
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 5.2f, 6.2f, 0.2f, C(H("#c0392b"), 0.4f));
            Sign(k, f[South], f[South].Width / 2, 9f, 5.25f, 0.9f, Row(BuildingType.BreadFactory), closed);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.6f, 1.6f, 1.4f, 2.8f, ref s, closed);
            // oven vents, a flour silo, the vans
            var steel = C(H("#c9ced3"), 0.6f, 0.7f);
            for (int i = 0; i < 4; i++) k.Cylinder(new Vector3(-6 + i * 3.5f, 6.5f, 2), 0.35f, 0.35f, 2.4f, 8, steel, C(H("#3a3e44")));
            Silo(k, w / 2 - 3, d / 2 - 3, 1.4f, 9f, steel, steel, 1f);
            if (!closed)
                for (int i = 0; i < 3; i++)
                {
                    k.Push(new Vector3(-w / 2 + 4 + i * 5, 0.06f, -d / 2 + 2.5f), 90);
                    var van = C(H("#f4c26b"), 0.4f);
                    k.Box(new Vector3(-0.95f, 0.4f, -2.2f), new Vector3(0.95f, 2.4f, 2.2f), van, van);
                    foreach (var p in new[] { new Vector2(-0.95f, -1.4f), new Vector2(0.95f, -1.4f), new Vector2(-0.95f, 1.4f), new Vector2(0.95f, 1.4f) })
                        k.Cylinder(new Vector3(p.x, 0, p.y), 0.36f, 0.36f, 0.3f, 8, C(H("#2b2b2b")), C(H("#2b2b2b")));
                    k.Pop();
                }
        }

        static void DairyPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 7, -d / 2 + 5, d / 2 - 1, 7f, T("metal_panel", new Color(0.97f, 0.97f, 0.96f)), C(H("#1e88e5")), season);
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 0.0f, 0.8f, 0.1f, C(H("#1e88e5"), 0.4f));
            Sign(k, f[South], f[South].Width / 2, 7f, 5.2f, 0.75f, Row(BuildingType.DairyPlant), closed);
            WindowRow(k, f[South], 1, f[South].Width - 1, 2.4f, 1.4f, 1.0f, 2.2f, ref s, closed);
            // stainless milk silos and a tanker
            var steel = C(H("#e1e6ea"), 0.8f, 0.9f);
            for (int i = 0; i < 3; i++) Silo(k, w / 2 - 3.5f, -d / 2 + 4 + i * 5, 1.8f, 9f, steel, steel, 0.8f);
            if (!closed)
            {
                k.Push(new Vector3(-2, 0.06f, -d / 2 + 2), 90);
                var cab = C(H("#1e88e5"), 0.4f);
                k.Box(new Vector3(-1.1f, 0.5f, 3.0f), new Vector3(1.1f, 2.8f, 4.6f), cab, cab);
                k.Box(new Vector3(-1.1f, 0.5f, -4.0f), new Vector3(1.1f, 0.9f, 3.0f), C(H("#3a3e44")), C(H("#3a3e44")));
                k.Cylinder(new Vector3(0, 0.9f, -3.6f), 1.05f, 1.05f, 0.01f, 8, steel, steel);
                k.Box(new Vector3(-1.05f, 0.9f, -3.8f), new Vector3(1.05f, 2.9f, 2.8f), steel, steel);
                k.Pop();
            }
        }

        static void Meatpacking(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 7, d / 2 - 6, 6.5f, T("wall_stucco", new Color(0.86f, 0.86f, 0.84f)), C(H("#8e1b1b")), season);
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 5.4f, 6.4f, 0.15f, C(H("#8e1b1b"), 0.4f));
            Sign(k, f[South], f[South].Width / 2, 6f, 5.45f, 0.8f, Row(BuildingType.Meatpacking), closed);
            var dock = S("concrete");
            k.Box(new Vector3(-w / 2 + 2, 0, -d / 2 + 5.4f), new Vector3(w / 2 - 2, 1.1f, -d / 2 + 7), dock, dock, 2f);
            for (int i = 0; i < 4; i++) Ledge(k, f[South], 1.5f + i * 4.5f, 4.5f + i * 4.5f, 1.1f, 4.0f, 0.05f, closed ? MaterialLibrary.Dark("gf_rollup", Color.grey) : S("gf_rollup", 1.2f));
            uint rs = s;
            RoofClutter(k, -w / 2 + 2, w / 2 - 2, -d / 2 + 8, d / 2 - 7, 6.5f, ref rs, 2.5f, S("metal_panel"));
            // holding pens out back
            var rail = C(H("#9aa3ab"), 0.4f, 0.5f);
            k.Box(new Vector3(-w / 2 + 1, 0.06f, d / 2 - 5), new Vector3(w / 2 - 1, 1.2f, d / 2 - 4.9f), rail, rail);
            k.Box(new Vector3(-w / 2 + 1, 0.06f, d / 2 - 1.1f), new Vector3(w / 2 - 1, 1.2f, d / 2 - 1), rail, rail);
            Truck(k, -3, -d / 2 + 1.6f, 90, H("#f2f2ee"), closed);
        }

        static void Cannery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, 0, d / 2 - 1, 6f, new Color(0.93f, 0.92f, 0.86f), new Color(0.3f, 0.5f, 0.3f), closed, season, 2, 1.8f);
            Sign(k, f[South], f[South].Width / 2, 7f, 4.6f, 0.8f, Row(BuildingType.Cannery), closed);
            // the produce shed in front, linked by a conveyor; crates of cans
            var shed = GableBox(k, -w / 2 + 1, -1, -d / 2 + 2, -d / 2 + 7.5f, 3.4f, 1.4f, T("metal_panel", new Color(0.7f, 0.78f, 0.62f)), T("roof_metal", new Color(0.3f, 0.5f, 0.3f)), season);
            Conveyor(k, new Vector3(-4, 0, -d / 2 + 7.5f), new Vector3(-4, 0, 0), 3.2f, 4.2f);
            var can = C(H("#c0c6cc"), 0.6f, 0.8f); var label = C(H("#2e7d32"), 0.3f);
            for (int i = 0; i < 6; i++)
            {
                float x = 2 + (i % 3) * 2.2f, z = -d / 2 + 2 + (i / 3) * 2.4f;
                k.Box(new Vector3(x, 0.06f, z), new Vector3(x + 1.8f, 0.2f, z + 1.8f), T("wood", new Color(0.85f, 0.75f, 0.6f)), T("wood", new Color(0.85f, 0.75f, 0.6f)));
                k.Box(new Vector3(x + 0.1f, 0.2f, z + 0.1f), new Vector3(x + 1.7f, 1.4f, z + 1.7f), label, can);
            }
        }

        static void Brewery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            // the brick brewhouse with arched windows, a copper kettle showing through a glass front
            var brick = S("wall_brick_brown");
            var f = GableFront(k, -w / 2 + 1.5f, 2, -d / 2 + 3, d / 2 - 1.5f, 7f, 3f, brick, T("roof_shingle", new Color(0.3f, 0.3f, 0.32f)), season);
            Sign(k, f[South], f[South].Width / 2, f[South].Width - 1.6f, 5.4f, 0.9f, Row(BuildingType.Brewery), closed);
            var glass = Glass(closed);
            Rect(k, f[South], 1.4f, f[South].Width - 1.4f, 0.4f, 5f, glass, Vector2.zero, new Vector2(1, 1), 0.03f);
            var copper = C(H("#b87333"), 0.7f, 0.9f);
            k.Cylinder(new Vector3(-w / 2 + 6, 0.06f, -d / 2 + 5.5f), 1.6f, 1.6f, 2.6f, 14, copper, null);
            k.Cylinder(new Vector3(-w / 2 + 6, 2.66f, -d / 2 + 5.5f), 1.6f, 0.4f, 1.2f, 14, copper, copper);
            // fermenting tanks outside, kegs
            var steel = C(H("#d7dde2"), 0.8f, 0.9f);
            for (int i = 0; i < 4; i++)
            {
                float x = 4.5f + (i % 2) * 3.4f, z = -1 + (i / 2) * 4.2f;
                k.Cylinder(new Vector3(x, 1.2f, z), 0.4f, 1.3f, 1.2f, 12, steel, null);
                k.Cylinder(new Vector3(x, 2.4f, z), 1.3f, 1.3f, 6.5f, 12, steel, null);
                k.Cylinder(new Vector3(x, 8.9f, z), 1.3f, 0.2f, 0.8f, 12, steel, steel);
                k.Box(new Vector3(x - 0.08f, 0.06f, z - 0.08f), new Vector3(x + 0.08f, 1.3f, z + 0.08f), steel, steel);
            }
            Barrels(k, 4, -d / 2 + 2, 5, C(H("#a8b0b8"), 0.6f, 0.8f));
        }

        static void Winery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            var stone = T("wall_stone", new Color(0.92f, 0.86f, 0.76f));
            var f = GableBox(k, -w / 2 + 1.5f, w / 2 - 1.5f, 0, d / 2 - 1.5f, 5f, 2.6f, stone, T("roof_shingle", new Color(0.82f, 0.46f, 0.32f)), season, 0.6f);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.8f, 1.0f, 1.8f, 3.2f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 6f, 4.0f, 0.7f, Row(BuildingType.Winery), closed);
            Door(k, f[South], f[South].Width / 2, H("#4a2c1d"), 1.8f, 2.6f);
            // barrels stacked on racks under a pergola, a few vines
            var wood = T("wood", new Color(0.6f, 0.4f, 0.26f));
            var post = T("wood", new Color(0.45f, 0.34f, 0.25f));
            for (int i = 0; i < 4; i++) k.Box(new Vector3(-w / 2 + 2 + i * 2.6f, 0.06f, -d / 2 + 3), new Vector3(-w / 2 + 2.2f + i * 2.6f, 3f, -d / 2 + 3.2f), post, post);
            k.Box(new Vector3(-w / 2 + 2, 3f, -d / 2 + 3), new Vector3(-w / 2 + 10, 3.2f, -d / 2 + 7.5f), post, post);
            for (int i = 0; i < 6; i++) k.Cylinder(new Vector3(-w / 2 + 2.8f + (i % 3) * 2.4f, 0.06f + (i / 3) * 1.1f, -d / 2 + 5), 0.5f, 0.5f, 1.0f, 10, wood, wood);
            Rows(k, 2, -d / 2 + 2, w / 2 - 1.5f, -1, 2.2f, 1.4f, H("#4f7f2f"), season, true);
        }

        static void Distillery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            // the still house (brick, a glass wall onto the pot still) and the tall black rickhouse behind
            var f = Storefront(k, -w / 2 + 1, 1, -d / 2 + 2, -d / 2 + 9, 6f, S("wall_brick_red"), "gf_store_dark", closed, season, Row(BuildingType.Distillery), C(H("#d4a24c")), 0.15f, 0.85f, -1, 4.6f);
            var copper = C(H("#b87333"), 0.7f, 0.9f);
            k.Cylinder(new Vector3(-4.5f, 0.06f, -d / 2 + 5.5f), 1.4f, 1.6f, 1.6f, 12, copper, null);
            k.Cylinder(new Vector3(-4.5f, 1.66f, -d / 2 + 5.5f), 1.6f, 0.4f, 1.6f, 12, copper, null);
            k.Cylinder(new Vector3(-4.5f, 3.26f, -d / 2 + 5.5f), 0.4f, 0.25f, 2.4f, 10, copper, copper);
            var black = T("wall_board", new Color(0.18f, 0.18f, 0.2f));
            var rf = GableBox(k, -w / 2 + 1, w / 2 - 1, 1, d / 2 - 1, 11f, 2f, black, T("roof_metal", new Color(0.25f, 0.25f, 0.27f)), season, 0.3f);
            for (float y = 1.5f; y < 10; y += 2.6f) WindowRow(k, rf[East], 1, rf[East].Width - 1, y, 0.5f, 0.8f, 2.2f, ref s, true);
            Barrels(k, 3, -d / 2 + 2, 6, T("wood", new Color(0.55f, 0.36f, 0.22f)));
        }

        static void BottlingPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 7f, T("metal_panel", new Color(0.98f, 0.98f, 0.97f)), C(H("#d32f2f")), season);
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 5.6f, 6.8f, 0.2f, C(H("#d32f2f"), 0.5f));
            Sign(k, f[South], f[South].Width / 2, 8f, 5.7f, 0.95f, Row(BuildingType.BottlingPlant), closed);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.8f, 1.6f, 1.6f, 3f, ref s, closed);
            // a giant bottle on the roof
            var red = C(H("#d32f2f"), 0.6f, 0.2f); var white = C(H("#f4f4f2"), 0.5f);
            k.Cylinder(new Vector3(4, 7f, 2), 1.3f, 1.3f, 4.5f, 12, red, null);
            k.Cylinder(new Vector3(4, 11.5f, 2), 1.3f, 0.45f, 1.6f, 12, red, null);
            k.Cylinder(new Vector3(4, 13.1f, 2), 0.45f, 0.45f, 1.4f, 10, red, null);
            k.Cylinder(new Vector3(4, 14.5f, 2), 0.5f, 0.5f, 0.3f, 10, white, white);
            k.Cylinder(new Vector3(4, 8.6f, 2), 1.32f, 1.32f, 1.2f, 12, white, null);
            Truck(k, -4, -d / 2 + 2.2f, 90, H("#d32f2f"), closed);
            Truck(k, 4, -d / 2 + 2.2f, 90, H("#d32f2f"), closed);
        }

        static void PetFoodPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 6, -d / 2 + 5, d / 2 - 1, 6f, new Color(0.98f, 0.82f, 0.45f), new Color(0.45f, 0.35f, 0.25f), closed, season, 2, 1.6f);
            Sign(k, f[South], f[South].Width / 2, 6.4f, 4.6f, 0.75f, Row(BuildingType.PetFoodPlant), closed);
            var steel = C(H("#c9ced3"), 0.6f, 0.7f);
            Silo(k, w / 2 - 3, -d / 2 + 5, 1.6f, 10f, steel, steel, 1f);
            Silo(k, w / 2 - 3, -d / 2 + 10, 1.6f, 10f, steel, steel, 1f);
            // a paw print on the roof gable
            var paw = C(H("#5b3a1a"), 0.3f);
            var g = f[West];
            k.Blob(g.At(g.Width / 2, 3.2f) + g.Normal * 0.1f, new Vector3(0.1f, 0.7f, 0.8f), H("#5b3a1a"));
            var bag = C(H("#e67e22"), 0.2f);
            for (int i = 0; i < 8; i++) k.Box(new Vector3(-w / 2 + 2 + (i % 4) * 1.1f, 0.06f + (i / 4) * 0.5f, -d / 2 + 2), new Vector3(-w / 2 + 2.9f + (i % 4) * 1.1f, 0.5f + (i / 4) * 0.5f, -d / 2 + 2.8f), bag, bag);
        }

        // ================================================================== factories and workshops
        static void ShoeFactory(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var skin = new Skin { Upper = "fac_civic_brown", Floor = 3.6f, Bay = 2.7f, Ground = "gf_apt_base", GroundH = 3.8f, Wall = "wall_brick_brown", Coping = H("#d8cfc0") };
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 4, z1 = d / 2 - 1;
            float top = Block(k, x0, x1, z0, z1, skin, 1, 2, 1, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            RoofSign(k, 0, z0 + 0.5f, top, 8f, Row(BuildingType.ShoeFactory), closed);
            Door(k, faces[South], faces[South].Width / 2, H("#212121"), 2.2f, 2.8f);
            // a big boot over the door
            var black = C(H("#212121"), 0.5f);
            var p = faces[South].At(faces[South].Width / 2, 3.2f) + Vector3.back * 0.3f;
            k.Box(p + new Vector3(-0.9f, 0, -0.3f), p + new Vector3(0.1f, 1.2f, 0), black, black);
            k.Box(p + new Vector3(-0.9f, 0, -0.3f), p + new Vector3(0.9f, 0.4f, 0), black, black);
            Truck(k, 5, -d / 2 + 2, 90, H("#212121"), closed);
        }

        static void AppliancePlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var f = FlatBox(k, -w / 2 + 1.5f, w / 2 - 1.5f, -d / 2 + 7, d / 2 - 1, 8f, T("metal_panel", new Color(0.95f, 0.96f, 0.97f)), C(H("#37474f")), season);
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 6.6f, 7.8f, 0.2f, C(H("#37474f"), 0.4f));
            Sign(k, f[South], 7, 9f, 6.75f, 0.9f, Row(BuildingType.AppliancePlant), closed);
            for (int i = 0; i < 4; i++) Ledge(k, f[South], 14 + i * 3.6f, 17 + i * 3.6f, 0, 4.2f, 0.05f, closed ? MaterialLibrary.Dark("gf_rollup", Color.grey) : S("gf_rollup", 1.2f));
            uint rs = s;
            RoofClutter(k, -w / 2 + 3, w / 2 - 3, -d / 2 + 8, d / 2 - 2, 8f, ref rs, 2.5f, S("metal_panel"));
            // boxed fridges and washers on pallets
            var box = C(H("#d9c7a8"), 0.1f); var white = C(H("#f4f4f2"), 0.4f);
            for (int i = 0; i < 8; i++)
            {
                float x = -w / 2 + 2 + (i % 4) * 1.6f, z = -d / 2 + 2 + (i / 4) * 2f;
                k.Box(new Vector3(x, 0.06f, z), new Vector3(x + 1.2f, i % 2 == 0 ? 1.9f : 1.0f, z + 1.2f), i % 3 == 0 ? white : box, box);
            }
            Truck(k, 6, -d / 2 + 2.5f, 90, H("#eceff1"), closed);
        }

        static void ToolWorks(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 5.8f, new Color(0.35f, 0.36f, 0.38f), new Color(0.3f, 0.3f, 0.32f), closed, season, 2, 1.6f);
            // hazard stripes along the front, a forge stack
            for (int i = 0; i < 10; i++) Ledge(k, f[South], i * 2f, i * 2f + 1f, 4.6f, 5.0f, 0.12f, C(i % 2 == 0 ? H("#f9a825") : H("#212121"), 0.4f));
            Sign(k, f[South], f[South].Width / 2, 6.4f, 5.1f, 0.6f, Row(BuildingType.ToolWorks), closed);
            Stack(k, w / 2 - 4, d / 2 - 4, 5.8f, 0.6f, 6f, C(H("#4d5258"), 0.3f, 0.4f));
            // a giant wrench on the lawn
            var steel = C(H("#b0b8c0"), 0.7f, 0.9f);
            k.Box(new Vector3(-w / 2 + 2, 0.06f, -d / 2 + 2.5f), new Vector3(-w / 2 + 2.4f, 0.5f, -d / 2 + 2.9f), C(H("#3a3e44")), C(H("#3a3e44")));
            k.Box(new Vector3(-w / 2 + 2.1f, 0.5f, -d / 2 + 2.6f), new Vector3(-w / 2 + 2.3f, 4f, -d / 2 + 2.8f), steel, steel);
            k.Box(new Vector3(-w / 2 + 1.5f, 3.8f, -d / 2 + 2.55f), new Vector3(-w / 2 + 2.9f, 4.6f, -d / 2 + 2.85f), steel, steel);
            Crates(k, 2, -d / 2 + 2, 6, T("wood", new Color(0.86f, 0.74f, 0.55f)), ref s);
        }

        static void PaintFactory(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 7, -d / 2 + 4, d / 2 - 1, 6f, T("metal_panel", new Color(0.97f, 0.97f, 0.96f)), C(H("#6a1b9a")), season);
            // stripes of paint down the front
            Color32[] cols = { H("#e53935"), H("#fb8c00"), H("#fdd835"), H("#43a047"), H("#1e88e5"), H("#8e24aa") };
            for (int i = 0; i < cols.Length; i++) Ledge(k, f[South], 1 + i * 1.2f, 2 + i * 1.2f, 2.4f, 6f, 0.05f, C(cols[i], 0.5f));
            Sign(k, f[South], f[South].Width * 0.68f, 5f, 4.2f, 0.6f, Row(BuildingType.PaintFactory), closed);
            // the mixing tanks, each its own colour
            for (int i = 0; i < 6; i++) Tank(k, w / 2 - 4.5f + (i % 2) * 3f, -d / 2 + 3.5f + (i / 2) * 5f, 1.2f, 4f, C(cols[i], 0.6f, 0.2f), C(H("#9aa3ab"), 0.5f, 0.6f));
            Barrels(k, -w / 2 + 2, -d / 2 + 2, 5, C(cols[(int)(Next(ref s) % 6)], 0.5f, 0.3f));
        }

        static void PharmaLab(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            k.Flat(-2, -d / 2 + 0.5f, 2, -d / 2 + 6, 0.07f, S("pavers"), 3f);
            // a crisp glass-and-white lab, its plant on the roof behind a screen
            var skin = new Skin { Upper = "fac_glass_blue", Floor = 3.8f, Bay = 3f, Ground = "gf_lobby", GroundH = 4f, Wall = "wall_stucco", Coping = H("#f4f4f2"), Glow = 1.4f };
            float x0 = -w / 2 + 2, x1 = w / 2 - 2, z0 = -d / 2 + 6, z1 = d / 2 - 2;
            float top = Block(k, x0, x1, z0, z1, skin, 1, 0, 2, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            Ledge(k, faces[South], -0.2f, faces[South].Width + 0.2f, top - 0.2f, top + 0.6f, 0.3f, C(H("#f4f4f2"), 0.4f));
            Sign(k, faces[South], faces[South].Width / 2, 5f, top - 1.4f, 0.6f, Row(BuildingType.PharmaLab), closed);
            k.Box(new Vector3(x0 + 3, top, z0 + 3), new Vector3(x1 - 3, top + 2.4f, z1 - 3), C(H("#c9ced3"), 0.5f, 0.6f), C(H("#9aa3ab"), 0.5f, 0.6f));
            for (int i = 0; i < 3; i++) k.Cylinder(new Vector3(x0 + 4 + i * 2.5f, top + 2.4f, z1 - 4), 0.3f, 0.3f, 2.6f, 8, C(H("#d7dde2"), 0.7f, 0.8f), C(H("#3a3e44")));
            Planter(k, -3, -d / 2 + 4, season); Planter(k, 3, -d / 2 + 4, season);
            Tree(k, -w / 2 + 2.5f, -d / 2 + 2.5f, 0.9f, season, s);
            Tree(k, w / 2 - 2.5f, -d / 2 + 2.5f, 0.9f, season, s + 9);
        }

        static void SoapWorks(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 5, d / 2 - 1, 6f, T("wall_stucco", new Color(0.82f, 0.92f, 0.98f)), C(H("#0277bd")), season);
            Sign(k, f[South], f[South].Width / 2, 7f, 4.4f, 0.75f, Row(BuildingType.SoapWorks), closed);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.6f, 1.4f, 1.2f, 2.6f, ref s, closed);
            // bubbles on the roof
            var bubble = C(H("#e1f5fe"), 0.9f, 0.1f);
            for (int i = 0; i < 6; i++)
            {
                float r = Range(ref s, 0.5f, 1.3f);
                k.Blob(new Vector3(Range(ref s, -6, 6), 6.6f + r + i * 0.4f, Range(ref s, 0, 6)), new Vector3(r, r, r), H("#d4f0ff"));
            }
            var steel = C(H("#d7dde2"), 0.7f, 0.8f);
            Tank(k, -w / 2 + 3, -d / 2 + 2.5f, 1.2f, 3f, steel, steel);
            Tank(k, -w / 2 + 6, -d / 2 + 2.5f, 1.2f, 3f, steel, steel);
            Truck(k, 4, -d / 2 + 2.3f, 90, H("#81d4fa"), closed);
        }

        static void PrintingPress(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var brick = S("wall_brick_grey");
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 7f, brick, C(H("#263238")), season, 1.0f);
            // tall arched windows, the press name in a band
            var pane = Glass(closed);
            for (float a = 1.6f; a < f[South].Width - 1.2f; a += 3f)
            {
                Rect(k, f[South], a, a + 1.6f, 1.2f, 5f, pane, new Vector2(0, 0), new Vector2(0.25f, 1), 0.03f);
                k.Cylinder(f[South].At(a + 0.8f, 5f) + Vector3.back * 0.04f, 0.8f, 0.0f, 0.6f, 6, brick, brick);
            }
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 5.9f, 7.1f, 0.2f, C(H("#263238"), 0.4f));
            Sign(k, f[South], f[South].Width / 2, 6f, 6.0f, 0.8f, Row(BuildingType.PrintingPress), closed);
            // paper rolls waiting
            var roll = C(H("#f4f2ec"), 0.3f);
            for (int i = 0; i < 5; i++) k.Cylinder(new Vector3(-w / 2 + 2.5f + i * 2.4f, 0.06f, -d / 2 + 3), 1.0f, 1.0f, 1.6f, 12, roll, C(H("#cfc9bb")));
            Truck(k, w / 2 - 4, -d / 2 + 2.6f, 90, H("#263238"), closed);
        }

        static void PaperGoodsPlant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 6f, new Color(0.88f, 0.9f, 0.96f), new Color(0.24f, 0.29f, 0.6f), closed, season, 2, 1.4f);
            Sign(k, f[South], f[South].Width / 2, 6.4f, 4.6f, 0.7f, Row(BuildingType.PaperGoodsPlant), closed);
            // a giant pencil leaning on the wall, boxes of notebooks
            var yel = C(H("#fdd835"), 0.4f); var tip = C(H("#f4d6a8"), 0.2f); var lead = C(H("#2b2b2b"));
            var b = f[South].At(1.2f, 0.06f) + Vector3.back * 0.6f;
            k.Cylinder(b, 0.45f, 0.45f, 4.2f, 6, yel, null);
            k.Cylinder(b + Vector3.up * 4.2f, 0.45f, 0.1f, 0.9f, 6, tip, null);
            k.Cylinder(b + Vector3.up * 5.1f, 0.1f, 0f, 0.25f, 6, lead, null);
            var box = C(H("#3949ab"), 0.2f);
            Crates(k, 3, -d / 2 + 2, 6, box, ref s);
        }

        static void ToyFactory(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            // a box in primary colours with a stack of giant toy blocks out front
            Color32[] cols = { H("#e53935"), H("#1e88e5"), H("#fdd835"), H("#43a047") };
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 7f, T("wall_stucco", new Color(0.98f, 0.98f, 0.97f)), C(cols[0]), season);
            for (int i = 0; i < 4; i++) Ledge(k, f[South], i * f[South].Width / 4, (i + 1) * f[South].Width / 4, 5.6f, 7f, 0.15f, C(cols[i], 0.4f));
            Sign(k, f[South], f[South].Width / 2, 5f, 5.65f, 0.7f, Row(BuildingType.ToyFactory), closed);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.6f, 1.6f, 1.6f, 2.6f, ref s, closed);
            for (int i = 0; i < 6; i++)
            {
                int row = i < 3 ? 0 : i < 5 ? 1 : 2;
                float x = -w / 2 + 2.2f + (i < 3 ? i : i < 5 ? i - 2.5f : 1f) * 1.6f, y = 0.06f + row * 1.4f;
                k.Box(new Vector3(x, y, -d / 2 + 2), new Vector3(x + 1.4f, y + 1.4f, -d / 2 + 3.4f), C(cols[i % 4], 0.4f), C(cols[(i + 1) % 4], 0.4f));
            }
            Truck(k, 5, -d / 2 + 2.3f, 90, H("#fdd835"), closed);
        }

        static void SportsGoodsFactory(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 6f, new Color(0.9f, 0.92f, 0.95f), new Color(0.15f, 0.2f, 0.45f), closed, season, 2, 1.4f);
            // a running-track stripe along the front and a giant ball on the roof
            Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 4.6f, 5.6f, 0.15f, C(H("#c0392b"), 0.3f));
            for (int i = 0; i < 3; i++) Ledge(k, f[South], -0.1f, f[South].Width + 0.1f, 4.8f + i * 0.3f, 4.86f + i * 0.3f, 0.17f, C(H("#f4f4f2")));
            Sign(k, f[South], f[South].Width / 2, 6f, 4.65f, 0.75f, Row(BuildingType.SportsGoodsFactory), closed);
            k.Blob(new Vector3(-4, 8.6f, 2), new Vector3(1.8f, 1.8f, 1.8f), H("#f4f4f2"));
            k.Blob(new Vector3(-4, 8.6f, 2), new Vector3(1.85f, 0.25f, 1.85f), H("#1a237e"));
            Truck(k, 4, -d / 2 + 2.3f, 90, H("#1a237e"), closed);
        }

        static void BicycleWorks(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var f = GableFront(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 5, d / 2 - 1, 5.6f, 3f, S("wall_brick_red"), T("roof_metal", new Color(0.0f, 0.42f, 0.36f)), season);
            Sign(k, f[South], f[South].Width / 2, 8f, 4.2f, 0.8f, Row(BuildingType.BicycleWorks), closed);
            var glass = Glass(closed);
            Rect(k, f[South], 2, f[South].Width - 2, 0.3f, 3.8f, glass, Vector2.zero, new Vector2(1.5f, 1), 0.03f);
            // a rack of new bicycles out front
            for (int i = 0; i < 6; i++) Bicycle(k, -w / 2 + 3 + i * 2.2f, -d / 2 + 2.5f, 0, i % 2 == 0 ? H("#00695c") : H("#e53935"));
        }

        static void Bicycle(MeshKit k, float x, float z, float yaw, Color32 frame)
        {
            k.Push(new Vector3(x, 0.06f, z), yaw);
            var tyre = C(H("#2b2b2b")); var f = C(frame, 0.5f, 0.4f);
            foreach (float oz in new[] { -0.55f, 0.55f })
            {
                k.Box(new Vector3(-0.03f, 0, oz - 0.35f), new Vector3(0.03f, 0.7f, oz - 0.3f), tyre, tyre);
                k.Box(new Vector3(-0.03f, 0, oz + 0.3f), new Vector3(0.03f, 0.7f, oz + 0.35f), tyre, tyre);
                k.Box(new Vector3(-0.03f, 0.65f, oz - 0.35f), new Vector3(0.03f, 0.7f, oz + 0.35f), tyre, tyre);
                k.Box(new Vector3(-0.03f, 0, oz - 0.35f), new Vector3(0.03f, 0.05f, oz + 0.35f), tyre, tyre);
            }
            k.Box(new Vector3(-0.04f, 0.35f, -0.55f), new Vector3(0.04f, 0.75f, 0.55f), f, f);
            k.Box(new Vector3(-0.25f, 0.95f, 0.5f), new Vector3(0.25f, 1.0f, 0.56f), f, f);
            k.Pop();
        }

        static void InstrumentWorkshop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            var wood = T("wall_siding", new Color(0.72f, 0.56f, 0.4f));
            var f = GableBox(k, -w / 2 + 1, w / 2 - 4, -d / 2 + 2, d / 2 - 1, 4f, 2.4f, wood, T("roof_shingle", new Color(0.35f, 0.3f, 0.28f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.2f, 1.6f, 1.6f, 2.6f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 5.6f, 3.2f, 0.55f, Row(BuildingType.InstrumentWorkshop), closed);
            // a clerestory along the ridge, fine lumber drying out the side
            k.Box(new Vector3(-w / 2 + 3, 5.4f, -1.2f), new Vector3(w / 2 - 6, 6.6f, 1.2f), wood, Snowy(T("roof_shingle", new Color(0.35f, 0.3f, 0.28f)), season));
            Timber(k, w / 2 - 3.5f, -d / 2 + 1, w / 2 - 0.8f, d / 2 - 1, 1.6f);
        }

        static void JewelryWorkshop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("sidewalk"), 0.02f, 6f);
            var stone = T("wall_stone", new Color(0.86f, 0.84f, 0.8f));
            var gold = C(H("#d4af37"), 0.7f, 0.9f);
            var f = FlatBox(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 2, d / 2 - 0.6f, 5.6f, stone, gold, season, 0.8f);
            Ledge(k, f[South], -0.15f, f[South].Width + 0.15f, 5.0f, 5.6f, 0.3f, stone, gold);
            Sign(k, f[South], f[South].Width / 2, f[South].Width - 1.6f, 4.0f, 0.7f, Row(BuildingType.JewelryWorkshop), closed);
            // small barred windows, a heavy door
            var bar = C(H("#2b2b2b"), 0.4f, 0.6f);
            foreach (float a in new[] { 1.4f, f[South].Width - 1.4f })
            {
                Rect(k, f[South], a - 0.7f, a + 0.7f, 1.2f, 3f, MaterialLibrary.Pane(closed ? new Color32(26, 30, 38, 255) : new Color32(255, 230, 170, 255)), Vector2.zero, Vector2.one, 0.04f);
                for (float b = a - 0.5f; b <= a + 0.51f; b += 0.25f) Ledge(k, f[South], b - 0.03f, b + 0.03f, 1.2f, 3f, 0.08f, bar);
            }
            Door(k, f[South], f[South].Width / 2, H("#1b1b1b"), 1.2f, 2.6f);
            Ledge(k, f[South], f[South].Width / 2 - 0.8f, f[South].Width / 2 + 0.8f, 2.7f, 2.82f, 0.12f, gold);
        }

        static void Pottery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            var f = GableBox(k, -w / 2 + 1, 3, -d / 2 + 6, d / 2 - 1, 4.2f, 2.4f, S("wall_brick_tan"), T("roof_shingle", new Color(0.72f, 0.42f, 0.3f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.4f, 1.2f, 1.2f, 2.6f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 5.6f, 3.4f, 0.6f, Row(BuildingType.Pottery), closed);
            // two bottle kilns
            var brick = S("wall_brick_red");
            foreach (float z in new[] { -2f, 5f })
            {
                k.Cylinder(new Vector3(w / 2 - 3.5f, 0.06f, z), 2.6f, 2.8f, 3f, 12, brick, null);
                k.Cylinder(new Vector3(w / 2 - 3.5f, 3.06f, z), 2.8f, 0.9f, 5f, 12, brick, null);
                k.Cylinder(new Vector3(w / 2 - 3.5f, 8.06f, z), 0.9f, 1.1f, 1f, 10, brick, C(H("#2b2b2b")));
            }
            // pots and vases on a bench out front
            var bench = T("wood", new Color(0.7f, 0.56f, 0.4f));
            k.Box(new Vector3(-w / 2 + 2, 0.06f, -d / 2 + 2), new Vector3(2, 0.8f, -d / 2 + 3.2f), bench, bench);
            Color32[] glaze = { H("#b5651d"), H("#2e6b8c"), H("#e8e2d4"), H("#6b8e23") };
            for (int i = 0; i < 8; i++)
            {
                float x = -w / 2 + 2.6f + i * 1.4f;
                k.Cylinder(new Vector3(x, 0.8f, -d / 2 + 2.6f), 0.25f, 0.4f, 0.4f, 8, C(glaze[i % 4], 0.6f), null);
                k.Cylinder(new Vector3(x, 1.2f, -d / 2 + 2.6f), 0.4f, 0.2f, 0.4f, 8, C(glaze[i % 4], 0.6f), C(H("#2b2b2b")));
            }
        }

        static void MattressFactory(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Hall(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 6, d / 2 - 1, 6.2f, new Color(0.84f, 0.87f, 0.96f), new Color(0.16f, 0.21f, 0.58f), closed, season, 2, 1.4f);
            Sign(k, f[South], f[South].Width / 2, 6.4f, 4.7f, 0.75f, Row(BuildingType.MattressFactory), closed);
            // stacks of wrapped mattresses
            var white = C(H("#f4f4f2"), 0.5f); var blue = C(H("#90caf9"), 0.4f);
            for (int st = 0; st < 3; st++)
                for (int i = 0; i < 4; i++)
                    k.Box(new Vector3(-w / 2 + 2 + st * 3.2f, 0.06f + i * 0.32f, -d / 2 + 2), new Vector3(-w / 2 + 4.8f + st * 3.2f, 0.36f + i * 0.32f, -d / 2 + 4.2f), i % 2 == 0 ? white : blue, white);
            Truck(k, 5, -d / 2 + 2.3f, 90, H("#283593"), closed);
        }

        static void CabinetShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            var wood = T("wall_board", new Color(0.55f, 0.4f, 0.3f));
            var f = GableFront(k, -w / 2 + 1, 3, -d / 2 + 4, d / 2 - 1, 5f, 3.2f, wood, T("roof_metal", new Color(0.3f, 0.32f, 0.3f)), season);
            Sign(k, f[South], f[South].Width / 2, 6f, 5.2f, 0.75f, Row(BuildingType.CabinetShop), closed);
            var door = closed ? MaterialLibrary.Dark("gf_store_wood", Color.grey) : S("gf_store_wood", 1.4f);
            Rect(k, f[South], 2, f[South].Width - 2, 0, 4.2f, door, Vector2.zero, new Vector2(1, 1), 0.03f);
            Timber(k, 5, -d / 2 + 2, w / 2 - 1, -d / 2 + 5, 1.8f);
            Timber(k, 5, -d / 2 + 6.5f, w / 2 - 1, -d / 2 + 9.5f, 1.2f);
            // a finished cabinet waiting for pick-up
            var oak = T("wood", new Color(0.86f, 0.68f, 0.46f));
            k.Box(new Vector3(-w / 2 + 2, 0.06f, -d / 2 + 1.2f), new Vector3(-w / 2 + 4.2f, 2f, -d / 2 + 2f), oak, oak);
        }
    }
}
