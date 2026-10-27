using System.Collections.Generic;
using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>Rows of the signs atlas (tools/textures/signs.py): keep them in step.</summary>
    public static class SignRow
    {
        public const int Market = 0, Grill = 1, Coffee = 2, Bar = 3, Apparel = 4, Electronics = 5, Furniture = 6, Fuel = 7, AutoSales = 8,
            AutoRepair = 9, Hair = 10, Fitness = 11, Bank = 12, Offices = 13, Police = 14, Fire = 15, Clinic = 16, Hospital = 17, TownHall = 18,
            Elementary = 19, HighSchool = 20, College = 21, BusDepot = 22, Logistics = 23, Builders = 24, Foods = 25, ApparelCo = 26, Tech = 27,
            Workshop = 28, Apartments = 29, RealEstate = 30, Open = 31;
    }

    /// <summary>
    /// A model for every kind of building, made in code with textured facades (the Mobland approach): bay-mapped window
    /// atlases that light up at night, brick, siding, stucco and metal walls, shingle and membrane roofs, and the details
    /// that make a place read as real: cornices, canopies, balconies, signs, rooftop plant, porches, chimneys, yards.
    /// Each is built in its own frame: the lot is w by d metres centred on the origin, the street side along -z.
    /// </summary>
    public static partial class BuildingArt
    {
        // ================================================================== materials and randomness
        static Material S(string n, float glow = 0f) { return MaterialLibrary.Surface(n, glow); }
        static Material T(string n, Color c) { return MaterialLibrary.Tinted(n, c); }
        static Material C(Color32 c, float smooth = 0.2f, float metal = 0f) { return MaterialLibrary.Get(c, smooth, metal); }
        static Color32 H(string s) { return MeshKit.Hex(s); }
        static uint Next(ref uint s) { s = s * 1664525u + 1013904223u; return s >> 8; }
        static float R(ref uint s) { return (Next(ref s) & 0xFFFF) / 65535f; }
        static float Range(ref uint s, float a, float b) { return Mathf.Lerp(a, b, R(ref s)); }
        static T Pick<T>(ref uint s, T[] arr) { return arr[Next(ref s) % (uint)arr.Length]; }

        static readonly Color32 White = H("#f2f1ec"), Trim = H("#f4f2ec"), Dark = H("#2f3338"), Metal = H("#9aa3ab"), Concrete = H("#b9b6ad");

        public static Color GrassTint(int season)
        {
            switch (season)
            {
                // the grass texture is a fresh green; pull it toward a lived-in lawn for each season
                case 1: return new Color(0.92f, 0.86f, 0.96f);
                case 2: return new Color(0.83f, 0.77f, 0.9f);
                case 3: return new Color(1.04f, 0.8f, 0.72f);
                default: return Color.white;
            }
        }

        public static Material GrassMat(int season) { return season == 0 ? S("snow") : T("grass", GrassTint(season)); }
        static Material RoofMat(string name, Color tint, int season) { return season == 0 ? S("snow") : T(name, tint); }

        static void Lot(MeshKit k, float w, float d, Material m, float inset = 0.1f, float tile = 6f)
        {
            k.Flat(-w / 2 + inset, -d / 2 + inset, w / 2 - inset, d / 2 - inset, 0.06f, m, tile);
        }

        static void Tree(MeshKit k, float x, float z, float scale, int season, uint s) { PropArt.Tree(k, new Vector3(x, 0.06f, z), scale, season, s); }

        /// <summary>Where cars park on a lot, in the building's own frame (x, z, yaw): a house's driveway, a dealer's bays, a
        /// garage's forecourt. The house style and side are drawn from the same seed as Build uses.</summary>
        public static List<Vector3> ParkingSpots(BuildingType t, float w, float d, uint seed)
        {
            var l = new List<Vector3>();
            uint s = seed | 1u;
            for (int i = 0; i < 3; i++) Next(ref s);
            switch (t)
            {
                case BuildingType.House:
                {
                    int style = (int)(Next(ref s) % 4);
                    bool flip = R(ref s) < 0.5f;
                    if (style == 1)
                    {
                        // the ranch: across the front of its garage (a second car is inside)
                        float x0 = -(6.0f + 3.3f) / 2, gx0 = flip ? x0 : x0 + 6.0f;
                        l.Add(new Vector3(gx0 + 1.65f + (flip ? 0.35f : -0.35f), -d / 2 + 1.3f, flip ? -90 : 90));
                    }
                    else
                    {
                        // the others: two in a line up the driveway beside the house
                        float x = flip ? -w / 2 + 1.1f : w / 2 - 1.1f;
                        l.Add(new Vector3(x, -d / 2 + 2.45f, 180)); l.Add(new Vector3(x, -d / 2 + 7.15f, 180));
                    }
                    break;
                }
                case BuildingType.CarDealer:
                    for (float x = -w / 2 + 2.6f; x < w / 2 - 2; x += 3.2f) l.Add(new Vector3(x, -d / 2 + 3.4f, 180));
                    for (float x = -w / 2 + 2.6f; x < w / 2 - 2; x += 3.2f) l.Add(new Vector3(x, -d / 2 + 8.0f, 0));
                    break;
                case BuildingType.AutoRepair:
                    l.Add(new Vector3(-2.65f, -d / 2 + 1.05f, 90)); l.Add(new Vector3(1.55f, -d / 2 + 1.05f, 90));
                    break;
                case BuildingType.ParkingLot:
                    // a row along the front with the way in through the middle, and a row along the back
                    for (int i = 0; i < 7; i++) if (i != 3) l.Add(new Vector3(-7.8f + i * 2.6f, LotFrontZ(d), 180));
                    for (int i = 0; i < 7; i++) l.Add(new Vector3(-7.8f + i * 2.6f, LotBackZ(d), 0));
                    break;
                case BuildingType.GroceryStore:
                    // the front row of its car park (Parking below)
                    for (float x = -w / 2 + 0.9f; x < w / 2 - 3.0f; x += 2.6f) l.Add(new Vector3(x + 1.3f, -d / 2 + 2.22f, 0));
                    break;
            }
            return l;
        }

        /// <summary>The model of a building. Closed businesses go dark.</summary>
        public static Model Build(BuildingType t, float w, float d, uint seed, int floors, bool closed, int season)
        {
            var k = new MeshKit();
            uint s = seed | 1u;
            for (int i = 0; i < 3; i++) Next(ref s);
            switch (t)
            {
                case BuildingType.House: House(k, w, d, ref s, season); break;
                case BuildingType.Apartments: Apartments(k, w, d, ref s, season); break;
                case BuildingType.GroceryStore: Grocery(k, w, d, ref s, closed, season); break;
                case BuildingType.Restaurant: Shop(k, w, d, ref s, closed, season, 1, "gf_store_dark", SignRow.Grill, H("#8e2b23"), true); break;
                case BuildingType.Cafe: Shop(k, w, d, ref s, closed, season, 2, "gf_store_wood", SignRow.Coffee, H("#2e6b5a"), true); break;
                case BuildingType.Bar: Shop(k, w, d, ref s, closed, season, 2, "gf_store_dark", SignRow.Bar, null, false); break;
                case BuildingType.ClothingStore: Shop(k, w, d, ref s, closed, season, 2, "gf_store_light", SignRow.Apparel, H("#3d3d6b"), false); break;
                case BuildingType.ElectronicsStore: Shop(k, w, d, ref s, closed, season, 2, "gf_store_dark", SignRow.Electronics, null, false); break;
                case BuildingType.FurnitureStore: Shop(k, w, d, ref s, closed, season, 1, "gf_store_light", SignRow.Furniture, H("#a0522d"), false); break;
                case BuildingType.HairSalon: Shop(k, w, d, ref s, closed, season, 2, "gf_store_light", SignRow.Hair, H("#c76d8f"), false); break;
                case BuildingType.Gym: Shop(k, w, d, ref s, closed, season, 1, "gf_store_dark", SignRow.Fitness, null, false); break;
                case BuildingType.GasStation: Gas(k, w, d, ref s, closed, season); break;
                case BuildingType.CarDealer: CarDealer(k, w, d, ref s, closed, season); break;
                case BuildingType.AutoRepair: AutoRepair(k, w, d, ref s, closed, season); break;
                case BuildingType.Office: Office(k, w, d, ref s, floors, closed, season); break;
                case BuildingType.Bank: Bank(k, w, d, ref s, closed, season); break;
                case BuildingType.Farm: Farm(k, w, d, ref s, season); break;
                case BuildingType.FoodPlant: Plant(k, w, d, ref s, closed, season, "fac_metal_beige", SignRow.Foods, true, false); break;
                case BuildingType.GarmentFactory: Plant(k, w, d, ref s, closed, season, "fac_tiltup", SignRow.ApparelCo, false, true); break;
                case BuildingType.ElectronicsPlant: Plant(k, w, d, ref s, closed, season, "fac_metal_blue", SignRow.Tech, false, false); break;
                case BuildingType.FurnitureWorkshop: Workshop(k, w, d, ref s, closed, season); break;
                case BuildingType.Construction: Yard(k, w, d, ref s, closed, season); break;
                case BuildingType.Trucking: Depot(k, w, d, ref s, closed, season); break;
                case BuildingType.TownHall: TownHall(k, w, d, ref s, season); break;
                case BuildingType.Elementary: School(k, w, d, ref s, season, false); break;
                case BuildingType.HighSchool: School(k, w, d, ref s, season, true); break;
                case BuildingType.College: College(k, w, d, ref s, season); break;
                case BuildingType.Clinic: Clinic(k, w, d, ref s, season, false); break;
                case BuildingType.Hospital: Clinic(k, w, d, ref s, season, true); break;
                case BuildingType.Police: Station(k, w, d, ref s, season, false); break;
                case BuildingType.FireStation: Station(k, w, d, ref s, season, true); break;
                case BuildingType.Park: Park(k, w, d, ref s, season); break;
                case BuildingType.BusDepot: BusDepot(k, w, d, ref s, season); break;
                case BuildingType.ParkingLot: ParkingLot(k, w, d, ref s, season); break;
                case BuildingType.FreightTerminal: Terminal(k, w, d, ref s, season); break;
                case BuildingType.Realty: Shop(k, w, d, ref s, closed, season, 2, "gf_store_light", SignRow.RealEstate, H("#1d3557"), false); break;
            }
            return k.ToModel(t.ToString());
        }

        // ================================================================== houses
        static readonly Color[] SidingTints =
        {
            new Color(1f, 1f, 1f), new Color(0.98f, 0.93f, 0.82f), new Color(0.78f, 0.86f, 0.93f), new Color(0.8f, 0.86f, 0.76f),
            new Color(0.8f, 0.81f, 0.82f), new Color(0.98f, 0.9f, 0.66f), new Color(0.42f, 0.5f, 0.62f), new Color(0.7f, 0.36f, 0.32f),
            new Color(0.84f, 0.78f, 0.7f), new Color(0.48f, 0.5f, 0.52f), new Color(0.62f, 0.72f, 0.66f),
        };
        static readonly Color[] ShingleTints =
        {
            new Color(0.55f, 0.55f, 0.57f), new Color(0.66f, 0.52f, 0.43f), new Color(0.5f, 0.56f, 0.64f), new Color(0.4f, 0.4f, 0.42f),
            new Color(0.78f, 0.74f, 0.68f), new Color(0.45f, 0.55f, 0.48f), new Color(0.68f, 0.4f, 0.34f),
        };
        static readonly Color32[] Shutters = { H("#1e2a3a"), H("#22362a"), H("#5a1e22"), H("#2b2b2b"), H("#ffffff"), H("#3d5a73") };

        /// <summary>A house window from the 4 x 4 window atlas, with a sill and maybe shutters.</summary>
        static void HouseWindow(MeshKit k, Face f, float along, float y, float ww, float wh, ref uint s, Material shutter)
        {
            int cell = (int)(Next(ref s) % 16), cu = cell % 4, cv = cell / 4;
            Rect(k, f, along - ww / 2, along + ww / 2, y, y + wh, S("win_house", 1.4f), new Vector2(cu / 4f, cv / 4f), new Vector2((cu + 1) / 4f, (cv + 1) / 4f), 0.04f);
            var trim = C(Trim, 0.35f);
            Ledge(k, f, along - ww / 2 - 0.08f, along + ww / 2 + 0.08f, y - 0.07f, y + 0.02f, 0.12f, trim);
            if (shutter != null)
            {
                Ledge(k, f, along - ww / 2 - 0.42f, along - ww / 2 - 0.06f, y, y + wh, 0.05f, shutter);
                Ledge(k, f, along + ww / 2 + 0.06f, along + ww / 2 + 0.42f, y, y + wh, 0.05f, shutter);
            }
        }

        /// <summary>A front door from the door atlas, with a step and a porch light.</summary>
        static void FrontDoor(MeshKit k, Face f, float along, ref uint s, float y = 0.18f)
        {
            int cell = (int)(Next(ref s) % 4);
            Rect(k, f, along - 0.55f, along + 0.55f, y, y + 2.3f, S("door_house", 1.2f), new Vector2(cell / 4f, 0), new Vector2((cell + 1) / 4f, 1), 0.04f);
            Ledge(k, f, along - 0.9f, along + 0.9f, 0, y, 1.0f, S("concrete"), null, 2f);
            var glow = MaterialLibrary.Glow(new Color(0.9f, 0.85f, 0.7f), new Color(1f, 0.8f, 0.5f), 3f);
            Ledge(k, f, along + 0.75f, along + 0.92f, y + 1.7f, y + 2.0f, 0.12f, glow);
        }

        static void HouseWalls(MeshKit k, float x0, float x1, float z0, float z1, float h, Material wall, float tile = 2f)
        {
            var faces = Faces(x0, x1, z0, z1);
            foreach (var f in faces) Plain(k, f, 0.45f, h, wall, tile);
            // a concrete foundation, set out a touch
            var found = S("concrete");
            k.Box(new Vector3(x0 - 0.04f, 0, z0 - 0.04f), new Vector3(x1 + 0.04f, 0.45f, z1 + 0.04f), found, found, 2f);
            // corner boards
            var trim = C(Trim, 0.35f);
            foreach (var p in new[] { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x0, z1), new Vector2(x1, z1) })
                k.Box(new Vector3(p.x - 0.08f, 0.45f, p.y - 0.08f), new Vector3(p.x + 0.08f, h, p.y + 0.08f), trim, trim);
        }

        static void House(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 8f);
            int style = (int)(Next(ref s) % 4);
            bool flip = R(ref s) < 0.5f;
            var sidingTint = Pick(ref s, SidingTints);
            var shTint = Pick(ref s, ShingleTints);
            var shutter = R(ref s) < 0.55f ? C(Pick(ref s, Shutters), 0.35f) : null;
            switch (style)
            {
                case 0: Colonial(k, w, d, ref s, season, sidingTint, shTint, shutter, flip); break;
                case 1: Ranch(k, w, d, ref s, season, sidingTint, shTint, flip); break;
                case 2: CapeCod(k, w, d, ref s, season, sidingTint, shTint, shutter, flip); break;
                default: Farmhouse(k, w, d, ref s, season, flip); break;
            }
            // the yard: a tree out back, a fence, a mailbox by the kerb
            Tree(k, flip ? w / 2 - 1.6f : -w / 2 + 1.6f, d / 2 - 1.6f, Range(ref s, 0.8f, 1.1f), season, s);
            var fence = T("wood", new Color(1f, 1f, 0.98f));
            k.Box(new Vector3(-w / 2 + 0.15f, 0.06f, d / 2 - 0.25f), new Vector3(w / 2 - 0.15f, 1.15f, d / 2 - 0.15f), fence, C(White), 2f);
            float mx = flip ? w / 2 - 0.7f : -w / 2 + 0.7f;
            k.Box(new Vector3(mx - 0.04f, 0.06f, -d / 2 + 0.5f), new Vector3(mx + 0.04f, 1.0f, -d / 2 + 0.58f), C(Dark), C(Dark));
            k.Box(new Vector3(mx - 0.12f, 1.0f, -d / 2 + 0.36f), new Vector3(mx + 0.12f, 1.22f, -d / 2 + 0.74f), C(Dark, 0.5f, 0.4f), C(Dark, 0.5f, 0.4f));
        }

        static void Bushes(MeshKit k, Face f, float a0, float a1, int season)
        {
            for (float a = a0; a <= a1 + 0.01f; a += 1.1f) PropArt.Bush(k, f.At(a, 0.06f) + f.Normal * 0.55f, 0.75f, season);
        }

        /// <summary>Two-storey colonial: symmetrical windows, shutters, a portico over the door, a chimney.</summary>
        static void Colonial(MeshKit k, float w, float d, ref uint s, int season, Color siding, Color shingle, Material shutter, bool flip)
        {
            float bw = 7.4f, bd = 6f, h = 5.7f;
            float x0 = -bw / 2 + (flip ? 0.9f : -0.9f), x1 = x0 + bw, z0 = -d / 2 + 2.8f, z1 = z0 + bd;
            var wall = T("wall_siding", siding);
            HouseWalls(k, x0, x1, z0, z1, h, wall);
            k.Gable(x0, z0, x1, z1, h, 2.4f, RoofMat("roof_shingle", shingle, season), wall, 0.4f);
            var faces = Faces(x0, x1, z0, z1);
            var f = faces[South];
            FrontDoor(k, f, bw / 2, ref s);
            HouseWindow(k, f, bw * 0.18f, 0.95f, 1.0f, 1.35f, ref s, shutter);
            HouseWindow(k, f, bw * 0.82f, 0.95f, 1.0f, 1.35f, ref s, shutter);
            for (int i = 0; i < 3; i++) HouseWindow(k, f, bw * (0.18f + 0.32f * i), 3.75f, 0.95f, 1.25f, ref s, shutter);
            foreach (int side in new[] { East, West })
                for (int fl = 0; fl < 2; fl++) HouseWindow(k, faces[side], bd / 2, fl == 0 ? 0.95f : 3.75f, 0.95f, 1.25f, ref s, null);
            for (int i = 0; i < 2; i++) for (int fl = 0; fl < 2; fl++) HouseWindow(k, faces[North], bw * (0.28f + 0.44f * i), fl == 0 ? 0.95f : 3.75f, 0.95f, 1.25f, ref s, null);
            // portico: a small roof on two columns
            var white = C(Trim, 0.35f);
            Ledge(k, f, bw / 2 - 1.2f, bw / 2 + 1.2f, 2.7f, 2.92f, 1.3f, white, RoofMat("roof_shingle", shingle, season));
            foreach (float a in new[] { bw / 2 - 1.05f, bw / 2 + 1.05f })
                k.Cylinder(f.At(a, 0.18f) + f.Normal * 1.15f, 0.1f, 0.1f, 2.52f, 8, white, white);
            Bushes(k, f, 0.6f, bw / 2 - 1.6f, season);
            Bushes(k, f, bw / 2 + 1.6f, bw - 0.6f, season);
            // a brick chimney up the side
            var brick = S("wall_brick_red");
            float cx = flip ? x1 + 0.35f : x0 - 0.35f;
            k.Box(new Vector3(cx - 0.45f, 0, z0 + bd / 2 - 0.6f), new Vector3(cx + 0.45f, h + 3.1f, z0 + bd / 2 + 0.6f), brick, C(H("#5a5a5a")));
            // driveway to the street on the other side
            float dx = flip ? -w / 2 + 0.2f : w / 2 - 2.4f;
            k.Flat(dx, -d / 2 + 0.05f, dx + 2.2f, d / 2 - 0.45f, 0.08f, S("concrete"), 4f);
            if (R(ref s) < 0.6f) R(ref s);   // (the family's cars are parked by the view: ParkingSpots)
            k.Flat(x0 + bw / 2 - 0.6f, -d / 2 + 0.05f, x0 + bw / 2 + 0.6f, z0 - 0.9f, 0.08f, S("pavers"), 3f);
        }

        /// <summary>Single-storey ranch with an attached garage and a low hip roof.</summary>
        static void Ranch(MeshKit k, float w, float d, ref uint s, int season, Color siding, Color shingle, bool flip)
        {
            float hw = 6.0f, gw = 3.3f, bd = 6.2f, h = 2.9f;
            float total = hw + gw;
            float x0 = -total / 2, z0 = -d / 2 + 2.6f, z1 = z0 + bd;
            float gx0 = flip ? x0 : x0 + hw, gx1 = gx0 + gw;
            bool brick = R(ref s) < 0.45f;
            var wall = brick ? S(R(ref s) < 0.5f ? "wall_brick_red" : "wall_brick_tan") : T("wall_siding", siding);
            HouseWalls(k, x0, x0 + total, z0, z1, h, wall);
            k.Hip(x0, z0, x0 + total, z1, h, 1.7f, RoofMat("roof_shingle", shingle, season), 0.5f);
            var faces = Faces(x0, x0 + total, z0, z1);
            var f = faces[South];
            float house0 = flip ? gw : 0;
            float doorA = house0 + hw * 0.62f;
            FrontDoor(k, f, doorA, ref s);
            HouseWindow(k, f, house0 + hw * 0.22f, 0.95f, 1.6f, 1.3f, ref s, null);
            HouseWindow(k, f, house0 + hw * 0.88f, 0.95f, 0.9f, 1.3f, ref s, null);
            float ga0 = flip ? 0 : hw, ga1 = ga0 + gw;
            Rect(k, f, ga0 + 0.35f, ga1 - 0.35f, 0.08f, 2.35f, S("garage_door"), new Vector2(0, 0), new Vector2(1, 1), 0.04f);
            Ledge(k, f, ga0 + 0.25f, ga1 - 0.25f, 2.35f, 2.5f, 0.08f, C(Trim));
            // the north face runs the other way: measure from its left (the east end)
            float nh0 = flip ? 0 : gw;
            HouseWindow(k, faces[North], nh0 + hw * 0.3f, 0.95f, 1.0f, 1.3f, ref s, null);
            HouseWindow(k, faces[North], nh0 + hw * 0.75f, 0.95f, 1.0f, 1.3f, ref s, null);
            HouseWindow(k, faces[flip ? East : West], bd / 2, 0.95f, 1.0f, 1.3f, ref s, null);
            Bushes(k, f, house0 + 0.6f, house0 + hw * 0.45f, season);
            // driveway to the garage
            k.Flat(gx0 + 0.2f, -d / 2 + 0.05f, gx1 - 0.2f, z0, 0.08f, S("concrete"), 4f);
            R(ref s);
            k.Flat(x0 + doorA - 0.55f, -d / 2 + 0.05f, x0 + doorA + 0.55f, z0 - 1.0f, 0.08f, S("pavers"), 3f);
        }

        /// <summary>A storey and a half under a steep roof with two dormers.</summary>
        static void CapeCod(MeshKit k, float w, float d, ref uint s, int season, Color siding, Color shingle, Material shutter, bool flip)
        {
            float bw = 7.6f, bd = 6.2f, h = 2.9f, rise = 3.8f;
            float x0 = -bw / 2 + (flip ? 0.9f : -0.9f), x1 = x0 + bw, z0 = -d / 2 + 2.8f, z1 = z0 + bd, mid = (x0 + x1) / 2;
            var wall = T(R(ref s) < 0.5f ? "wall_siding" : "wall_stucco", siding);
            var roof = RoofMat("roof_shingle", shingle, season);
            HouseWalls(k, x0, x1, z0, z1, h, wall);
            k.Gable(x0, z0, x1, z1, h, rise, roof, wall, 0.35f);
            var faces = Faces(x0, x1, z0, z1);
            var f = faces[South];
            FrontDoor(k, f, bw / 2, ref s);
            HouseWindow(k, f, bw * 0.2f, 0.9f, 1.0f, 1.35f, ref s, shutter);
            HouseWindow(k, f, bw * 0.8f, 0.9f, 1.0f, 1.35f, ref s, shutter);
            HouseWindow(k, faces[East], bd / 2, 3.4f, 0.85f, 1.1f, ref s, null);
            HouseWindow(k, faces[West], bd / 2, 3.4f, 0.85f, 1.1f, ref s, null);
            HouseWindow(k, faces[North], bw * 0.3f, 0.9f, 1.0f, 1.35f, ref s, null);
            HouseWindow(k, faces[North], bw * 0.7f, 0.9f, 1.0f, 1.35f, ref s, null);
            // dormers on the front slope: built along their own x (pointing at the street), then turned
            foreach (float cx in new[] { mid - bw * 0.22f, mid + bw * 0.22f })
            {
                float L = 1.9f, dw = 1.6f;
                float zBack = z0 + bd / 2 - 0.3f;
                k.Push(new Vector3(cx, 0, zBack), 90);
                var df = Faces(0, L, -dw / 2, dw / 2);
                float yb = h + 0.7f, yt = h + 2.1f;
                foreach (int side in new[] { North, South, East }) Plain(k, df[side], yb, yt, wall, 2f);
                k.Gable(0, -dw / 2, L, dw / 2, yt, 0.75f, roof, wall, 0.12f);
                HouseWindow(k, df[East], dw / 2, yb + 0.2f, 0.8f, 1.0f, ref s, null);
                k.Pop();
            }
            Bushes(k, f, 0.6f, bw / 2 - 1.2f, season);
            Bushes(k, f, bw / 2 + 1.2f, bw - 0.6f, season);
            float dx = flip ? -w / 2 + 0.2f : w / 2 - 2.3f;
            k.Flat(dx, -d / 2 + 0.05f, dx + 2.1f, d / 2 - 0.45f, 0.08f, S("gravel"), 3f);
            k.Flat(mid - 0.55f, -d / 2 + 0.05f, mid + 0.55f, z0 - 1.0f, 0.08f, S("pavers"), 3f);
        }

        /// <summary>Modern farmhouse: board-and-batten, a dark metal roof, a porch across the front.</summary>
        static void Farmhouse(MeshKit k, float w, float d, ref uint s, int season, bool flip)
        {
            float bw = 7.8f, bd = 6.0f, h = 5.4f;
            float x0 = -bw / 2 + (flip ? 1.0f : -1.0f), x1 = x0 + bw, z0 = -d / 2 + 3.2f, z1 = z0 + bd;
            bool darkHouse = R(ref s) < 0.35f;
            var wall = T("wall_board", darkHouse ? new Color(0.32f, 0.34f, 0.36f) : new Color(0.98f, 0.98f, 0.97f));
            var roof = RoofMat("roof_metal", darkHouse ? new Color(0.75f, 0.75f, 0.75f) : new Color(0.25f, 0.26f, 0.28f), season);
            HouseWalls(k, x0, x1, z0, z1, h, wall);
            k.Gable(x0, z0, x1, z1, h, 3.0f, roof, wall, 0.45f);
            var faces = Faces(x0, x1, z0, z1);
            var f = faces[South];
            FrontDoor(k, f, bw * 0.5f, ref s, 0.45f);
            HouseWindow(k, f, bw * 0.2f, 1.0f, 1.1f, 1.6f, ref s, null);
            HouseWindow(k, f, bw * 0.8f, 1.0f, 1.1f, 1.6f, ref s, null);
            HouseWindow(k, f, bw * 0.3f, 3.5f, 1.0f, 1.3f, ref s, null);
            HouseWindow(k, f, bw * 0.7f, 3.5f, 1.0f, 1.3f, ref s, null);
            foreach (int side in new[] { East, West }) HouseWindow(k, faces[side], bd / 2, 3.5f, 1.0f, 1.3f, ref s, null);
            HouseWindow(k, faces[North], bw * 0.5f, 1.0f, 1.4f, 1.5f, ref s, null);
            // the porch: a deck, posts and a shed roof
            var deck = T("wood", new Color(0.8f, 0.7f, 0.58f));
            var post = C(darkHouse ? Trim : H("#2b2d30"), 0.3f);
            k.Box(new Vector3(x0 + 0.4f, 0, z0 - 2.0f), new Vector3(x1 - 0.4f, 0.45f, z0), deck, deck, 2f);
            for (int i = 0; i < 4; i++)
            {
                float px = Mathf.Lerp(x0 + 0.6f, x1 - 0.6f, i / 3f);
                k.Box(new Vector3(px - 0.08f, 0.45f, z0 - 1.9f), new Vector3(px + 0.08f, 2.85f, z0 - 1.74f), post, post);
            }
            k.Quad(new Vector3(x0 + 0.2f, 2.85f, z0 - 2.2f), new Vector3(x0 + 0.2f, 3.2f, z0), new Vector3(x1 - 0.2f, 3.2f, z0), new Vector3(x1 - 0.2f, 2.85f, z0 - 2.2f), roof, 2f);
            k.Box(new Vector3(x0 + 0.2f, 2.7f, z0 - 2.2f), new Vector3(x1 - 0.2f, 2.85f, z0 - 2.05f), post, post);
            float dx = flip ? -w / 2 + 0.2f : w / 2 - 2.3f;
            k.Flat(dx, -d / 2 + 0.05f, dx + 2.1f, d / 2 - 0.45f, 0.08f, S("gravel"), 3f);
            R(ref s);
        }

        // ================================================================== apartments
        static readonly string[] AptUpper = { "fac_apt_brick", "fac_apt_tan", "fac_apt_cream", "fac_apt_grey" };

        static string WallFor(string upper)
        {
            return upper == "fac_apt_brick" ? "wall_brick_red" : upper == "fac_apt_tan" ? "wall_brick_tan" : "wall_stucco";
        }

        static void Apartments(MeshKit k, float w, float d, ref uint s, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 8f);
            string upper = Pick(ref s, AptUpper);
            var skin = new Skin { Upper = upper, Ground = "gf_apt_base", GroundH = 3.8f, Wall = WallFor(upper) };
            float x0 = -w / 2 + 2f, x1 = w / 2 - 2f, z0 = -d / 2 + 4.5f, z1 = d / 2 - 2f;
            int floors = 3 + (int)(Next(ref s) % 3);
            int u = (int)(Next(ref s) % 4), v = (int)(Next(ref s) % 4);
            float top = Block(k, x0, x1, z0, z1, skin, floors, u, v, true, true, "roof_membrane", season);
            var faces = Faces(x0, x1, z0, z1);
            var concrete = S("concrete");
            var rail = C(H("#3b3e42"), 0.4f, 0.5f);
            var glass = MaterialLibrary.Pane(new Color32(20, 22, 30, 255));
            // balconies in front of the balcony doors (atlas columns 1 and 3) on the front and back
            foreach (int side in new[] { South, North })
            {
                var f = faces[side];
                int bays = Bays(f, skin.Bay);
                float bw = f.Width / bays;
                for (int i = 0; i < bays; i++)
                {
                    int col = (u + i) % 4;
                    if (col != 1 && col != 3) continue;
                    float c = (i + 0.5f) * bw;
                    for (int fl = 0; fl < floors; fl++)
                    {
                        float y = skin.GroundH + fl * skin.Floor;
                        Balcony(k, f, c, y, bw * 0.82f, concrete, rail, glass);
                    }
                }
            }
            // the entrance: a canopy over the door bay (ground atlas column 2) with a light under it
            var front = faces[South];
            int gb = Bays(front, skin.GroundBay);
            float gbw = front.Width / gb;
            for (int i = 0; i < gb; i++)
            {
                if ((u + i) % 4 != 2) continue;
                float c = (i + 0.5f) * gbw;
                Ledge(k, front, c - 1.6f, c + 1.6f, 3.05f, 3.25f, 1.8f, C(H("#4a4f55"), 0.4f, 0.3f));
                var glow = MaterialLibrary.Glow(new Color(0.9f, 0.88f, 0.8f), new Color(1f, 0.85f, 0.6f), 2.5f);
                Ledge(k, front, c - 0.4f, c + 0.4f, 3.0f, 3.05f, 1.6f, glow);
                k.Flat(x0 + c - 1.0f, -d / 2 + 0.1f, x0 + c + 1.0f, z0, 0.08f, S("pavers"), 3f);
                break;
            }
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 1.2f, S(skin.Wall));
            // front garden: trees, a hedge, a lamp
            Tree(k, -w / 2 + 1.2f, -d / 2 + 1.5f, 0.95f, season, s);
            Tree(k, w / 2 - 1.2f, -d / 2 + 1.5f, 0.95f, season, s + 7);
            for (float x = -w / 2 + 3f; x < w / 2 - 3f; x += 1.1f) if (Mathf.Abs(x) > 2.2f) PropArt.Bush(k, new Vector3(x, 0.06f, z0 - 0.7f), 0.7f, season);
            PropArt.Lamp(k, new Vector3(-3.4f, 0.06f, -d / 2 + 0.6f));
        }

        static void Balcony(MeshKit k, Face f, float c, float y, float bw, Material slab, Material rail, Material glass)
        {
            const float depth = 1.25f;
            Ledge(k, f, c - bw / 2, c + bw / 2, y - 0.18f, y + 0.02f, depth, slab, slab);
            var n = f.Normal;
            Vector3 a = f.At(c - bw / 2, y + 0.02f) + n * depth, b = f.At(c + bw / 2, y + 0.02f) + n * depth;
            Vector3 up = Vector3.up * 1.0f;
            // glass front and sides, both faces (it's seen from above)
            k.Quad(a, a + up, b + up, b, glass); k.Quad(b, b + up, a + up, a, glass);
            Vector3 ai = f.At(c - bw / 2, y + 0.02f), bi = f.At(c + bw / 2, y + 0.02f);
            k.Quad(ai, ai + up, a + up, a, glass); k.Quad(a, a + up, ai + up, ai, glass);
            k.Quad(b, b + up, bi + up, bi, glass); k.Quad(bi, bi + up, b + up, b, glass);
            Vector3 r0 = a + up, r1 = b + up;
            k.Box(Vector3.Min(r0, r1) - new Vector3(0.04f, 0, 0.04f), Vector3.Max(r0, r1) + new Vector3(0.04f, 0.06f, 0.04f), rail, rail);
        }

        // ================================================================== shops
        static readonly string[] ShopUpper = { "fac_apt_brick", "fac_apt_cream", "fac_apt_tan", "fac_apt_grey" };

        static void Shop(MeshKit k, float w, float d, ref uint s, bool closed, int season, int storeys, string ground, int signRow, Color32? awning, bool tables)
        {
            Lot(k, w, d, S("sidewalk"), 0.02f, 6f);
            float x0 = -w / 2 + 0.4f, x1 = w / 2 - 0.4f, z0 = -d / 2 + 2.4f, z1 = d / 2 - 0.4f;
            string upper = Pick(ref s, ShopUpper);
            var skin = new Skin { Upper = upper, Ground = ground, GroundH = 4.2f, Wall = WallFor(upper) };
            int u = (int)(Next(ref s) % 4), v = (int)(Next(ref s) % 4);
            var faces = Faces(x0, x1, z0, z1);
            var front = faces[South];
            float top;
            if (storeys >= 2) top = Block(k, x0, x1, z0, z1, skin, storeys - 1, u, v, false, true, "roof_membrane", season, closed);
            else
            {
                // one storey: the shopfront, a parapet band for the sign, blank walls round the rest
                var wallMat = S(skin.Wall);
                var groundMat = closed ? MaterialLibrary.Dark(ground, new Color(0.7f, 0.7f, 0.7f)) : S(ground, 1.8f);
                top = 5.4f;
                Band(k, front, 0, 4.2f, groundMat, 2.7f, 4.2f, 1, u, 0);
                Plain(k, front, 4.2f, top, wallMat, 2f);
                for (int i = 0; i < 4; i++) if (i != South) Plain(k, faces[i], 0, top, wallMat, 2f);
                FlatRoof(k, x0, x1, z0, z1, top, season == 0 ? S("snow") : S("roof_membrane"), wallMat, C(skin.Coping, 0.3f));
            }
            // sign board over the shopfront
            float signW = Mathf.Min(front.Width - 1.2f, 7.5f);
            Sign(k, front, front.Width / 2, signW, storeys >= 2 ? 4.35f : 4.45f, signW / 11f, signRow, closed);
            if (closed)
            {
                var boards = T("wood", new Color(0.75f, 0.65f, 0.52f));
                for (float a = 0.6f; a < front.Width - 1f; a += 2.7f) Ledge(k, front, a, a + 2.0f, 0.6f, 3.6f, 0.06f, boards);
            }
            else if (awning.HasValue)
            {
                Awning(k, front, 0.3f, front.Width - 0.3f, 3.85f, 1.5f, awning.Value);
                if (tables)
                    for (float a = 1.6f; a < front.Width - 1f; a += 2.8f)
                        CafeTable(k, front.At(a, 0.06f) + front.Normal * 1.4f, awning.Value);
            }
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 0.8f, S(skin.Wall));
            PropArt.Lamp(k, new Vector3(w / 2 - 0.8f, 0.06f, -d / 2 + 0.5f));
            if (w > 12) Tree(k, -w / 2 + 1.2f, -d / 2 + 1.0f, 0.75f, season, s);
        }

        /// <summary>A striped canvas awning on a face.</summary>
        static void Awning(MeshKit k, Face f, float a0, float a1, float y, float depth, Color32 col)
        {
            int n = Mathf.Max(2, Mathf.RoundToInt((a1 - a0) / 0.9f));
            float sw = (a1 - a0) / n;
            var ca = C(col, 0.1f); var cb = C(H("#f2efe6"), 0.1f);
            for (int i = 0; i < n; i++)
            {
                var m = i % 2 == 0 ? ca : cb;
                float xa = a0 + i * sw, xb = xa + sw;
                Vector3 p0 = f.At(xa, y), p1 = f.At(xb, y), q0 = f.At(xa, y - 0.9f) + f.Normal * depth, q1 = f.At(xb, y - 0.9f) + f.Normal * depth;
                k.Quad(q0, p0, p1, q1, m); k.Quad(q1, p1, p0, q0, m);
                Vector3 d0 = q0 + Vector3.down * 0.3f, d1 = q1 + Vector3.down * 0.3f;
                k.Quad(d0, q0, q1, d1, m); k.Quad(d1, q1, q0, d0, m);
            }
        }

        static void CafeTable(MeshKit k, Vector3 p, Color32 col)
        {
            var white = C(H("#f4f4f2")); var dark = C(Dark, 0.4f, 0.4f);
            k.Cylinder(p, 0.45f, 0.45f, 0.06f, 10, white, white);
            k.Cylinder(p + Vector3.up * 0.7f, 0.42f, 0.42f, 0.04f, 10, white, white);
            k.Box(p + new Vector3(-0.03f, 0, -0.03f), p + new Vector3(0.03f, 2.1f, 0.03f), dark, dark);
            k.Cylinder(p + Vector3.up * 1.9f, 1.3f, 0.05f, 0.5f, 8, C(col, 0.1f), C(col, 0.1f));
            foreach (var o in new[] { new Vector3(0.75f, 0, 0), new Vector3(-0.75f, 0, 0) })
                k.Box(p + o + new Vector3(-0.2f, 0, -0.2f), p + o + new Vector3(0.2f, 0.45f, 0.2f), dark, dark);
        }

        static void Grocery(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = 0f, z1 = d / 2 - 0.8f, h = 7f;
            var faces = Faces(x0, x1, z0, z1);
            var wall = T("wall_stucco", new Color(0.93f, 0.89f, 0.8f));
            var side = T("metal_panel", new Color(0.86f, 0.84f, 0.78f));
            var green = C(H("#2e7d4f"), 0.35f);
            var front = faces[South];
            // the shopfront in the middle, solid walls either side
            float g0 = front.Width * 0.18f, g1 = front.Width * 0.82f;
            var store = closed ? MaterialLibrary.Dark("gf_store_light", new Color(0.7f, 0.7f, 0.7f)) : S("gf_store_light", 1.8f);
            Rect(k, front, g0, g1, 0, 4.2f, store, new Vector2(0, 0), new Vector2(Mathf.Round((g1 - g0) / 2.7f) * 0.25f, 1));
            k.Quad(front.At(0, 0), front.At(0, h), front.At(g0, h), front.At(g0, 0), wall, 3f);
            k.Quad(front.At(g1, 0), front.At(g1, h), front.At(front.Width, h), front.At(front.Width, 0), wall, 3f);
            k.Quad(front.At(g0, 4.2f), front.At(g0, h), front.At(g1, h), front.At(g1, 4.2f), wall, 3f);
            for (int i = 0; i < 4; i++) if (i != South) Plain(k, faces[i], 0, h, side, 2f);
            FlatRoof(k, x0, x1, z0, z1, h, season == 0 ? S("snow") : S("roof_membrane"), wall, green, 0.9f);
            Ledge(k, front, -0.1f, front.Width + 0.1f, h - 0.4f, h + 0.9f, 0.2f, green);
            Sign(k, front, front.Width / 2, 9f, 5.0f, 0.82f, SignRow.Market, closed);
            Ledge(k, front, front.Width / 2 - 4f, front.Width / 2 + 4f, 3.9f, 4.15f, 2.4f, green, C(H("#e9e9e4")));
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x1 - 0.5f, z0 + 0.5f, z1 - 0.5f, h, ref rs, 2f, wall);
            Parking(k, -w / 2 + 0.5f, -d / 2 + 0.5f, w / 2 - 0.5f, z0 - 3.2f, ref s, closed ? 0.05f : 0.55f);
            var steel = C(Metal, 0.5f, 0.7f);
            k.Box(new Vector3(x1 - 3.4f, 0.06f, z0 - 1.6f), new Vector3(x1 - 1.0f, 1.0f, z0 - 0.6f), steel, steel);
            Tree(k, -w / 2 + 1.2f, -d / 2 + 1.2f, 0.85f, season, s);
            Tree(k, w / 2 - 1.2f, -d / 2 + 1.2f, 0.85f, season, s + 3);
        }

        /// <summary>A parking lot: asphalt with painted bays and some cars. (Its front row is left for the view to park real
        /// cars in: ParkingSpots.)</summary>
        static void Parking(MeshKit k, float x0, float z0, float x1, float z1, ref uint s, float fill)
        {
            k.Flat(x0, z0, x1, z1, 0.08f, S("asphalt"), 8f);
            var paint = C(H("#f2f2ee"), 0.3f);
            float depth = z1 - z0;
            float rowD = Mathf.Min(5.2f, depth / 2);
            for (int row = 0; row < 2 && rowD * (row + 1) <= depth + 0.01f; row++)
            {
                float rz0 = row == 0 ? z0 + 0.3f : z1 - rowD, rz1 = rz0 + rowD - 0.3f;
                for (float x = x0 + 0.4f; x < x1 - 2.5f; x += 2.6f)
                {
                    k.Flat(x, rz0, x + 0.12f, rz1, 0.09f, paint);
                    if (R(ref s) < fill && row == 1) PropArt.Parked(k, new Vector3(x + 1.3f, 0.09f, (rz0 + rz1) / 2), 180, s);   // the front row: shoppers' cars (the view)
                }
            }
        }

        static void Gas(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            float cz = -d / 2 + 3.8f;
            var red = closed ? C(H("#7f8c8d")) : C(H("#c0392b"), 0.4f);
            var white = C(H("#f4f4f2"), 0.3f);
            // canopy over the pumps, lit from below
            float cx0 = -w / 2 + 1.2f, cx1 = w / 2 - 8.2f;
            k.Box(new Vector3(cx0, 4.6f, cz - 2.8f), new Vector3(cx1, 5.4f, cz + 2.8f), red, season == 0 ? S("snow") : white);
            var underside = closed ? C(H("#cfcfcf")) : MaterialLibrary.Glow(new Color(0.92f, 0.92f, 0.9f), new Color(1f, 0.97f, 0.9f), 2.2f);
            k.Quad(new Vector3(cx0 + 0.1f, 4.59f, cz + 2.7f), new Vector3(cx0 + 0.1f, 4.59f, cz - 2.7f), new Vector3(cx1 - 0.1f, 4.59f, cz - 2.7f), new Vector3(cx1 - 0.1f, 4.59f, cz + 2.7f), underside);
            var canopy = Faces(cx0, cx1, cz - 2.8f, cz + 2.8f);
            Sign(k, canopy[South], canopy[South].Width / 2, 5f, 4.68f, 0.62f, SignRow.Fuel, closed);
            for (float x = cx0 + 2f; x < cx1 - 1f; x += 4.4f)
            {
                k.Box(new Vector3(x - 0.18f, 0.06f, cz - 0.18f), new Vector3(x + 0.18f, 4.6f, cz + 0.18f), white, white);
                k.Box(new Vector3(x - 1.2f, 0.06f, cz - 0.6f), new Vector3(x + 1.2f, 0.25f, cz + 0.6f), S("concrete"), S("concrete"));
                k.Box(new Vector3(x - 0.45f, 0.25f, cz + 0.25f), new Vector3(x + 0.45f, 1.8f, cz + 0.6f), white, red);
                k.Quad(new Vector3(x - 0.3f, 1.2f, cz + 0.24f), new Vector3(x - 0.3f, 1.6f, cz + 0.24f), new Vector3(x + 0.3f, 1.6f, cz + 0.24f), new Vector3(x + 0.3f, 1.2f, cz + 0.24f), new Color32(120, 220, 160, 255), true);
                if (!closed && R(ref s) < 0.5f) PropArt.Parked(k, new Vector3(x, 0.06f, cz - 1.9f), 90, s);
            }
            // the shop
            float sx0 = w / 2 - 7f, sx1 = w / 2 - 0.5f, sz0 = -d / 2 + 2.5f, sz1 = d / 2 - 0.5f;
            var sf = Faces(sx0, sx1, sz0, sz1);
            var store = closed ? MaterialLibrary.Dark("gf_store_light", new Color(0.7f, 0.7f, 0.7f)) : S("gf_store_light", 1.8f);
            Band(k, sf[South], 0, 3.6f, store, 2.7f, 4.2f, 1, 1, 0);
            Plain(k, sf[South], 3.6f, 4.4f, white, 2f);
            for (int i = 0; i < 4; i++) if (i != South) Plain(k, sf[i], 0, 4.4f, T("wall_stucco", new Color(0.95f, 0.94f, 0.92f)), 2f);
            FlatRoof(k, sx0, sx1, sz0, sz1, 4.4f, season == 0 ? S("snow") : S("roof_membrane"), white, red, 0.4f);
            Ledge(k, sf[South], -0.05f, sf[South].Width + 0.05f, 3.6f, 4.4f, 0.15f, red);
            // price pylon
            k.Box(new Vector3(-w / 2 + 0.6f, 0.06f, -d / 2 + 0.6f), new Vector3(-w / 2 + 1.0f, 5.2f, -d / 2 + 1.0f), C(Metal, 0.5f, 0.6f), C(Metal));
            k.Box(new Vector3(-w / 2 + 0.2f, 4.0f, -d / 2 + 0.7f), new Vector3(-w / 2 + 1.6f, 6.2f, -d / 2 + 0.9f), red, red);
            k.Quad(new Vector3(-w / 2 + 0.35f, 4.2f, -d / 2 + 0.69f), new Vector3(-w / 2 + 0.35f, 5.4f, -d / 2 + 0.69f), new Vector3(-w / 2 + 1.45f, 5.4f, -d / 2 + 0.69f), new Vector3(-w / 2 + 1.45f, 4.2f, -d / 2 + 0.69f), new Color32(255, 240, 200, 255), true);
        }

        static void CarDealer(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            float x0 = -w / 2 + 1.5f, x1 = w / 2 - 1.5f, z0 = 0.5f, z1 = d / 2 - 1, h = 6.2f;
            var faces = Faces(x0, x1, z0, z1);
            var glass = closed ? MaterialLibrary.Dark("fac_glass_blue", new Color(0.7f, 0.7f, 0.7f)) : S("fac_glass_blue", 1.4f);
            Band(k, faces[South], 0, h, glass, 3.0f, h, 4, 0, 0);
            Band(k, faces[East], 0, h, glass, 3.0f, h, 4, 1, 1);
            Band(k, faces[West], 0, h, glass, 3.0f, h, 4, 2, 2);
            Plain(k, faces[North], 0, h, T("metal_panel", new Color(0.92f, 0.93f, 0.95f)), 2f);
            var trim = C(H("#0a3d62"), 0.4f, 0.3f);
            FlatRoof(k, x0, x1, z0, z1, h, season == 0 ? S("snow") : S("roof_membrane"), C(H("#e9ecef")), trim, 0.8f);
            Ledge(k, faces[South], -0.1f, faces[South].Width + 0.1f, h - 0.2f, h + 0.9f, 0.25f, trim);
            Sign(k, faces[South], faces[South].Width / 2, 7.5f, h + 0.02f, 0.68f, SignRow.AutoSales, closed);
            // bays on the lot: the cars for sale are parked in them by the view (ParkingSpots)
            var line = C(H("#f1f2f6"), 0.2f);
            for (float x = -w / 2 + 2.6f - 1.6f; x < w / 2 - 0.4f; x += 3.2f)
            {
                k.Box(new Vector3(x - 0.06f, 0.06f, -d / 2 + 1.2f), new Vector3(x + 0.06f, 0.075f, -d / 2 + 5.6f), line, line);
                k.Box(new Vector3(x - 0.06f, 0.06f, -d / 2 + 5.8f), new Vector3(x + 0.06f, 0.075f, -d / 2 + 10.2f), line, line);
            }
            for (float x = -w / 2 + 1; x < w / 2; x += 6) Flag(k, x, -d / 2 + 0.8f, 6f, H("#e74c3c"));
        }

        static void Flag(MeshKit k, float x, float z, float h, Color32 col)
        {
            var pole = C(H("#c9ced3"), 0.6f, 0.8f);
            k.Box(new Vector3(x - 0.05f, 0.06f, z - 0.05f), new Vector3(x + 0.05f, h, z + 0.05f), pole, pole);
            var cloth = C(col, 0.1f);
            k.Box(new Vector3(x + 0.05f, h - 1.1f, z - 0.02f), new Vector3(x + 1.7f, h - 0.1f, z + 0.02f), cloth, cloth);
        }

        static void AutoRepair(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            float x0 = -w / 2 + 0.4f, x1 = w / 2 - 0.4f, z0 = -d / 2 + 2.2f, z1 = d / 2 - 0.4f, h = 5.6f;
            var faces = Faces(x0, x1, z0, z1);
            var doors = closed ? MaterialLibrary.Dark("gf_rollup", new Color(0.75f, 0.75f, 0.75f)) : S("gf_rollup", 1.5f);
            Band(k, faces[South], 0, 4.7f, doors, 3.0f, 4.7f, 1, 1, 0);
            var cladding = T("metal_panel", new Color(0.8f, 0.82f, 0.85f));
            Plain(k, faces[South], 4.7f, h, cladding, 2f);
            for (int i = 0; i < 4; i++) if (i != South) Plain(k, faces[i], 0, h, cladding, 2f);
            FlatRoof(k, x0, x1, z0, z1, h, season == 0 ? S("snow") : S("roof_membrane"), C(H("#9aa3ab")), C(H("#f39c12"), 0.3f), 0.5f);
            Sign(k, faces[South], faces[South].Width / 2, Mathf.Min(7f, faces[South].Width - 1f), 4.82f, 0.58f, SignRow.AutoRepair, closed);
            var tire = C(H("#1b1b1b"), 0.25f);
            for (int i = 0; i < 4; i++) k.Cylinder(new Vector3(x1 - 0.5f, 0.06f + i * 0.28f, -d / 2 + 1.0f), 0.38f, 0.38f, 0.26f, 10, tire, tire);
            // cars waiting their turn are parked out front by the view (ParkingSpots)
        }

        // ================================================================== offices and the bank
        static void Office(MeshKit k, float w, float d, ref uint s, int floors, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.02f, 3f);
            string upper = Pick(ref s, new[] { "fac_glass_blue", "fac_glass_green", "fac_precast", "fac_glass_blue" });
            var skin = new Skin { Upper = upper, Ground = "gf_lobby", GroundH = 4.8f, Floor = 3.15f, Wall = "wall_stone", Coping = upper == "fac_precast" ? H("#d6d1c6") : H("#3b4450"), Glow = 1.4f };
            float x0 = -w / 2 + 3, x1 = w / 2 - 3, z0 = -d / 2 + 4.5f, z1 = d / 2 - 2.5f;
            int fl = floors + (int)(Next(ref s) % 4);
            int u = (int)(Next(ref s) % 4), v = (int)(Next(ref s) % 4);
            float top = Block(k, x0, x1, z0, z1, skin, fl, u, v, true, true, "roof_membrane", season, closed);
            var faces = Faces(x0, x1, z0, z1);
            Ledge(k, faces[South], faces[South].Width / 2 - 3.5f, faces[South].Width / 2 + 3.5f, 4.3f, 4.55f, 2.6f, C(H("#3b4450"), 0.5f, 0.6f));
            Sign(k, faces[South], faces[South].Width / 2, 4.2f, 3.65f, 0.38f, SignRow.Offices, closed);
            var mech = T("metal_panel", new Color(0.82f, 0.84f, 0.86f));
            k.Box(new Vector3(x0 + 2.5f, top, z0 + 2), new Vector3(x1 - 2.5f, top + 3f, z1 - 2), mech, season == 0 ? S("snow") : S("roof_membrane"), 2f);
            var dark = C(Dark, 0.4f, 0.5f);
            float cz = (z0 + z1) / 2;
            k.Box(new Vector3(-0.1f, top + 3f, cz - 0.1f), new Vector3(0.1f, top + 9f, cz + 0.1f), dark, dark);
            var red = MaterialLibrary.Glow(new Color(0.6f, 0.1f, 0.1f), new Color(1f, 0.1f, 0.05f), 4f);
            k.Box(new Vector3(-0.15f, top + 9f, cz - 0.15f), new Vector3(0.15f, top + 9.3f, cz + 0.15f), red, red);
            uint rs = s;
            RoofClutter(k, x0 + 0.5f, x0 + 2.3f, z0 + 0.5f, z1 - 0.5f, top, ref rs, 1f, mech);
            var planter = S("concrete");
            foreach (float x in new[] { -w / 2 + 1.5f, w / 2 - 1.5f })
            {
                k.Box(new Vector3(x - 0.9f, 0.02f, -d / 2 + 0.8f), new Vector3(x + 0.9f, 0.6f, -d / 2 + 2.6f), planter, GrassMat(season), 2f);
                Tree(k, x, -d / 2 + 1.7f, 0.85f, season, s);
            }
            PropArt.Lamp(k, new Vector3(-w / 2 + 3.5f, 0.02f, -d / 2 + 1));
            PropArt.Lamp(k, new Vector3(w / 2 - 3.5f, 0.02f, -d / 2 + 1));
        }

        static void Bank(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.02f, 3f);
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 3.2f, z1 = d / 2 - 0.6f, h = 7.6f;
            var stone = S("wall_stone");
            var stoneTop = C(H("#ddd5c3"), 0.25f);
            var faces = Faces(x0, x1, z0, z1);
            k.Box(new Vector3(x0 - 0.5f, 0, z0 - 2.2f), new Vector3(x1 + 0.5f, 0.6f, z1), stone, stoneTop, 2f);
            for (int i = 1; i < 3; i++) k.Box(new Vector3(x0 - 0.5f, 0, z0 - 2.2f - i * 0.35f), new Vector3(x1 + 0.5f, 0.6f - i * 0.2f, z0 - 2.2f), stone, stoneTop, 2f);
            var glass = closed ? MaterialLibrary.Dark("gf_lobby", new Color(0.7f, 0.7f, 0.7f)) : S("gf_lobby", 1.5f);
            var f = faces[South];
            Rect(k, f, 0.3f, f.Width - 0.3f, 0.6f, h - 1.2f, glass, new Vector2(0, 0), new Vector2(Mathf.Round((f.Width - 0.6f) / 2.7f) * 0.25f, 1));
            Plain(k, f, h - 1.2f, h, stone, 2f);
            for (int i = 0; i < 4; i++) if (i != South) Plain(k, faces[i], 0.6f, h, stone, 2f);
            FlatRoof(k, x0, x1, z0, z1, h, season == 0 ? S("snow") : S("roof_membrane"), stone, stoneTop, 0.5f);
            // portico: columns, entablature, pediment
            var white = C(H("#efeae0"), 0.3f);
            int cols = Mathf.Max(4, Mathf.RoundToInt(f.Width / 2.6f));
            for (int i = 0; i < cols; i++)
            {
                float a = Mathf.Lerp(0.7f, f.Width - 0.7f, i / (float)(cols - 1));
                k.Cylinder(f.At(a, 0.6f) + f.Normal * 1.5f, 0.42f, 0.38f, h - 1.7f, 12, white, white);
            }
            Ledge(k, f, -0.4f, f.Width + 0.4f, h - 1.1f, h, 2.0f, stone, stoneTop);
            Sign(k, Out(f, 2.0f), f.Width / 2, 4.5f, h - 0.95f, 0.5f, SignRow.Bank, closed);
            var roofTop = season == 0 ? S("snow") : T("roof_metal", new Color(0.45f, 0.55f, 0.5f));
            Vector3 l = f.At(-0.4f, h) + f.Normal * 2.0f, r = f.At(f.Width + 0.4f, h) + f.Normal * 2.0f, apex = f.At(f.Width / 2, h + 2.2f) + f.Normal * 2.0f;
            k.Tri(l, apex, r, stone);
            Vector3 lb = f.At(-0.4f, h), rb = f.At(f.Width + 0.4f, h), ab = f.At(f.Width / 2, h + 2.2f);
            k.Quad(lb, ab, apex, l, roofTop); k.Quad(r, apex, ab, rb, roofTop);
            Tree(k, -w / 2 + 0.9f, -d / 2 + 0.9f, 0.7f, season, s);
        }
    }
}
