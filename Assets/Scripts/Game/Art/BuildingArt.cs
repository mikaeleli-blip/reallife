using System.Collections.Generic;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game.Art
{
    /// <summary>
    /// A low-poly model for every kind of building, made in code. Each is built in its own frame: the lot is w by d metres,
    /// centred on the origin, with the street side along -z. The view turns it to face its road.
    /// </summary>
    public static class BuildingArt
    {
        static Color32 H(string s) { return MeshKit.Hex(s); }

        static readonly Color32 Grass = H("#86b45e"), Paving = H("#c9c5bb"), Asphalt = H("#4e5258"), Line = H("#eeeeee"), Concrete = H("#b9b6ad"),
            Dark = H("#2f3338"), Door = H("#5a3d2b"), Trim = H("#f4f1ea"), Glass = H("#7fa6c0"), Metal = H("#8d96a0"), Brick = H("#b4583f"),
            BrickDark = H("#8e4331"), White = H("#f2f2ee"), Roof = H("#5d646c"), Soil = H("#8b6a4a");
        static readonly Color32[] HouseWalls = { H("#f2e8d5"), H("#e8d3b9"), H("#cfe0e8"), H("#d9e4c8"), H("#f0d9d2"), H("#e6e1f0"), H("#fbfbf7"), H("#d8c3a5"), H("#c9d6df"), H("#f3e2b3") };
        static readonly Color32[] HouseRoofs = { H("#8a4b3a"), H("#5b6770"), H("#6e4e3a"), H("#44505c"), H("#9c5a3c"), H("#3e4a3d"), H("#7a7f86") };

        static uint Next(ref uint s) { s = s * 1664525u + 1013904223u; return s >> 8; }
        static float R(ref uint s) { return (Next(ref s) & 0xFFFF) / 65535f; }

        /// <summary>The model of a building. floors only matters for some types; closed shops go dark.</summary>
        public static Mesh Build(BuildingType t, float w, float d, uint seed, int floors, bool closed, int season)
        {
            var k = new MeshKit();
            switch (t)
            {
                case BuildingType.House: House(k, w, d, seed, season); break;
                case BuildingType.Apartments: Apartments(k, w, d, seed, season); break;
                case BuildingType.GroceryStore: Grocery(k, w, d, seed, closed); break;
                case BuildingType.Restaurant: Shop(k, w, d, seed, closed, H("#c0392b"), H("#f6e9d7"), 1, true, H("#c0392b")); break;
                case BuildingType.Cafe: Shop(k, w, d, seed, closed, H("#6d4c41"), H("#efe2cf"), 2, true, H("#2e8b7a")); break;
                case BuildingType.Bar: Shop(k, w, d, seed, closed, H("#2c2f3a"), H("#5b4a42"), 2, false, H("#8e44ad")); break;
                case BuildingType.ClothingStore: Shop(k, w, d, seed, closed, H("#8e44ad"), H("#ece8f3"), 2, true, H("#b07cc6")); break;
                case BuildingType.ElectronicsStore: Shop(k, w, d, seed, closed, H("#1e6fb8"), H("#dfe7ee"), 2, false, H("#1e6fb8")); break;
                case BuildingType.FurnitureStore: Shop(k, w, d, seed, closed, H("#d35400"), H("#efe4d4"), 1, true, H("#e67e22")); break;
                case BuildingType.HairSalon: Shop(k, w, d, seed, closed, H("#e84393"), H("#fbe9f1"), 2, true, H("#fd79a8")); break;
                case BuildingType.Gym: Shop(k, w, d, seed, closed, H("#27ae60"), H("#d9dde1"), 1, false, H("#2d3436")); break;
                case BuildingType.GasStation: Gas(k, w, d, seed, closed); break;
                case BuildingType.CarDealer: CarDealer(k, w, d, seed, closed); break;
                case BuildingType.AutoRepair: AutoRepair(k, w, d, seed, closed); break;
                case BuildingType.Office: Office(k, w, d, seed, floors, closed); break;
                case BuildingType.Bank: Bank(k, w, d, seed, closed); break;
                case BuildingType.Farm: Farm(k, w, d, seed, season); break;
                case BuildingType.FoodPlant: Factory(k, w, d, seed, closed, H("#d8d3c8"), H("#2e86de"), true, false); break;
                case BuildingType.GarmentFactory: Factory(k, w, d, seed, closed, Brick, H("#6c5ce7"), false, true); break;
                case BuildingType.ElectronicsPlant: Factory(k, w, d, seed, closed, H("#eef1f4"), H("#0984e3"), false, false); break;
                case BuildingType.FurnitureWorkshop: Workshop(k, w, d, seed, closed); break;
                case BuildingType.Construction: Yard(k, w, d, seed, closed); break;
                case BuildingType.Trucking: Depot(k, w, d, seed, closed); break;
                case BuildingType.TownHall: TownHall(k, w, d, seed); break;
                case BuildingType.Elementary: School(k, w, d, seed, 2, false); break;
                case BuildingType.HighSchool: School(k, w, d, seed, 3, true); break;
                case BuildingType.College: College(k, w, d, seed, season); break;
                case BuildingType.Clinic: Clinic(k, w, d, seed, false); break;
                case BuildingType.Hospital: Clinic(k, w, d, seed, true); break;
                case BuildingType.Police: Station(k, w, d, seed, H("#34495e"), H("#2980b9"), false); break;
                case BuildingType.FireStation: Station(k, w, d, seed, Brick, H("#c0392b"), true); break;
                case BuildingType.Park: Park(k, w, d, seed, season); break;
                case BuildingType.BusDepot: BusDepot(k, w, d, seed); break;
                case BuildingType.FreightTerminal: Terminal(k, w, d, seed); break;
            }
            return k.ToMesh(t.ToString());
        }

        // ================================================================== bits
        static void Lot(MeshKit k, float w, float d, Color32 c, float inset = 0.25f)
        {
            k.Flat(-w / 2 + inset, -d / 2 + inset, w / 2 - inset, d / 2 - inset, 0.08f, c);
        }

        /// <summary>A painted parking area with spaces along it, from x0..x1, z0..z1, with a few parked cars.</summary>
        static void Parking(MeshKit k, float x0, float z0, float x1, float z1, ref uint s, float fill)
        {
            k.Flat(x0, z0, x1, z1, 0.1f, Asphalt);
            float depth = z1 - z0;
            for (float x = x0 + 0.6f; x < x1 - 2.2f; x += 2.8f)
            {
                k.Flat(x, z0 + depth * 0.15f, x + 0.12f, z1 - depth * 0.15f, 0.12f, Line);
                if (R(ref s) < fill) PropArt.Parked(k, new Vector3(x + 1.4f, 0.12f, (z0 + z1) / 2), R(ref s) < 0.5f ? 0 : 180, s);
            }
        }

        /// <summary>A sign board above a shop front, with a glowing strip under it at night.</summary>
        static void Sign(MeshKit k, float x0, float x1, float y0, float y1, float z, Color32 c, bool closed)
        {
            var col = closed ? H("#7f8c8d") : c;
            k.Box(new Vector3(x0, y0, z - 0.25f), new Vector3(x1, y1, z), col, MeshKit.Shade(col, 0.9f));
            if (!closed)
                k.Quad(new Vector3(x0 + 0.3f, y0 - 0.12f, z - 0.27f), new Vector3(x0 + 0.3f, y0 + 0.02f, z - 0.27f), new Vector3(x1 - 0.3f, y0 + 0.02f, z - 0.27f), new Vector3(x1 - 0.3f, y0 - 0.12f, z - 0.27f), MeshKit.Shade(c, 1.4f), true);
        }

        /// <summary>A striped awning over a shop front.</summary>
        static void Awning(MeshKit k, float x0, float x1, float y, float z, float depth, Color32 a, Color32 b)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt((x1 - x0) / 0.9f));
            float sw = (x1 - x0) / n;
            for (int i = 0; i < n; i++)
            {
                var c = i % 2 == 0 ? a : b;
                float xa = x0 + i * sw, xb = xa + sw;
                k.Quad(new Vector3(xa, y - 0.9f, z - depth), new Vector3(xa, y, z), new Vector3(xb, y, z), new Vector3(xb, y - 0.9f, z - depth), c);
                k.Quad(new Vector3(xb, y - 0.9f, z - depth), new Vector3(xb, y, z), new Vector3(xa, y, z), new Vector3(xa, y - 0.9f, z - depth), MeshKit.Shade(c, 0.6f));
            }
        }

        /// <summary>A glass shop front on the -z face from x0..x1, up to height h.</summary>
        static void Storefront(MeshKit k, float x0, float x1, float h, float z, bool closed, uint seed)
        {
            float doorW = 1.2f, mid = (x0 + x1) / 2;
            var lit = closed ? new Color32(18, 20, 26, 255) : new Color32(255, 236, 196, 255);
            k.Quad(new Vector3(x0 + 0.3f, 0.5f, z - 0.03f), new Vector3(x0 + 0.3f, h, z - 0.03f), new Vector3(mid - doorW / 2 - 0.1f, h, z - 0.03f), new Vector3(mid - doorW / 2 - 0.1f, 0.5f, z - 0.03f), lit, true);
            k.Quad(new Vector3(mid + doorW / 2 + 0.1f, 0.5f, z - 0.03f), new Vector3(mid + doorW / 2 + 0.1f, h, z - 0.03f), new Vector3(x1 - 0.3f, h, z - 0.03f), new Vector3(x1 - 0.3f, 0.5f, z - 0.03f), lit, true);
            k.Box(new Vector3(mid - doorW / 2, 0.08f, z - 0.06f), new Vector3(mid + doorW / 2, 2.3f, z), closed ? H("#6b6b6b") : Dark);
            if (closed)
            {
                // boards across the windows
                k.Box(new Vector3(x0 + 0.4f, 1.1f, z - 0.1f), new Vector3(mid - doorW, 1.4f, z - 0.04f), H("#a0784f"));
                k.Box(new Vector3(mid + doorW, 1.6f, z - 0.1f), new Vector3(x1 - 0.4f, 1.9f, z - 0.04f), H("#a0784f"));
            }
        }

        static void Tree(MeshKit k, float x, float z, float scale, int season, uint s) { PropArt.Tree(k, new Vector3(x, 0.08f, z), scale, season, s); }

        static void Flag(MeshKit k, float x, float z, float h, Color32 c)
        {
            k.Box(new Vector3(x - 0.06f, 0, z - 0.06f), new Vector3(x + 0.06f, h, z + 0.06f), Metal);
            k.Box(new Vector3(x + 0.06f, h - 1.1f, z - 0.02f), new Vector3(x + 1.7f, h - 0.1f, z + 0.02f), c);
        }

        // ================================================================== homes
        static void House(MeshKit k, float w, float d, uint s, int season)
        {
            Lot(k, w, d, Grass, 0.15f);
            var wall = HouseWalls[Next(ref s) % (uint)HouseWalls.Length];
            var roof = HouseRoofs[Next(ref s) % (uint)HouseRoofs.Length];
            bool two = R(ref s) < 0.55f, garage = R(ref s) < 0.5f, flip = R(ref s) < 0.5f;
            float bw = garage ? 5.4f : 6.6f, bd = 5.6f, h = two ? 5.6f : 3.0f;
            float bx = flip ? -w / 2 + 1.0f : w / 2 - 1.0f - bw;
            float bz = -d / 2 + 2.4f;
            // driveway and path
            float gx = flip ? bx + bw : bx - 3.0f;
            k.Flat(garage ? gx + 0.2f : bx + bw / 2 - 0.5f, -d / 2 + 0.2f, garage ? gx + 2.8f : bx + bw / 2 + 0.5f, bz, 0.1f, Paving);
            k.Box(new Vector3(bx, 0.08f, bz), new Vector3(bx + bw, h, bz + bd), wall, wall);
            k.Gable(bx, bz, bx + bw, bz + bd, h, 2.1f, roof, wall, 0.35f);
            // door, windows
            float doorX = bx + bw * 0.5f;
            k.Box(new Vector3(doorX - 0.5f, 0.08f, bz - 0.06f), new Vector3(doorX + 0.5f, 2.2f, bz), Door);
            k.Box(new Vector3(doorX - 0.9f, 2.25f, bz - 0.7f), new Vector3(doorX + 0.9f, 2.35f, bz), Trim);
            k.WindowsFront(bx + 0.3f, doorX - 0.6f, 0.9f, 2.4f, bz, 1, 1, 1.1f, 1.0f, s, 0.6f);
            k.WindowsFront(doorX + 0.6f, bx + bw - 0.3f, 0.9f, 2.4f, bz, 1, 1, 1.1f, 1.0f, s + 3, 0.6f);
            if (two) k.WindowsFront(bx + 0.3f, bx + bw - 0.3f, 3.3f, 5.0f, bz, 3, 1, 0.9f, 1.0f, s + 9, 0.5f);
            k.WindowsAround(new Vector3(bx, 0, bz), new Vector3(bx + bw, h, bz + bd), 2.4f, 2.6f, 0.8f, 1.0f, 0.9f, s + 17, 0.45f, true);
            // chimney
            if (R(ref s) < 0.6f) k.Box(new Vector3(bx + bw * 0.75f, h, bz + bd * 0.55f), new Vector3(bx + bw * 0.75f + 0.6f, h + 2.6f, bz + bd * 0.55f + 0.6f), BrickDark);
            if (garage)
            {
                k.Box(new Vector3(gx, 0.08f, bz + 0.3f), new Vector3(gx + 3.0f, 2.8f, bz + bd), MeshKit.Shade(wall, 0.95f), roof);
                k.Box(new Vector3(gx + 0.3f, 0.08f, bz + 0.24f), new Vector3(gx + 2.7f, 2.3f, bz + 0.3f), H("#d6d2c8"));
            }
            else if (R(ref s) < 0.7f) PropArt.Parked(k, new Vector3(bx - 1.6f > -w / 2 ? bx - 1.6f : bx + bw + 1.6f, 0.1f, bz - 0.5f), 0, s);
            // the yard
            if (R(ref s) < 0.7f) Tree(k, flip ? w / 2 - 1.6f : -w / 2 + 1.6f, d / 2 - 1.8f, 0.8f + R(ref s) * 0.3f, season, s);
            PropArt.Bush(k, new Vector3(bx + 0.5f, 0.08f, bz - 0.7f), 0.7f, season);
            PropArt.Bush(k, new Vector3(bx + bw - 0.5f, 0.08f, bz - 0.7f), 0.7f, season);
            // fence at the back
            k.Box(new Vector3(-w / 2 + 0.2f, 0.08f, d / 2 - 0.3f), new Vector3(w / 2 - 0.2f, 1.2f, d / 2 - 0.2f), H("#efe9dd"));
        }

        static void Apartments(MeshKit k, float w, float d, uint s, int season)
        {
            Lot(k, w, d, Grass, 0.2f);
            Color32[] walls = { H("#c46a4f"), H("#d9cbb0"), H("#a9afb5"), H("#e3d6c0"), H("#9c6b5a") };
            var wall = walls[Next(ref s) % (uint)walls.Length];
            int fl = 4 + (int)(Next(ref s) % 3);
            float fh = 3.1f, h = fl * fh + 0.6f;
            float x0 = -w / 2 + 2.2f, x1 = w / 2 - 2.2f, z0 = -d / 2 + 4.5f, z1 = d / 2 - 2.5f;
            k.Flat(-w / 2 + 0.2f, -d / 2 + 0.2f, w / 2 - 0.2f, z0 - 0.4f, 0.1f, Paving);
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), wall, Roof);
            k.Box(new Vector3(x0 - 0.1f, h, z0 - 0.1f), new Vector3(x1 + 0.1f, h + 0.6f, z0 + 0.25f), MeshKit.Shade(wall, 0.85f));
            k.Box(new Vector3(x0 - 0.1f, h, z1 - 0.25f), new Vector3(x1 + 0.1f, h + 0.6f, z1 + 0.1f), MeshKit.Shade(wall, 0.85f));
            k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h, z1), 2.6f, fh, 1.3f, 1.5f, 0.8f, s, 0.55f);
            // balconies on the front
            for (int f = 1; f < fl; f++)
                for (float x = x0 + 1.4f; x < x1 - 1.4f; x += 5.2f)
                {
                    k.Box(new Vector3(x - 1.2f, f * fh + 0.1f, z0 - 1.1f), new Vector3(x + 1.2f, f * fh + 0.3f, z0), Concrete);
                    k.Box(new Vector3(x - 1.2f, f * fh + 0.3f, z0 - 1.12f), new Vector3(x + 1.2f, f * fh + 1.2f, z0 - 1.02f), MeshKit.Shade(wall, 0.7f));
                }
            // entrance and roof boxes
            k.Box(new Vector3(-1.4f, 0.08f, z0 - 1.6f), new Vector3(1.4f, 3.0f, z0), H("#4a4f55"), Concrete);
            k.Quad(new Vector3(-1.0f, 0.3f, z0 - 1.63f), new Vector3(-1.0f, 2.5f, z0 - 1.63f), new Vector3(1.0f, 2.5f, z0 - 1.63f), new Vector3(1.0f, 0.3f, z0 - 1.63f), new Color32(255, 230, 180, 255), true);
            k.Box(new Vector3(x0 + 1.5f, h, z0 + 2), new Vector3(x0 + 3.5f, h + 1.6f, z0 + 4), Metal);
            k.Box(new Vector3(x1 - 4.0f, h, z1 - 4), new Vector3(x1 - 1.5f, h + 2.4f, z1 - 1.5f), H("#7d858d"));
            Tree(k, -w / 2 + 1.1f, -d / 2 + 1.6f, 0.9f, season, s);
            Tree(k, w / 2 - 1.1f, -d / 2 + 1.6f, 0.9f, season, s + 1);
            PropArt.Lamp(k, new Vector3(-3.2f, 0.08f, -d / 2 + 1.0f));
        }

        // ================================================================== shops
        static void Shop(MeshKit k, float w, float d, uint s, bool closed, Color32 sign, Color32 wall, int storeys, bool awning, Color32 awnColor)
        {
            Lot(k, w, d, Paving, 0.1f);
            float x0 = -w / 2 + 0.5f, x1 = w / 2 - 0.5f, z0 = -d / 2 + 2.6f, z1 = d / 2 - 0.6f;
            float h1 = 4.2f, h = storeys == 1 ? 5.0f : h1 + 3.4f;
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), wall, Roof);
            k.Box(new Vector3(x0 - 0.1f, h, z0 - 0.1f), new Vector3(x1 + 0.1f, h + 0.5f, z0 + 0.3f), MeshKit.Shade(wall, 0.85f));
            Storefront(k, x0, x1, 3.0f, z0, closed, s);
            Sign(k, x0 + 0.4f, x1 - 0.4f, 3.25f, 4.05f, z0, sign, closed);
            if (awning && !closed) Awning(k, x0 + 0.3f, x1 - 0.3f, 3.15f, z0, 1.4f, awnColor, Trim);
            if (storeys > 1) k.WindowsFront(x0 + 0.4f, x1 - 0.4f, h1 + 0.3f, h - 0.3f, z0, Mathf.Max(2, (int)((x1 - x0) / 2.4f)), 1, 1.2f, 1.6f, s, closed ? 0f : 0.5f);
            k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h, z1), 3.0f, 3.3f, 1.0f, 1.3f, 1.0f, s + 5, closed ? 0f : 0.35f, true);
            // a table or two on the pavement for places that serve
            if (awning && !closed && w > 12)
                for (float x = x0 + 2.5f; x < x1 - 2; x += 4.5f)
                {
                    k.Cylinder(new Vector3(x, 0.08f, -d / 2 + 1.2f), 0.5f, 0.5f, 0.75f, 6, Trim, Trim);
                    k.Cylinder(new Vector3(x, 2.0f, -d / 2 + 1.2f), 1.5f, 0.05f, 0.6f, 6, awnColor, awnColor);
                    k.Box(new Vector3(x - 0.04f, 0.08f, -d / 2 + 1.16f), new Vector3(x + 0.04f, 2.0f, -d / 2 + 1.24f), Metal);
                }
            PropArt.Lamp(k, new Vector3(x1 - 0.4f, 0.08f, -d / 2 + 0.5f));
        }

        static void Grocery(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, Paving, 0.1f);
            float z0 = -d / 2 + 8.0f, z1 = d / 2 - 0.8f, x0 = -w / 2 + 1, x1 = w / 2 - 1, h = 6.5f;
            Parking(k, -w / 2 + 0.5f, -d / 2 + 0.5f, w / 2 - 0.5f, z0 - 1.6f, ref s, closed ? 0.05f : 0.6f);
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), H("#e9e4d8"), Roof);
            k.Box(new Vector3(x0, h - 1.4f, z0 - 0.3f), new Vector3(x1, h + 0.6f, z0), closed ? H("#7f8c8d") : H("#27ae60"));
            Storefront(k, x0 + 1, x1 - 1, 3.6f, z0, closed, s);
            k.Box(new Vector3(-3, 3.8f, z0 - 2.2f), new Vector3(3, 4.1f, z0), H("#dfe6e9"));
            k.Box(new Vector3(x1 - 3, 0.1f, z0 - 1.2f), new Vector3(x1 - 1.2f, 1.0f, z0 - 0.4f), Metal);
        }

        static void Gas(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, Concrete, 0.1f);
            float cz = -d / 2 + 3.6f;
            // canopy over the pumps
            var red = closed ? H("#7f8c8d") : H("#e74c3c");
            k.Box(new Vector3(-w / 2 + 1.5f, 4.6f, cz - 2.6f), new Vector3(w / 2 - 7.5f, 5.3f, cz + 2.6f), red, White);
            k.Quad(new Vector3(-w / 2 + 1.7f, 4.58f, cz + 2.4f), new Vector3(-w / 2 + 1.7f, 4.58f, cz - 2.4f), new Vector3(w / 2 - 7.7f, 4.58f, cz - 2.4f), new Vector3(w / 2 - 7.7f, 4.58f, cz + 2.4f), new Color32(255, 248, 225, 255), true);
            for (float x = -w / 2 + 3.5f; x < w / 2 - 8; x += 4.5f)
            {
                k.Box(new Vector3(x - 0.2f, 0.1f, cz - 0.2f), new Vector3(x + 0.2f, 4.6f, cz + 0.2f), White);
                k.Box(new Vector3(x - 0.5f, 0.1f, cz + 0.4f), new Vector3(x + 0.5f, 1.6f, cz + 0.9f), red);
            }
            // the shop
            float sx0 = w / 2 - 6.5f, sx1 = w / 2 - 0.5f, sz0 = -d / 2 + 2.2f, sz1 = d / 2 - 0.6f;
            k.Box(new Vector3(sx0, 0.08f, sz0), new Vector3(sx1, 3.6f, sz1), H("#f5f3ee"), Roof);
            Storefront(k, sx0, sx1, 2.6f, sz0, closed, s);
            k.Box(new Vector3(-w / 2 + 0.8f, 0.1f, d / 2 - 2), new Vector3(-w / 2 + 1.2f, 6.5f, d / 2 - 1.6f), Metal);
            k.Box(new Vector3(-w / 2 + 0.2f, 5.0f, d / 2 - 2.2f), new Vector3(-w / 2 + 1.8f, 6.8f, d / 2 - 1.4f), red);
        }

        static void CarDealer(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, Paving, 0.1f);
            float z0 = 0.5f, z1 = d / 2 - 1, x0 = -w / 2 + 1.5f, x1 = w / 2 - 1.5f, h = 5.5f;
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), H("#eef1f4"), Roof);
            k.WindowsFront(x0 + 0.2f, x1 - 0.2f, 0.4f, h - 0.6f, z0, 6, 1, 2.5f, h - 1.2f, s, closed ? 0 : 0.95f);
            Sign(k, x0 + 2, x1 - 2, h - 0.2f, h + 1.2f, z0 + 0.2f, H("#0a3d62"), closed);
            // cars for sale out front
            if (!closed)
                for (float x = -w / 2 + 2.5f; x < w / 2 - 2; x += 3.2f)
                    PropArt.Parked(k, new Vector3(x, 0.1f, -d / 2 + 4.5f), 160, s + (uint)(x * 13));
            for (float x = -w / 2 + 1; x < w / 2; x += 6) Flag(k, x, -d / 2 + 0.8f, 5, H("#e74c3c"));
        }

        static void AutoRepair(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, Concrete, 0.1f);
            float z0 = -d / 2 + 2.0f, z1 = d / 2 - 0.6f, x0 = -w / 2 + 0.5f, x1 = w / 2 - 0.5f, h = 5.2f;
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), H("#bfc6cc"), Roof);
            k.Box(new Vector3(x0 + 0.6f, 0.08f, z0 - 0.06f), new Vector3(-0.3f, 3.6f, z0), H("#8d969e"));
            k.Box(new Vector3(0.3f, 0.08f, z0 - 0.06f), new Vector3(x1 - 0.6f, 3.6f, z0), H("#8d969e"));
            for (float y = 0.5f; y < 3.6f; y += 0.45f) k.Box(new Vector3(x0 + 0.6f, y, z0 - 0.08f), new Vector3(x1 - 0.6f, y + 0.05f, z0 - 0.06f), H("#737b83"));
            Sign(k, x0 + 0.5f, x1 - 0.5f, 3.9f, 4.8f, z0, H("#f39c12"), closed);
            for (int i = 0; i < 3; i++) k.Cylinder(new Vector3(x1 - 0.2f, 0.1f + i * 0.35f, -d / 2 + 1.0f), 0.4f, 0.4f, 0.32f, 7, Dark, Dark);
            if (!closed) PropArt.Parked(k, new Vector3(-2.2f, 0.1f, -d / 2 + 1.0f), 90, s);
        }

        static void Office(MeshKit k, float w, float d, uint s, int floors, bool closed)
        {
            Lot(k, w, d, Paving, 0.1f);
            int fl = floors + (int)(Next(ref s) % 4);
            float fh = 3.6f, h = fl * fh + 1;
            float x0 = -w / 2 + 3, x1 = w / 2 - 3, z0 = -d / 2 + 4, z1 = d / 2 - 2.5f;
            Color32[] glass = { H("#5d7b93"), H("#4a6a7f"), H("#6e8796"), H("#3f5a6e") };
            var frame = (Next(ref s) % 2) == 0 ? H("#d5d8dc") : H("#4d5359");
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), frame, Roof);
            k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h - 0.5f, z1), 1.9f, fh, 1.7f, 2.6f, 1.2f, s, closed ? 0 : 0.6f);
            // lobby and a crown on top
            k.Box(new Vector3(-3, 0.08f, z0 - 1.8f), new Vector3(3, 3.6f, z0), MeshKit.Shade(frame, 0.8f));
            k.Quad(new Vector3(-2.6f, 0.3f, z0 - 1.83f), new Vector3(-2.6f, 3.2f, z0 - 1.83f), new Vector3(2.6f, 3.2f, z0 - 1.83f), new Vector3(2.6f, 0.3f, z0 - 1.83f), closed ? new Color32(18, 20, 26, 255) : new Color32(255, 244, 220, 255), true);
            k.Box(new Vector3(x0 + 2, h, z0 + 2), new Vector3(x1 - 2, h + 2.4f, z1 - 2), glass[s % 4]);
            k.Box(new Vector3(-0.1f, h + 2.4f, -0.1f), new Vector3(0.1f, h + 6, 0.1f), Metal);
            PropArt.Lamp(k, new Vector3(-w / 2 + 1.2f, 0.08f, -d / 2 + 1));
            PropArt.Lamp(k, new Vector3(w / 2 - 1.2f, 0.08f, -d / 2 + 1));
        }

        static void Bank(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, Paving, 0.1f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 3, z1 = d / 2 - 0.6f, h = 7.5f;
            var stone = H("#e7e1d3");
            k.Box(new Vector3(x0 - 0.4f, 0.08f, z0 - 1.6f), new Vector3(x1 + 0.4f, 0.6f, z1), H("#d6cfbd"));
            k.Box(new Vector3(x0, 0.6f, z0), new Vector3(x1, h, z1), stone, Roof);
            for (float x = x0 + 0.8f; x <= x1 - 0.6f; x += 2.2f) k.Cylinder(new Vector3(x, 0.6f, z0 - 0.9f), 0.4f, 0.4f, h - 1.6f, 8, Trim, Trim);
            k.Box(new Vector3(x0, h - 1.0f, z0 - 1.5f), new Vector3(x1, h, z0), stone);
            // pediment
            k.Tri(new Vector3(x0, h, z0 - 1.5f), new Vector3(0, h + 2.2f, z0 - 1.5f), new Vector3(x1, h, z0 - 1.5f), MeshKit.Shade(stone, 1.05f));
            k.Quad(new Vector3(x0, h, z0 - 1.5f), new Vector3(x0, h, z1), new Vector3(0, h + 2.2f, z1), new Vector3(0, h + 2.2f, z0 - 1.5f), Roof);
            k.Quad(new Vector3(x1, h, z1), new Vector3(x1, h, z0 - 1.5f), new Vector3(0, h + 2.2f, z0 - 1.5f), new Vector3(0, h + 2.2f, z1), MeshKit.Shade(Roof, 0.85f));
            k.Tri(new Vector3(x1, h, z1), new Vector3(0, h + 2.2f, z1), new Vector3(x0, h, z1), MeshKit.Shade(stone, 0.9f));
            k.WindowsFront(x0 + 0.6f, x1 - 0.6f, 1.6f, 5.6f, z0, 4, 1, 1.0f, 3.0f, s, closed ? 0 : 0.6f);
            k.Box(new Vector3(-0.8f, 0.6f, z0 - 0.05f), new Vector3(0.8f, 3.6f, z0), H("#2f2a24"));
        }

        // ================================================================== industry
        static void Farm(MeshKit k, float w, float d, uint s, int season)
        {
            // fields in rows
            Color32 crop, soil = Soil;
            switch (season)
            {
                case 0: crop = H("#e9eef2"); soil = H("#dfe5ea"); break;
                case 1: crop = H("#7fbf4f"); break;
                case 2: crop = H("#cbb34a"); break;
                default: crop = H("#d9a441"); break;
            }
            k.Flat(-w / 2 + 0.3f, -d / 2 + 0.3f, w / 2 - 0.3f, d / 2 - 0.3f, 0.07f, soil);
            float fx1 = w / 2 - 11f;
            for (float x = -w / 2 + 1; x < fx1; x += 1.6f)
                k.Box(new Vector3(x, 0.07f, -d / 2 + 1), new Vector3(x + 0.9f, season == 0 ? 0.12f : 0.45f, d / 2 - 1), crop, MeshKit.Shade(crop, 1.1f));
            // barn, silo, farmhouse, tractor
            float bx0 = w / 2 - 9.5f, bx1 = w / 2 - 2.5f;
            k.Flat(bx0 - 1, -d / 2 + 0.4f, w / 2 - 0.4f, d / 2 - 0.4f, 0.09f, H("#a89a7c"));
            k.Box(new Vector3(bx0, 0.09f, 1), new Vector3(bx1, 5.0f, 11), H("#b03a2e"), H("#b03a2e"));
            k.Gable(bx0, 1, bx1, 11, 5.0f, 3.0f, H("#5b5f63"), H("#b03a2e"), 0.4f);
            k.Box(new Vector3(bx0 + 2, 0.09f, 0.94f), new Vector3(bx1 - 2, 3.8f, 1), Trim);
            k.Cylinder(new Vector3(w / 2 - 2.2f, 0.09f, 12.5f), 1.6f, 1.6f, 9, 10, H("#c7ccd1"), H("#c7ccd1"));
            k.Cylinder(new Vector3(w / 2 - 2.2f, 9.09f, 12.5f), 1.6f, 0, 1.6f, 10, Metal, Metal);
            k.Box(new Vector3(bx0, 0.09f, -d / 2 + 2), new Vector3(bx0 + 5, 3.0f, -d / 2 + 7), H("#f3ead6"));
            k.Gable(bx0, -d / 2 + 2, bx0 + 5, -d / 2 + 7, 3.0f, 1.8f, H("#6e4e3a"), H("#f3ead6"), 0.3f);
            k.WindowsFront(bx0 + 0.3f, bx0 + 4.7f, 0.9f, 2.4f, -d / 2 + 2, 2, 1, 1.0f, 1.0f, s, 0.6f);
            k.Push(new Vector3(bx1 - 1, 0.09f, -d / 2 + 4), 30);
            k.Box(new Vector3(-0.9f, 0.6f, -1.6f), new Vector3(0.9f, 1.6f, 1.2f), H("#2e8b3a"));
            k.Box(new Vector3(-0.7f, 1.6f, -0.2f), new Vector3(0.7f, 2.8f, 1.1f), H("#2e8b3a"));
            k.Cylinder(new Vector3(-1.1f, 0, 0.6f), 0.9f, 0.9f, 0.5f, 8, Dark, Dark);
            k.Cylinder(new Vector3(1.1f, 0, 0.6f), 0.9f, 0.9f, 0.5f, 8, Dark, Dark);
            k.Pop();
            k.Box(new Vector3(-w / 2 + 0.3f, 0.07f, -d / 2 + 0.3f), new Vector3(w / 2 - 0.3f, 1.0f, -d / 2 + 0.4f), H("#a0784f"));
        }

        static void Factory(MeshKit k, float w, float d, uint s, bool closed, Color32 wall, Color32 accent, bool tanks, bool sawtooth)
        {
            Lot(k, w, d, Concrete, 0.1f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 3.5f, z1 = d / 2 - 1, h = 8f;
            if (tanks) x1 = w / 2 - 6;
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), wall, Roof);
            k.Box(new Vector3(x0, h - 1.2f, z0 - 0.15f), new Vector3(x1, h - 0.4f, z0), closed ? H("#7f8c8d") : accent);
            if (sawtooth)
                for (float z = z0; z < z1 - 0.5f; z += 3.2f)
                {
                    float ze = Mathf.Min(z + 3.2f, z1);
                    k.Quad(new Vector3(x0, h, z), new Vector3(x0, h + 2.2f, z), new Vector3(x1, h + 2.2f, z), new Vector3(x1, h, z), new Color32(240, 220, 170, 255), true);
                    k.Quad(new Vector3(x0, h + 2.2f, z), new Vector3(x0, h, ze), new Vector3(x1, h, ze), new Vector3(x1, h + 2.2f, z), Roof);
                    k.Tri(new Vector3(x0, h, ze), new Vector3(x0, h + 2.2f, z), new Vector3(x0, h, z), MeshKit.Shade(wall, 0.9f));
                    k.Tri(new Vector3(x1, h, z), new Vector3(x1, h + 2.2f, z), new Vector3(x1, h, ze), MeshKit.Shade(wall, 0.9f));
                }
            k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h - 1.6f, z1), 3.2f, 3.2f, 2.2f, 1.2f, 3.6f, s, closed ? 0 : 0.7f);
            // loading doors and a chimney
            k.Box(new Vector3(x0 + 1, 0.08f, z0 - 0.06f), new Vector3(x0 + 4.5f, 4.0f, z0), H("#8d969e"));
            k.Box(new Vector3(x1 - 4.5f, 0.08f, z0 - 0.06f), new Vector3(x1 - 1, 4.0f, z0), H("#8d969e"));
            k.Cylinder(new Vector3(x1 - 2.5f, h, z1 - 2.5f), 0.9f, 0.7f, 8, 8, BrickDark, Dark);
            if (tanks)
            {
                k.Cylinder(new Vector3(w / 2 - 3.2f, 0.08f, -2), 2.0f, 2.0f, 7, 10, H("#dfe4e8"), H("#dfe4e8"));
                k.Cylinder(new Vector3(w / 2 - 3.2f, 0.08f, 4), 2.0f, 2.0f, 9, 10, H("#dfe4e8"), H("#dfe4e8"));
            }
            if (!closed) PropArt.Parked(k, new Vector3(-w / 2 + 3, 0.1f, -d / 2 + 1.5f), 90, s);
        }

        static void Workshop(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, H("#a89a7c"), 0.1f);
            var wood = H("#9a6a3f");
            k.Box(new Vector3(-w / 2 + 1, 0.08f, -d / 2 + 4), new Vector3(w / 2 - 6, 6, d / 2 - 1), wood, wood);
            k.Gable(-w / 2 + 1, -d / 2 + 4, w / 2 - 6, d / 2 - 1, 6, 3, H("#5e4634"), wood, 0.5f);
            k.WindowsAround(new Vector3(-w / 2 + 1, 0, -d / 2 + 4), new Vector3(w / 2 - 6, 5, d / 2 - 1), 3.5f, 3, 1.6f, 1.2f, 2.2f, s, closed ? 0 : 0.6f);
            k.Box(new Vector3(-3, 0.08f, -d / 2 + 3.94f), new Vector3(1, 4, -d / 2 + 4), H("#5e4634"));
            // stacks of timber
            for (int i = 0; i < 3; i++)
                k.Box(new Vector3(w / 2 - 5, 0.08f, -d / 2 + 2 + i * 5), new Vector3(w / 2 - 1, 1.2f + (i % 2) * 0.6f, -d / 2 + 5 + i * 5), H("#c69c6d"));
            Sign(k, -w / 2 + 2, w / 2 - 8, 6.4f, 7.4f, -d / 2 + 4, H("#d35400"), closed);
        }

        static void Yard(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, H("#b8a585"), 0.1f);
            // office cabin, materials, machines, a fence
            k.Box(new Vector3(-w / 2 + 1, 0.08f, -d / 2 + 2), new Vector3(-w / 2 + 8, 3.0f, -d / 2 + 5), H("#f1c40f"), Roof);
            k.WindowsFront(-w / 2 + 1.5f, -w / 2 + 7.5f, 1, 2.4f, -d / 2 + 2, 3, 1, 1.0f, 0.9f, s, closed ? 0 : 0.7f);
            k.Box(new Vector3(-w / 2 + 1, 0.08f, d / 2 - 7), new Vector3(-w / 2 + 6, 1.6f, d / 2 - 2), H("#c69c6d"));
            k.Box(new Vector3(-w / 2 + 7, 0.08f, d / 2 - 6), new Vector3(-w / 2 + 11, 1.0f, d / 2 - 2), Concrete);
            k.Cylinder(new Vector3(2, 0.08f, d / 2 - 4), 2.5f, 0, 2.5f, 8, H("#9b8b70"), H("#9b8b70"));
            if (!closed)
            {
                k.Push(new Vector3(w / 2 - 5, 0.08f, -1), 20);
                k.Box(new Vector3(-1.4f, 0.4f, -2), new Vector3(1.4f, 2.2f, 2), H("#f39c12"));
                k.Box(new Vector3(-1.1f, 2.2f, -0.5f), new Vector3(1.1f, 3.6f, 1.6f), H("#f39c12"));
                k.Box(new Vector3(-0.25f, 2.0f, -5.5f), new Vector3(0.25f, 2.5f, -2), H("#e67e22"));
                k.Box(new Vector3(-1.6f, 0, -2.2f), new Vector3(1.6f, 0.6f, 2.2f), Dark);
                k.Pop();
                // a tower crane
                float cx = w / 2 - 3, cz = d / 2 - 3;
                for (float y = 0; y < 16; y += 2) k.Box(new Vector3(cx - 0.5f, y, cz - 0.5f), new Vector3(cx + 0.5f, y + 1.8f, cz + 0.5f), H("#f1c40f"));
                k.Box(new Vector3(cx - 12, 16, cz - 0.4f), new Vector3(cx + 4, 16.8f, cz + 0.4f), H("#f1c40f"));
                k.Box(new Vector3(cx + 2, 15, cz - 1), new Vector3(cx + 4, 16, cz + 1), Concrete);
            }
            Fence(k, w, d, 0.3f);
        }

        static void Fence(MeshKit k, float w, float d, float inset)
        {
            var c = H("#9aa3ab");
            k.Box(new Vector3(-w / 2 + inset, 0.08f, d / 2 - inset - 0.1f), new Vector3(w / 2 - inset, 1.8f, d / 2 - inset), c);
            k.Box(new Vector3(-w / 2 + inset, 0.08f, -d / 2 + inset), new Vector3(-w / 2 + inset + 0.1f, 1.8f, d / 2 - inset), c);
            k.Box(new Vector3(w / 2 - inset - 0.1f, 0.08f, -d / 2 + inset), new Vector3(w / 2 - inset, 1.8f, d / 2 - inset), c);
        }

        static void Depot(MeshKit k, float w, float d, uint s, bool closed)
        {
            Lot(k, w, d, Concrete, 0.1f);
            float z0 = 0, z1 = d / 2 - 1, x0 = -w / 2 + 1, x1 = w / 2 - 1, h = 7;
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), H("#c9ced3"), Roof);
            for (float x = x0 + 1; x < x1 - 3; x += 4)
                k.Box(new Vector3(x, 0.9f, z0 - 0.06f), new Vector3(x + 3, 4.4f, z0), H("#7b848c"));
            k.Box(new Vector3(x0, 0.08f, z0 - 1.2f), new Vector3(x1, 0.9f, z0), Concrete);
            Sign(k, x0 + 2, x1 - 2, h - 1.2f, h - 0.2f, z0, H("#0a79df"), closed);
            if (!closed)
                for (int i = 0; i < 2; i++)
                {
                    k.Push(new Vector3(x0 + 4 + i * 8, 0.08f, -d / 2 + 4.5f), 0);
                    PropArt.Truck(k, H("#ecf0f1"), i == 0 ? H("#0a79df") : H("#e1e5e8"));
                    k.Pop();
                }
        }

        // ================================================================== public
        static void TownHall(MeshKit k, float w, float d, uint s)
        {
            Lot(k, w, d, Paving, 0.1f);
            float x0 = -w / 2 + 2, x1 = w / 2 - 2, z0 = -d / 2 + 6, z1 = d / 2 - 1.5f, h = 9;
            var stone = H("#ece6d8");
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), stone, Roof);
            k.Box(new Vector3(x0 - 0.2f, h, z0 - 0.2f), new Vector3(x1 + 0.2f, h + 0.7f, z1 + 0.2f), H("#d8d0bd"));
            k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h, z1), 2.4f, 3.6f, 1.1f, 2.2f, 1.0f, s, 0.5f, true);
            // portico and steps
            k.Box(new Vector3(-5, 0.08f, z0 - 3.0f), new Vector3(5, 0.7f, z0), H("#d6cfbd"));
            for (float x = -4.2f; x <= 4.3f; x += 2.1f) k.Cylinder(new Vector3(x, 0.7f, z0 - 2.2f), 0.45f, 0.45f, 6.6f, 8, Trim, Trim);
            k.Box(new Vector3(-5, 7.3f, z0 - 3.0f), new Vector3(5, 8.2f, z0), stone);
            k.Tri(new Vector3(-5, 8.2f, z0 - 3.0f), new Vector3(0, 10.6f, z0 - 3.0f), new Vector3(5, 8.2f, z0 - 3.0f), MeshKit.Shade(stone, 1.05f));
            k.Quad(new Vector3(-5, 8.2f, z0 - 3.0f), new Vector3(-5, 8.2f, z0), new Vector3(0, 10.6f, z0), new Vector3(0, 10.6f, z0 - 3.0f), Roof);
            k.Quad(new Vector3(5, 8.2f, z0), new Vector3(5, 8.2f, z0 - 3.0f), new Vector3(0, 10.6f, z0 - 3.0f), new Vector3(0, 10.6f, z0), MeshKit.Shade(Roof, 0.85f));
            // clock tower and dome
            k.Box(new Vector3(-2, h + 0.7f, 0), new Vector3(2, h + 5.5f, 4), stone);
            k.Box(new Vector3(-1.1f, h + 2.0f, -0.15f), new Vector3(1.1f, h + 4.2f, 0), White);
            k.Box(new Vector3(-0.08f, h + 3.0f, -0.2f), new Vector3(0.08f, h + 3.9f, -0.15f), Dark);
            k.Cylinder(new Vector3(0, h + 5.5f, 2), 2.3f, 0, 3.4f, 10, H("#4f7f6a"), H("#4f7f6a"));
            Flag(k, -w / 2 + 2, -d / 2 + 2, 9, H("#2e86de"));
            Flag(k, w / 2 - 3.8f, -d / 2 + 2, 9, H("#c0392b"));
        }

        static void School(MeshKit k, float w, float d, uint s, int floors, bool field)
        {
            Lot(k, w, d, Grass, 0.1f);
            float h = floors * 3.6f + 0.5f;
            float x0 = -w / 2 + 1.5f, x1 = field ? -w / 2 + w * 0.55f : w / 2 - 1.5f, z0 = -d / 2 + 3.5f, z1 = z0 + 7;
            k.Flat(-w / 2 + 0.3f, -d / 2 + 0.3f, x1, z0, 0.1f, Paving);
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), Brick, Roof);
            k.Box(new Vector3(x0, 0.08f, z1), new Vector3(x0 + 7, h, d / 2 - 1.5f), Brick, Roof);
            k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h, z1), 2.2f, 3.6f, 1.5f, 1.7f, 0.9f, s, 0.3f);
            k.WindowsAround(new Vector3(x0, 0, z1), new Vector3(x0 + 7, h, d / 2 - 1.5f), 2.2f, 3.6f, 1.5f, 1.7f, 0.9f, s + 3, 0.3f, true);
            k.Box(new Vector3((x0 + x1) / 2 - 2, 0.08f, z0 - 1.5f), new Vector3((x0 + x1) / 2 + 2, 3.2f, z0), Trim, Concrete);
            Sign(k, (x0 + x1) / 2 - 3, (x0 + x1) / 2 + 3, h - 1.4f, h - 0.4f, z0, H("#1e3799"), false);
            Flag(k, x0 + 1, -d / 2 + 1.2f, 8, H("#2e86de"));
            if (field)
            {
                // a sports field with a running track
                float fx0 = x1 + 1.5f, fx1 = w / 2 - 1, fz0 = -d / 2 + 1.5f, fz1 = d / 2 - 1.5f;
                k.Flat(fx0, fz0, fx1, fz1, 0.09f, H("#b5523b"));
                k.Flat(fx0 + 1.4f, fz0 + 1.4f, fx1 - 1.4f, fz1 - 1.4f, 0.11f, H("#5fa04a"));
                k.Flat((fx0 + fx1) / 2 - 0.08f, fz0 + 1.4f, (fx0 + fx1) / 2 + 0.08f, fz1 - 1.4f, 0.12f, Line);
                k.Box(new Vector3(fx0 + 2, 0.1f, (fz0 + fz1) / 2 - 1.5f), new Vector3(fx0 + 2.15f, 2.4f, (fz0 + fz1) / 2 + 1.5f), White);
                k.Box(new Vector3(fx1 - 2.15f, 0.1f, (fz0 + fz1) / 2 - 1.5f), new Vector3(fx1 - 2, 2.4f, (fz0 + fz1) / 2 + 1.5f), White);
            }
            else
            {
                // a playground
                float px = x0 + 10, pz = z1 + 4;
                k.Flat(px - 1, pz - 1, w / 2 - 1.5f, d / 2 - 1.5f, 0.1f, H("#d9b98c"));
                k.Box(new Vector3(px, 0.1f, pz), new Vector3(px + 0.15f, 2.6f, pz + 0.15f), H("#e74c3c"));
                k.Box(new Vector3(px + 3, 0.1f, pz), new Vector3(px + 3.15f, 2.6f, pz + 0.15f), H("#e74c3c"));
                k.Box(new Vector3(px, 2.5f, pz), new Vector3(px + 3.15f, 2.65f, pz + 0.15f), H("#e74c3c"));
                k.Box(new Vector3(px + 0.8f, 0.6f, pz - 0.2f), new Vector3(px + 1.4f, 0.7f, pz + 0.35f), H("#f1c40f"));
                k.Box(new Vector3(px + 1.8f, 0.6f, pz - 0.2f), new Vector3(px + 2.4f, 0.7f, pz + 0.35f), H("#3498db"));
                k.Quad(new Vector3(px + 6.2f, 0.1f, pz + 3), new Vector3(px + 6.2f, 2.0f, pz), new Vector3(px + 5, 2.0f, pz), new Vector3(px + 5, 0.1f, pz + 3), H("#1abc9c"));
            }
        }

        static void College(MeshKit k, float w, float d, uint s, int season)
        {
            Lot(k, w, d, Grass, 0.1f);
            var stone = H("#c9a27e");
            // a quad with paths, three halls and a clock tower
            k.Flat(-1, -d / 2 + 0.3f, 1, d / 2 - 8, 0.1f, Paving);
            k.Flat(-w / 2 + 8, -2, w / 2 - 8, 0, 0.1f, Paving);
            k.Box(new Vector3(-w / 2 + 1, 0.08f, -d / 2 + 3), new Vector3(-w / 2 + 8, 10, d / 2 - 3), stone, Roof);
            k.Gable(-w / 2 + 1, -d / 2 + 3, -w / 2 + 8, d / 2 - 3, 10, 3, H("#5d4a3c"), stone);
            k.Box(new Vector3(w / 2 - 8, 0.08f, -d / 2 + 3), new Vector3(w / 2 - 1, 10, d / 2 - 3), stone, Roof);
            k.Gable(w / 2 - 8, -d / 2 + 3, w / 2 - 1, d / 2 - 3, 10, 3, H("#5d4a3c"), stone);
            k.Box(new Vector3(-w / 2 + 8, 0.08f, d / 2 - 8), new Vector3(w / 2 - 8, 11, d / 2 - 1), stone, Roof);
            k.WindowsAround(new Vector3(-w / 2 + 1, 0, -d / 2 + 3), new Vector3(-w / 2 + 8, 10, d / 2 - 3), 2.4f, 3.3f, 1.1f, 2.0f, 1.0f, s, 0.4f);
            k.WindowsAround(new Vector3(w / 2 - 8, 0, -d / 2 + 3), new Vector3(w / 2 - 1, 10, d / 2 - 3), 2.4f, 3.3f, 1.1f, 2.0f, 1.0f, s + 1, 0.4f);
            k.WindowsAround(new Vector3(-w / 2 + 8, 0, d / 2 - 8), new Vector3(w / 2 - 8, 11, d / 2 - 1), 2.4f, 3.3f, 1.1f, 2.0f, 1.0f, s + 2, 0.4f);
            k.Box(new Vector3(-2, 11, d / 2 - 6), new Vector3(2, 19, d / 2 - 2), stone);
            k.Box(new Vector3(-1.1f, 14.4f, d / 2 - 6.15f), new Vector3(1.1f, 16.6f, d / 2 - 6), White);
            k.Box(new Vector3(-0.08f, 15.4f, d / 2 - 6.2f), new Vector3(0.08f, 16.3f, d / 2 - 6.15f), Dark);
            k.Hip(-2, d / 2 - 6, 2, d / 2 - 2, 19, 3, H("#3f6f5a"), 0.3f);
            for (int i = 0; i < 4; i++) Tree(k, -6 + i * 4, -d / 2 + 6 + (i % 2) * 6, 1.0f, season, s + (uint)i);
        }

        static void Clinic(MeshKit k, float w, float d, uint s, bool hospital)
        {
            Lot(k, w, d, Paving, 0.1f);
            if (!hospital)
            {
                float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 3, z1 = d / 2 - 0.6f, h = 7.2f;
                k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), White, Roof);
                k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, h, z1), 2.4f, 3.4f, 1.6f, 1.5f, 0.9f, s, 0.6f, true);
                k.Box(new Vector3(-2.5f, 0.08f, z0 - 2.0f), new Vector3(2.5f, 3.3f, z0), H("#dfe6e9"));
                k.Quad(new Vector3(-2.1f, 0.3f, z0 - 2.03f), new Vector3(-2.1f, 2.8f, z0 - 2.03f), new Vector3(2.1f, 2.8f, z0 - 2.03f), new Vector3(2.1f, 0.3f, z0 - 2.03f), new Color32(255, 248, 230, 255), true);
                Cross(k, x1 - 2.2f, h - 1.8f, z0 - 0.2f, 1.0f);
            }
            else
            {
                float x0 = -w / 2 + 3, x1 = w / 2 - 3, z0 = -d / 2 + 5, z1 = d / 2 - 2;
                k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, 8, z1), White, Roof);
                k.Box(new Vector3(-6, 8, z0 + 3), new Vector3(6, 24, z1 - 4), H("#eef2f4"), Roof);
                k.WindowsAround(new Vector3(x0, 0, z0), new Vector3(x1, 8, z1), 2.4f, 3.6f, 1.6f, 1.6f, 0.9f, s, 0.6f);
                k.WindowsAround(new Vector3(-6, 8, z0 + 3), new Vector3(6, 24, z1 - 4), 2.2f, 3.4f, 1.6f, 1.6f, 8.8f, s + 1, 0.65f);
                Cross(k, 0, 20, z0 + 2.75f, 2.0f);
                // helipad
                k.Cylinder(new Vector3(0, 24, (z0 + z1) / 2 - 0.5f), 4, 4, 0.2f, 12, H("#59626b"), H("#59626b"));
                k.Box(new Vector3(-1.2f, 24.2f, (z0 + z1) / 2 - 2), new Vector3(-0.7f, 24.25f, (z0 + z1) / 2 + 1), White);
                k.Box(new Vector3(0.7f, 24.2f, (z0 + z1) / 2 - 2), new Vector3(1.2f, 24.25f, (z0 + z1) / 2 + 1), White);
                k.Box(new Vector3(-0.7f, 24.2f, (z0 + z1) / 2 - 0.75f), new Vector3(0.7f, 24.25f, (z0 + z1) / 2 - 0.25f), White);
                k.Box(new Vector3(-4, 0.08f, z0 - 3), new Vector3(4, 3.5f, z0), H("#dfe6e9"));
                PropArt.Parked(k, new Vector3(-w / 2 + 3, 0.1f, -d / 2 + 2.5f), 90, 3);
            }
        }

        static void Cross(MeshKit k, float x, float y, float z, float s)
        {
            var red = new Color32(230, 50, 50, 255);
            k.Quad(new Vector3(x - 0.3f * s, y - 1 * s, z), new Vector3(x - 0.3f * s, y + 1 * s, z), new Vector3(x + 0.3f * s, y + 1 * s, z), new Vector3(x + 0.3f * s, y - 1 * s, z), red, true);
            k.Quad(new Vector3(x - 1 * s, y - 0.3f * s, z - 0.01f), new Vector3(x - 1 * s, y + 0.3f * s, z - 0.01f), new Vector3(x + 1 * s, y + 0.3f * s, z - 0.01f), new Vector3(x + 1 * s, y - 0.3f * s, z - 0.01f), red, true);
            k.Box(new Vector3(x - 1.1f * s, y - 1.1f * s, z + 0.01f), new Vector3(x + 1.1f * s, y + 1.1f * s, z + 0.15f), White);
        }

        static void Station(MeshKit k, float w, float d, uint s, Color32 wall, Color32 accent, bool fire)
        {
            Lot(k, w, d, Concrete, 0.1f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 2.6f, z1 = d / 2 - 0.6f, h = 7.4f;
            k.Box(new Vector3(x0, 0.08f, z0), new Vector3(x1, h, z1), wall, Roof);
            k.Box(new Vector3(x0, h - 1, z0 - 0.15f), new Vector3(x1, h - 0.3f, z0), accent);
            if (fire)
            {
                for (int i = 0; i < 2; i++)
                {
                    float gx = x0 + 1.5f + i * 5;
                    k.Box(new Vector3(gx, 0.08f, z0 - 0.06f), new Vector3(gx + 4, 4.6f, z0), H("#ecf0f1"));
                    k.WindowsFront(gx + 0.3f, gx + 3.7f, 3.0f, 4.3f, z0 - 0.07f, 3, 1, 0.9f, 0.8f, s, 0.8f);
                }
                k.Push(new Vector3(x0 + 5.5f, 0.1f, -d / 2 + 1.35f), 90, new Vector3(0.9f, 0.9f, 0.9f));
                PropArt.Truck(k, H("#c0392b"), H("#e74c3c"));
                k.Pop();
            }
            else
            {
                k.WindowsFront(x0 + 0.5f, x1 - 0.5f, 1, 6, z0, 4, 2, 1.4f, 1.6f, s, 0.7f);
                k.Box(new Vector3(-1, 0.08f, z0 - 0.06f), new Vector3(1, 2.6f, z0), Dark);
                PropArt.Parked(k, new Vector3(x1 - 3, 0.1f, -d / 2 + 1.2f), 90, 1);
                k.Box(new Vector3(x1 - 3.4f, 1.48f, -d / 2 + 0.9f), new Vector3(x1 - 2.6f, 1.68f, -d / 2 + 1.5f), accent);
            }
            Flag(k, x1 - 0.5f, -d / 2 + 0.6f, 8, H("#2e86de"));
        }

        static void Park(MeshKit k, float w, float d, uint s, int season)
        {
            Lot(k, w, d, season == 0 ? H("#e8eef0") : H("#78b356"), 0.1f);
            k.Flat(-1, -d / 2 + 0.3f, 1, d / 2 - 0.3f, 0.11f, H("#d8c9a3"));
            k.Flat(-w / 2 + 0.3f, -1, w / 2 - 0.3f, 1, 0.11f, H("#d8c9a3"));
            k.Cylinder(new Vector3(0, 0.1f, 0), 2.6f, 2.6f, 0.6f, 12, Concrete, H("#5dade2"));
            k.Cylinder(new Vector3(0, 0.7f, 0), 0.5f, 0.3f, 1.6f, 8, Concrete, Concrete);
            for (int i = 0; i < 6; i++)
            {
                float a = i * 1.05f + 0.4f, r = 6.5f + (i % 2) * 1.5f;
                Tree(k, Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0.9f + (i % 3) * 0.15f, season, s + (uint)i);
            }
            PropArt.Bench(k, new Vector3(3.6f, 0.1f, -2.2f), 0);
            PropArt.Bench(k, new Vector3(-3.6f, 0.1f, 2.2f), 180);
            PropArt.Lamp(k, new Vector3(2, 0.1f, -d / 2 + 2));
            PropArt.Lamp(k, new Vector3(-2, 0.1f, d / 2 - 2));
        }

        static void BusDepot(MeshKit k, float w, float d, uint s)
        {
            Lot(k, w, d, Asphalt, 0.1f);
            k.Box(new Vector3(-w / 2 + 1, 0.08f, d / 2 - 6), new Vector3(w / 2 - 1, 6, d / 2 - 1), H("#d0d6db"), Roof);
            for (float x = -w / 2 + 2; x < w / 2 - 3; x += 4) k.Box(new Vector3(x, 0.08f, d / 2 - 6.06f), new Vector3(x + 3.2f, 4.6f, d / 2 - 6), H("#7f8c8d"));
            k.Box(new Vector3(-w / 2 + 1, 4.4f, -d / 2 + 1), new Vector3(w / 2 - 1, 4.8f, 1), H("#2980b9"), H("#e1e8ed"));
            for (float x = -w / 2 + 1.5f; x < w / 2; x += 6) k.Box(new Vector3(x, 0.08f, -d / 2 + 1), new Vector3(x + 0.25f, 4.4f, -d / 2 + 1.25f), Metal);
            for (int i = 0; i < 3; i++) { k.Push(new Vector3(-6 + i * 6, 0.08f, -d / 2 + 6.5f), 0); PropArt.Bus(k, i == 1 ? H("#f1c40f") : H("#2980b9")); k.Pop(); }
        }

        static void Terminal(MeshKit k, float w, float d, uint s)
        {
            Lot(k, w, d, Concrete, 0.1f);
            k.Box(new Vector3(-w / 2 + 1, 0.08f, 0), new Vector3(w / 2 - 1, 8, d / 2 - 1), H("#b0b8bf"), Roof);
            for (float x = -w / 2 + 2; x < w / 2 - 3; x += 4) k.Box(new Vector3(x, 0.9f, -0.06f), new Vector3(x + 3, 4.6f, 0), H("#6c7a89"));
            Color32[] c = { H("#c0392b"), H("#2980b9"), H("#27ae60"), H("#f39c12"), H("#8e44ad") };
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 1 + i % 2; j++)
                    k.Box(new Vector3(-w / 2 + 1.5f + i * 4.4f, 0.08f + j * 2.6f, -d / 2 + 1.5f), new Vector3(-w / 2 + 4.0f + i * 4.4f, 2.68f + j * 2.6f, -d / 2 + 8.0f), c[(i + j) % 5]);
            // a green highway sign
            k.Box(new Vector3(w / 2 - 1.2f, 0.08f, -d / 2 + 1), new Vector3(w / 2 - 1, 5.5f, -d / 2 + 1.2f), Metal);
            k.Box(new Vector3(w / 2 - 4.8f, 4.2f, -d / 2 + 0.85f), new Vector3(w / 2 - 0.6f, 6.2f, -d / 2 + 1.0f), H("#1e7a3c"));
        }

        // ================================================================== building sites
        /// <summary>A site under construction: footings, a frame going up with the work done, a fence and (for big jobs) a crane.</summary>
        public static Mesh Site(float w, float d, float progress, float height, bool started)
        {
            var k = new MeshKit();
            Lot(k, w, d, Soil, 0.1f);
            if (started)
            {
                k.Box(new Vector3(-w / 2 + 1, 0.08f, -d / 2 + 1.5f), new Vector3(w / 2 - 1, 0.5f, d / 2 - 1), Concrete);
                float top = Mathf.Max(0.6f, height * progress);
                float fh = 3.2f;
                for (float x = -w / 2 + 1.2f; x <= w / 2 - 1.1f; x += Mathf.Max(2.5f, (w - 2.4f) / 4))
                    for (float z = -d / 2 + 1.7f; z <= d / 2 - 1.1f; z += Mathf.Max(2.5f, (d - 2.6f) / 3))
                        k.Box(new Vector3(x - 0.15f, 0.5f, z - 0.15f), new Vector3(x + 0.15f, top, z + 0.15f), H("#9aa3ab"));
                for (float y = fh; y < top; y += fh)
                    k.Box(new Vector3(-w / 2 + 1, y, -d / 2 + 1.5f), new Vector3(w / 2 - 1, y + 0.25f, d / 2 - 1), Concrete);
                if (progress > 0.6f)
                    k.Box(new Vector3(-w / 2 + 1.05f, 0.5f, -d / 2 + 1.55f), new Vector3(w / 2 - 1.05f, Mathf.Min(top, height * (progress - 0.5f) * 2), d / 2 - 1.05f), H("#d7cbb5"));
                if (w * d >= 350)
                {
                    float cx = w / 2 - 1.5f, cz = d / 2 - 1.5f, ch = height + 8;
                    for (float y = 0; y < ch; y += 2) k.Box(new Vector3(cx - 0.45f, y, cz - 0.45f), new Vector3(cx + 0.45f, y + 1.8f, cz + 0.45f), H("#f1c40f"));
                    k.Box(new Vector3(cx - w * 0.9f, ch, cz - 0.35f), new Vector3(cx + 3, ch + 0.7f, cz + 0.35f), H("#f1c40f"));
                }
            }
            else
            {
                for (int i = 0; i < 3; i++) k.Box(new Vector3(-3 + i * 2.2f, 0.08f, -1), new Vector3(-1.6f + i * 2.2f, 0.9f, 1.5f), H("#c69c6d"));
            }
            // a site fence with a gap at the front
            var f = H("#e67e22");
            k.Box(new Vector3(-w / 2 + 0.3f, 0.08f, -d / 2 + 0.3f), new Vector3(-1.5f, 1.3f, -d / 2 + 0.4f), f);
            k.Box(new Vector3(1.5f, 0.08f, -d / 2 + 0.3f), new Vector3(w / 2 - 0.3f, 1.3f, -d / 2 + 0.4f), f);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.08f, d / 2 - 0.4f), new Vector3(w / 2 - 0.3f, 1.3f, d / 2 - 0.3f), f);
            k.Box(new Vector3(-w / 2 + 0.3f, 0.08f, -d / 2 + 0.3f), new Vector3(-w / 2 + 0.4f, 1.3f, d / 2 - 0.3f), f);
            k.Box(new Vector3(w / 2 - 0.4f, 0.08f, -d / 2 + 0.3f), new Vector3(w / 2 - 0.3f, 1.3f, d / 2 - 0.3f), f);
            return k.ToMesh("site");
        }

        /// <summary>How tall a finished building of this type is, for the site's frame.</summary>
        public static float Height(BuildingType t, int floors)
        {
            switch (t)
            {
                case BuildingType.House: return 6;
                case BuildingType.Apartments: return 17;
                case BuildingType.Office: return floors * 3.6f + 4;
                case BuildingType.Hospital: return 24;
                case BuildingType.College: return 11;
                case BuildingType.Park: return 1;
                case BuildingType.Farm: return 5;
                default: return 7;
            }
        }
    }
}
