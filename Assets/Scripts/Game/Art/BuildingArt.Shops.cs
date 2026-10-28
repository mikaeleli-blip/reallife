using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>The shops added later, each its own model.</summary>
    public static partial class BuildingArt
    {
        static void ExtraShops(MeshKit k, BuildingType t, float w, float d, ref uint s, bool closed, int season, int floors)
        {
            switch (t)
            {
                case BuildingType.ShoeStore: ShoeStore(k, w, d, ref s, closed, season); break;
                case BuildingType.ApplianceStore: ApplianceStore(k, w, d, ref s, closed, season); break;
                case BuildingType.HardwareStore: HardwareStore(k, w, d, ref s, closed, season); break;
                case BuildingType.Pharmacy: Pharmacy(k, w, d, ref s, closed, season); break;
                case BuildingType.Drugstore: Drugstore(k, w, d, ref s, closed, season); break;
                case BuildingType.Bookstore: Bookstore(k, w, d, ref s, closed, season); break;
                case BuildingType.ToyStore: ToyStore(k, w, d, ref s, closed, season); break;
                case BuildingType.SportsStore: SportsStore(k, w, d, ref s, closed, season); break;
                case BuildingType.Jeweler: Jeweler(k, w, d, ref s, closed, season); break;
                case BuildingType.LiquorStore: LiquorStore(k, w, d, ref s, closed, season); break;
                case BuildingType.PetStore: PetStore(k, w, d, ref s, closed, season); break;
                case BuildingType.GardenCenter: GardenCenter(k, w, d, ref s, closed, season); break;
                case BuildingType.Florist: Florist(k, w, d, ref s, closed, season); break;
                case BuildingType.BikeShop: BikeShop(k, w, d, ref s, closed, season); break;
                case BuildingType.MusicStore: MusicStore(k, w, d, ref s, closed, season); break;
                case BuildingType.StationeryStore: StationeryStore(k, w, d, ref s, closed, season); break;
                case BuildingType.HomeGoodsStore: HomeGoodsStore(k, w, d, ref s, closed, season); break;
                case BuildingType.Boutique: Boutique(k, w, d, ref s, closed, season); break;
                case BuildingType.ThriftStore: ThriftStore(k, w, d, ref s, closed, season); break;
                case BuildingType.DepartmentStore: DepartmentStore(k, w, d, ref s, closed, season); break;
                case BuildingType.PhoneStore: PhoneStore(k, w, d, ref s, closed, season); break;
                case BuildingType.MattressStore: MattressStore(k, w, d, ref s, closed, season); break;
                case BuildingType.ConvenienceStore: ConvenienceStore(k, w, d, ref s, closed, season); break;
                case BuildingType.Butcher: ButcherShop(k, w, d, ref s, closed, season); break;
                case BuildingType.Bakery: BakeryShop(k, w, d, ref s, closed, season); break;
                case BuildingType.FarmersMarket: FarmersMarket(k, w, d, ref s, closed, season); break;
                case BuildingType.FishMarket: FishMarket(k, w, d, ref s, closed, season); break;
                default: ExtraFood(k, t, w, d, ref s, closed, season, floors); break;
            }
        }

        /// <summary>A shop with flats above: a skinned block whose ground floor is the shopfront. Returns the front face and
        /// the roof height.</summary>
        static Face ShopBlock(MeshKit k, float x0, float x1, float z0, float z1, string upper, string ground, int storeys, ref uint s, bool closed, int season, out float top)
        {
            var skin = new Skin { Upper = upper, Ground = ground, GroundH = 4.2f, Wall = WallFor(upper) };
            top = Block(k, x0, x1, z0, z1, skin, storeys - 1, (int)(Next(ref s) % 4), (int)(Next(ref s) % 4), false, true, "roof_membrane", season, closed);
            return Faces(x0, x1, z0, z1)[South];
        }

        static void Sidewalk(MeshKit k, float w, float d) { Lot(k, w, d, S("sidewalk"), 0.02f, 6f); }

        static void ShoeStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.4f, T("wall_stucco", new Color(0.2f, 0.2f, 0.22f)), "gf_store_light", closed, season, Row(BuildingType.ShoeStore), C(H("#f4f4f2")));
            PlainAwning(k, f, 0.4f, f.Width - 0.4f, 3.8f, 1.2f, H("#212121"));
            // a giant sneaker on the roof
            var white = C(H("#f4f4f2"), 0.4f); var red = C(H("#e53935"), 0.4f);
            k.Box(new Vector3(-2.2f, 5.6f, 0), new Vector3(2.2f, 6.1f, 1.6f), white, white);
            k.Box(new Vector3(-2.0f, 6.1f, 0.1f), new Vector3(0.6f, 7.4f, 1.5f), red, red);
            k.Box(new Vector3(0.6f, 6.1f, 0.1f), new Vector3(2.0f, 6.7f, 1.5f), red, white);
            PropArt.Lamp(k, new Vector3(w / 2 - 0.8f, 0.06f, -d / 2 + 0.5f));
        }

        static void ApplianceStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 2.6f, d / 2 - 0.4f, 6f, T("metal_panel", new Color(0.92f, 0.94f, 0.96f)), "gf_store_light", closed, season, Row(BuildingType.ApplianceStore), C(H("#0d47a1")), 0.05f, 0.95f, 10f);
            Ledge(k, f, -0.1f, f.Width + 0.1f, 4.0f, 5.9f, 0.2f, C(H("#0d47a1"), 0.4f));
            Sign(k, f, f.Width / 2, 10f, 4.3f, 1.1f, Row(BuildingType.ApplianceStore), closed);
            // fridges and washers lined up in the windows
            var white = C(H("#f4f4f2"), 0.6f); var dark = C(H("#3a3e44"), 0.4f);
            for (float a = 1.5f; a < f.Width - 1.2f; a += 2.2f)
            {
                var p = f.At(a, 0) + Vector3.forward * 0.6f;
                bool fridge = ((int)a % 2) == 0;
                k.Box(p + new Vector3(-0.4f, 0, 0), p + new Vector3(0.4f, fridge ? 1.9f : 0.95f, 0.7f), white, white);
                if (!fridge) k.Cylinder(p + new Vector3(0, 0.25f, -0.01f), 0.25f, 0.25f, 0.02f, 10, dark, dark);
            }
        }

        static void HardwareStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            // a barn-red false front, a lumber rack and ladders outside
            var red = T("wall_board", new Color(0.66f, 0.18f, 0.16f));
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 5, -d / 2 + 3, d / 2 - 0.4f, 5.2f, red, "gf_store_wood", closed, season, -1, C(H("#f4f4f2")), 0.15f, 0.85f);
            Ledge(k, f, f.Width * 0.25f, f.Width * 0.75f, 5.2f, 6.6f, 0.2f, red, C(H("#f4f4f2")));
            Sign(k, f, f.Width / 2, 6f, 4.4f, 0.75f, Row(BuildingType.HardwareStore), closed);
            Canopy(k, f, 1, f.Width - 1, 3.6f, 1.8f, C(H("#f4f4f2"), 0.3f));
            var lumber = T("wood", new Color(0.95f, 0.84f, 0.64f)); var rack = C(H("#3a3e44"), 0.4f, 0.4f);
            for (int i = 0; i < 3; i++)
            {
                k.Box(new Vector3(w / 2 - 4.5f, 0.06f, -d / 2 + 3 + i * 2.2f), new Vector3(w / 2 - 4.3f, 2.4f, -d / 2 + 3.2f + i * 2.2f), rack, rack);
                k.Box(new Vector3(w / 2 - 4.6f, 0.6f + i * 0.5f, -d / 2 + 2.8f), new Vector3(w / 2 - 0.6f, 0.75f + i * 0.5f, -d / 2 + 8.2f), lumber, lumber, 1f);
            }
            var green = C(H("#2e7d32"), 0.4f);
            for (int i = 0; i < 3; i++) k.Box(new Vector3(-w / 2 + 1.5f + i * 1.3f, 0.06f, -d / 2 + 1.0f), new Vector3(-w / 2 + 2.4f + i * 1.3f, 0.7f, -d / 2 + 2.0f), green, green);
        }

        static void Pharmacy(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.2f, T("wall_stucco", new Color(0.98f, 0.98f, 0.97f)), "gf_store_light", closed, season, Row(BuildingType.Pharmacy), C(H("#2e7d32")));
            Ledge(k, f, -0.1f, f.Width + 0.1f, 0, 0.5f, 0.1f, C(H("#2e7d32"), 0.4f));
            // the green cross on a bracket, lit at night
            var cross = closed ? C(H("#5d6b5d")) : Lit(H("#43d16a"), 2.6f);
            var p = f.At(f.Width - 0.3f, 4.6f) + Vector3.back * 0.9f;
            k.Box(p + new Vector3(-0.08f, -0.7f, -0.25f), p + new Vector3(0.08f, 0.7f, 0.25f), cross, cross);
            k.Box(p + new Vector3(-0.08f, -0.25f, -0.7f), p + new Vector3(0.08f, 0.25f, 0.7f), cross, cross);
            k.Box(f.At(f.Width - 0.3f, 4.55f) + new Vector3(-0.05f, 0, -0.3f), f.At(f.Width - 0.3f, 4.65f) + new Vector3(0.05f, 0, 0), C(H("#3a3e44")), C(H("#3a3e44")));
            Planter(k, -w / 2 + 1, -d / 2 + 1.2f, season);
        }

        static void Drugstore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_grey", "gf_store_light", 2, ref s, closed, season, out top);
            Ledge(k, f, -0.1f, f.Width + 0.1f, 3.5f, 4.2f, 0.25f, C(H("#e53935"), 0.4f));
            Sign(k, f, f.Width / 2, 6.6f, 3.55f, 0.6f, Row(BuildingType.Drugstore), closed);
            Neon(k, f, 0.3f, f.Width - 0.3f, 3.42f, H("#ff5252"), closed, 0.06f);
        }

        static void Bookstore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_brick", "gf_store_wood", 2, ref s, closed, season, out top);
            // a deep green traditional shopfront with a bay window
            var green = C(H("#1b3a2f"), 0.35f);
            Ledge(k, f, 0.1f, f.Width - 0.1f, 3.6f, 4.3f, 0.3f, green);
            Ledge(k, f, 0.1f, 0.6f, 0, 3.6f, 0.3f, green); Ledge(k, f, f.Width - 0.6f, f.Width - 0.1f, 0, 3.6f, 0.3f, green);
            Sign(k, f, f.Width / 2, 6f, 3.7f, 0.5f, Row(BuildingType.Bookstore), closed);
            var bay = Glass(closed);
            var b0 = f.At(1.2f, 0.6f); var b1 = f.At(4.6f, 0.6f);
            k.Box(new Vector3(b0.x, 0.06f, b0.z - 0.8f), new Vector3(b1.x, 0.6f, b0.z), green, green);
            k.Box(new Vector3(b0.x, 0.6f, b0.z - 0.8f), new Vector3(b1.x, 3.0f, b0.z - 0.75f), bay, bay);
            k.Box(new Vector3(b0.x, 3.0f, b0.z - 0.85f), new Vector3(b1.x, 3.2f, b0.z), green, green);
            // a cart of books on the pavement
            var wood = T("wood", new Color(0.6f, 0.42f, 0.28f));
            var p = f.At(f.Width - 2.2f, 0) + Vector3.back * 1.2f;
            k.Box(p + new Vector3(-0.8f, 0.4f, -0.4f), p + new Vector3(0.8f, 0.9f, 0.4f), wood, wood);
            Color32[] spines = { H("#c0392b"), H("#2e6b8c"), H("#f1c40f"), H("#27ae60"), H("#8e44ad") };
            for (int i = 0; i < 8; i++) k.Box(p + new Vector3(-0.75f + i * 0.19f, 0.9f, -0.3f), p + new Vector3(-0.6f + i * 0.19f, 1.25f, 0.3f), C(spines[i % 5], 0.3f), C(spines[i % 5], 0.3f));
        }

        static void ToyStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.6f, T("wall_stucco", new Color(0.99f, 0.95f, 0.75f)), "gf_store_light", closed, season, Row(BuildingType.ToyStore), C(H("#ff5722")));
            Awning(k, f, 0.3f, f.Width - 0.3f, 3.85f, 1.4f, H("#1e88e5"));
            // giant toy blocks on the parapet
            Color32[] cols = { H("#e53935"), H("#1e88e5"), H("#fdd835"), H("#43a047") };
            for (int i = 0; i < 4; i++)
            {
                float x = -3 + i * 1.6f;
                k.Box(new Vector3(x, 5.6f, -d / 2 + 2.6f), new Vector3(x + 1.3f, 6.9f, -d / 2 + 3.9f), C(cols[i], 0.4f), C(cols[(i + 2) % 4], 0.4f));
            }
            k.Box(new Vector3(-2.2f, 6.9f, -d / 2 + 2.6f), new Vector3(-0.9f, 8.2f, -d / 2 + 3.9f), C(cols[3], 0.4f), C(cols[0], 0.4f));
        }

        static void SportsStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 2.6f, d / 2 - 0.4f, 6.2f, T("metal_panel", new Color(0.85f, 0.88f, 0.92f)), "gf_store_light", closed, season, -1, C(H("#0d47a1")), 0.25f, 0.95f);
            Ledge(k, f, -0.1f, f.Width + 0.1f, 3.9f, 6.1f, 0.2f, C(H("#0d47a1"), 0.4f));
            Sign(k, f, f.Width * 0.6f, 9f, 4.2f, 1.2f, Row(BuildingType.SportsStore), closed);
            // a basketball hoop on the blank end
            var white = C(H("#f4f4f2"), 0.4f); var orange = C(H("#ff6f00"), 0.4f);
            var p = f.At(2.2f, 0) + Vector3.back * 0.1f;
            Ledge(k, f, 1.2f, 3.2f, 4.4f, 5.6f, 0.08f, white);
            k.Cylinder(p + new Vector3(0, 4.5f, -0.6f), 0.35f, 0.35f, 0.05f, 10, orange, null);
            k.Blob(p + new Vector3(0.4f, 0.3f, -0.8f), new Vector3(0.24f, 0.24f, 0.24f), H("#ff6f00"));
        }

        static void Jeweler(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var stone = T("wall_stone", new Color(0.9f, 0.88f, 0.84f));
            var gold = C(H("#d4af37"), 0.7f, 0.9f);
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 2.4f, d / 2 - 0.4f, 6f, stone, "gf_store_dark", closed, season, -1, gold, 0.18f, 0.82f, -1, 3.2f);
            Ledge(k, f, -0.15f, f.Width + 0.15f, 5.4f, 6f, 0.35f, stone, gold);
            PlainAwning(k, f, 1.2f, f.Width - 1.2f, 3.9f, 1.0f, H("#111111"));
            Sign(k, f, f.Width / 2, 5.2f, 4.3f, 0.65f, Row(BuildingType.Jeweler), closed);
            foreach (float a in new[] { 0.6f, f.Width - 0.6f }) k.Cylinder(f.At(a, 0) + Vector3.back * 0.3f, 0.22f, 0.22f, 5.4f, 10, stone, stone);
            // a big gold diamond over the door
            var dp = f.At(f.Width / 2, 4.95f) + Vector3.back * 0.5f;
            k.Cylinder(dp, 0.0f, 0.5f, 0.4f, 6, gold, gold);
            k.Cylinder(dp + Vector3.up * 0.4f, 0.5f, 0.3f, 0.2f, 6, gold, gold);
        }

        static void LiquorStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3.6f, d / 2 - 0.4f, 4.8f, S("wall_brick_tan"), "gf_store_dark", closed, season, Row(BuildingType.LiquorStore), C(H("#4a0e1f")), 0.2f, 0.8f, 7.5f, 3.2f);
            // bars over the windows, an OPEN sign, a bottle painted on the side
            var bar = C(H("#2b2b2b"), 0.4f, 0.6f);
            for (float a = f.Width * 0.2f; a <= f.Width * 0.8f; a += 0.35f) Ledge(k, f, a - 0.03f, a + 0.03f, 0.6f, 3.2f, 0.06f, bar);
            Sign(k, f, f.Width * 0.12f, 1.2f, 2.2f, 0.4f, SignRow.Open, closed);
            var side = Faces(-w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3.6f, d / 2 - 0.4f)[East];
            Rect(k, side, 2, 2.8f, 0.8f, 3.4f, C(H("#4a0e1f"), 0.4f), Vector2.zero, Vector2.one, 0.03f);
            Rect(k, side, 2.25f, 2.55f, 3.4f, 4.2f, C(H("#4a0e1f"), 0.4f), Vector2.zero, Vector2.one, 0.03f);
        }

        static void PetStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.2f, T("wall_stucco", new Color(1f, 0.93f, 0.6f)), "gf_store_light", closed, season, Row(BuildingType.PetStore), C(H("#29b6f6")));
            PlainAwning(k, f, 0.3f, f.Width - 0.3f, 3.85f, 1.3f, H("#29b6f6"));
            // a dog house by the door, paw prints up the wall
            var red = T("wall_board", new Color(0.8f, 0.25f, 0.2f));
            var p = f.At(f.Width - 1.6f, 0.06f) + Vector3.back * 1.3f;
            k.Box(p + new Vector3(-0.6f, 0, -0.5f), p + new Vector3(0.6f, 0.8f, 0.5f), red, red);
            k.Gable(p.x - 0.6f, p.z - 0.5f, p.x + 0.6f, p.z + 0.5f, 0.86f, 0.4f, C(H("#3a3e44")), C(H("#a83224")), 0.08f);
            for (int i = 0; i < 4; i++) k.Blob(f.At(0.6f + i * 0.5f, 4.0f + i * 0.35f) + Vector3.back * 0.05f, new Vector3(0.12f, 0.12f, 0.03f), H("#5b3a1a"));
        }

        static void GardenCenter(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 4f);
            // the shop under a greenhouse roof, tables of plants outside, soil bags, a fence
            var f = Storefront(k, -w / 2 + 1, 3, 1, d / 2 - 1, 4.4f, T("wall_board", new Color(0.3f, 0.5f, 0.32f)), "gf_store_wood", closed, season, Row(BuildingType.GardenCenter), C(H("#f4f4f2")));
            Greenhouse(k, 4, w / 2 - 1, 1, d / 2 - 1, 3.4f, closed);
            var table = T("wood", new Color(0.7f, 0.58f, 0.42f));
            Color32[] bloom = { H("#e74c3c"), H("#f1c40f"), H("#9b59b6"), H("#e84393"), H("#ffffff"), H("#2ecc71") };
            for (int r = 0; r < 3; r++)
            {
                float z = -d / 2 + 2 + r * 2.6f;
                k.Box(new Vector3(-w / 2 + 2, 0.7f, z), new Vector3(w / 2 - 6, 0.8f, z + 1.2f), table, table);
                for (float x = -w / 2 + 2.4f; x < w / 2 - 6.2f; x += 0.7f)
                {
                    if (season == 0) continue;
                    k.Blob(new Vector3(x, 1.05f, z + 0.6f), new Vector3(0.28f, 0.22f, 0.28f), bloom[(int)(Next(ref s) % 6)]);
                }
            }
            var bag = C(H("#5d4037"), 0.2f);
            for (int i = 0; i < 6; i++) k.Box(new Vector3(w / 2 - 5 + (i % 3) * 1.2f, 0.06f + (i / 3) * 0.4f, -d / 2 + 2), new Vector3(w / 2 - 4 + (i % 3) * 1.2f, 0.45f + (i / 3) * 0.4f, -d / 2 + 2.8f), bag, bag);
            for (int i = 0; i < 4; i++) PropArt.Pine(k, new Vector3(w / 2 - 2, 0.06f, -d / 2 + 2 + i * 2.2f), 0.5f, season);
        }

        static void Florist(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_cream", "gf_store_wood", 2, ref s, closed, season, out top);
            Ledge(k, f, 0.1f, f.Width - 0.1f, 3.6f, 4.25f, 0.25f, C(H("#fce4ec"), 0.4f));
            Sign(k, f, f.Width / 2, 6f, 3.65f, 0.55f, Row(BuildingType.Florist), closed);
            PlainAwning(k, f, 0.3f, f.Width - 0.3f, 3.55f, 1.2f, H("#2e7d32"));
            // buckets of flowers on a stepped stand outside
            var stand = T("wood", new Color(0.85f, 0.75f, 0.6f)); var bucket = C(H("#9aa3ab"), 0.5f, 0.6f);
            Color32[] bloom = { H("#e74c3c"), H("#f1c40f"), H("#e84393"), H("#ffffff"), H("#9b59b6") };
            for (int r = 0; r < 2; r++)
            {
                var p = f.At(1, 0.06f) + Vector3.back * (1.6f - r * 0.6f);
                k.Box(p + new Vector3(0, 0, -0.3f), p + new Vector3(f.Width - 2, 0.4f + r * 0.4f, 0.3f), stand, stand);
                for (float a = 0.4f; a < f.Width - 2.2f; a += 0.7f)
                {
                    var q = p + new Vector3(a, 0.4f + r * 0.4f, 0);
                    k.Cylinder(q, 0.2f, 0.24f, 0.35f, 6, bucket, bucket);
                    if (season != 0) k.Blob(q + Vector3.up * 0.55f, new Vector3(0.24f, 0.22f, 0.24f), bloom[(int)(Next(ref s) % 5)]);
                }
            }
        }

        static void BikeShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.2f, S("wall_brick_grey"), "gf_rollup", closed, season, Row(BuildingType.BikeShop), C(H("#ff6f00")), 0.1f, 0.6f);
            Rect(k, f, f.Width * 0.62f, f.Width * 0.92f, 0.3f, 3.4f, Glass(closed), Vector2.zero, new Vector2(0.5f, 1), 0.03f);
            for (int i = 0; i < 4; i++) Bicycle(k, -w / 2 + 1.5f + i * 1.9f, -d / 2 + 1.0f, 90, i % 2 == 0 ? H("#ff6f00") : H("#1e88e5"));
        }

        static void MusicStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 6f, T("wall_stucco", new Color(0.15f, 0.15f, 0.17f)), "gf_store_dark", closed, season, Row(BuildingType.MusicStore), C(H("#ff4081")));
            Neon(k, f, 0.2f, f.Width - 0.2f, 3.5f, H("#ff4081"), closed);
            // a giant guitar up the side of the front
            var body = C(H("#c0392b"), 0.6f); var neck = C(H("#5d4037"), 0.4f); var black = C(H("#1b1b1b"), 0.5f);
            var p = f.At(f.Width - 0.7f, 0) + Vector3.back * 0.3f;
            k.Cylinder(p + Vector3.up * 3.6f, 0.75f, 0.75f, 0.25f, 12, body, body);
            k.Box(p + new Vector3(-0.18f, 3.6f, -0.25f), p + new Vector3(0.18f, 3.85f, 0.25f), body, body);
            k.Box(p + new Vector3(-0.12f, 4.4f, -0.05f), p + new Vector3(0.12f, 7.0f, 0.05f), neck, neck);
            k.Box(p + new Vector3(-0.2f, 7.0f, -0.08f), p + new Vector3(0.2f, 7.6f, 0.08f), black, black);
        }

        static void StationeryStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.2f, T("wall_stucco", new Color(1f, 0.98f, 0.88f)), "gf_store_light", closed, season, Row(BuildingType.StationeryStore), C(H("#283593")));
            Awning(k, f, 0.3f, f.Width - 0.3f, 3.85f, 1.2f, H("#283593"));
            // a pencil laid along the parapet
            var yel = C(H("#fdd835"), 0.4f); var tip = C(H("#f4d6a8"), 0.2f);
            k.Push(new Vector3(-3.6f, 5.55f, -d / 2 + 2.7f), 0);
            k.Box(new Vector3(0, 0, -0.25f), new Vector3(6, 0.5f, 0.25f), yel, yel);
            k.Box(new Vector3(6, 0.1f, -0.15f), new Vector3(6.8f, 0.4f, 0.15f), tip, tip);
            k.Box(new Vector3(-0.5f, 0, -0.25f), new Vector3(0, 0.5f, 0.25f), C(H("#f48fb1")), C(H("#f48fb1")));
            k.Pop();
        }

        static void HomeGoodsStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var wood = T("wall_siding", new Color(0.9f, 0.84f, 0.76f));
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 2.6f, d / 2 - 0.4f, 6f, wood, "gf_store_light", closed, season, Row(BuildingType.HomeGoodsStore), C(H("#5d4037")), 0.06f, 0.94f, 8f, 3.8f);
            // a gable over the door and lamps glowing in the windows
            var roof = Snowy(T("roof_shingle", new Color(0.36f, 0.3f, 0.27f)), season);
            k.Push(new Vector3(0, 0, -d / 2 + 2.6f), 0);
            k.Gable(-2.4f, -1.4f, 2.4f, 0.2f, 4.2f, 1.2f, roof, wood, 0.2f);
            k.Pop();
            if (!closed)
                for (float a = 2; a < f.Width - 1; a += 3.4f)
                    k.Cylinder(f.At(a, 2.4f) + Vector3.forward * 0.5f, 0.3f, 0.18f, 0.4f, 8, Lit(H("#ffd59a"), 2f), Lit(H("#ffd59a"), 2f));
        }

        static void Boutique(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_cream", "gf_store_dark", 2, ref s, closed, season, out top);
            var black = C(H("#111111"), 0.5f); var gold = C(H("#d4af37"), 0.7f, 0.9f);
            Ledge(k, f, 0, f.Width, 3.55f, 4.25f, 0.3f, black);
            Ledge(k, f, 0, f.Width, 3.5f, 3.56f, 0.32f, gold);
            Sign(k, f, f.Width / 2, 5f, 3.6f, 0.55f, Row(BuildingType.Boutique), closed);
            // mannequins in the window
            var form = C(H("#e8e2d4"), 0.4f);
            for (int i = 0; i < 3; i++)
            {
                var p = f.At(1.5f + i * 2.6f, 0) + Vector3.forward * 0.6f;
                k.Cylinder(p, 0.2f, 0.05f, 1.0f, 6, form, null);
                k.Cylinder(p + Vector3.up * 1.0f, 0.2f, 0.32f, 0.7f, 8, C(i == 1 ? H("#ad1457") : H("#111111"), 0.3f), null);
                k.Blob(p + Vector3.up * 1.95f, new Vector3(0.13f, 0.16f, 0.13f), H("#e8e2d4"));
            }
            Planter(k, -w / 2 + 0.9f, -d / 2 + 1.3f, season); Planter(k, w / 2 - 0.9f, -d / 2 + 1.3f, season);
        }

        static void ThriftStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.0f, S("wall_brick_red"), "gf_store_wood", closed, season, Row(BuildingType.ThriftStore), C(H("#7cb342")));
            PlainAwning(k, f, 0.3f, f.Width - 0.3f, 3.85f, 1.2f, H("#7cb342"));
            // a rail of clothes on the pavement
            var rail = C(H("#9aa3ab"), 0.5f, 0.6f);
            var p = f.At(1.2f, 0.06f) + Vector3.back * 1.4f;
            k.Box(p + new Vector3(0, 0, -0.03f), p + new Vector3(0.06f, 1.6f, 0.03f), rail, rail);
            k.Box(p + new Vector3(3.0f, 0, -0.03f), p + new Vector3(3.06f, 1.6f, 0.03f), rail, rail);
            k.Box(p + new Vector3(0, 1.55f, -0.03f), p + new Vector3(3.06f, 1.6f, 0.03f), rail, rail);
            Color32[] cols = { H("#c0392b"), H("#2e6b8c"), H("#f1c40f"), H("#8e44ad"), H("#ecf0f1"), H("#27ae60") };
            for (int i = 0; i < 8; i++) k.Box(p + new Vector3(0.2f + i * 0.34f, 0.7f, -0.25f), p + new Vector3(0.45f + i * 0.34f, 1.5f, 0.25f), C(cols[i % 6], 0.2f), C(cols[i % 6], 0.2f));
        }

        static void DepartmentStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            // three grand storeys: display windows below, a stone upper floor with tall windows, columns and a cornice
            var stone = T("wall_stone", new Color(0.93f, 0.9f, 0.84f));
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 2.6f, z1 = d / 2 - 0.6f;
            var faces = Faces(x0, x1, z0, z1);
            var f = faces[South];
            float h = 13f;
            Rect(k, f, 0.6f, f.Width - 0.6f, 0, 4.4f, Glass(closed), Vector2.zero, new Vector2(Mathf.Round(f.Width / 2.7f) * 0.25f, 1));
            k.Quad(f.At(0, 4.4f), f.At(0, h), f.At(f.Width, h), f.At(f.Width, 4.4f), stone, 3f);
            k.Quad(f.At(0, 0), f.At(0, 4.4f), f.At(0.6f, 4.4f), f.At(0.6f, 0), stone, 3f);
            k.Quad(f.At(f.Width - 0.6f, 0), f.At(f.Width - 0.6f, 4.4f), f.At(f.Width, 4.4f), f.At(f.Width, 0), stone, 3f);
            for (int i = 1; i < 4; i++) Plain(k, faces[i == 1 ? 0 : i], 0, h, stone, 3f);
            FlatRoof(k, x0, x1, z0, z1, h, Snowy(S("roof_membrane"), season), stone, C(H("#cfc6b4")), 0.8f);
            WindowRow(k, f, 0.6f, f.Width - 0.6f, 5.6f, 1.6f, 2.6f, 2.4f, ref s, closed);
            WindowRow(k, f, 0.6f, f.Width - 0.6f, 9.2f, 1.6f, 2.4f, 2.4f, ref s, closed);
            Ledge(k, f, -0.2f, f.Width + 0.2f, 4.4f, 4.8f, 0.4f, stone);
            Ledge(k, f, -0.3f, f.Width + 0.3f, h - 0.6f, h, 0.6f, stone);
            for (float a = 0.6f; a <= f.Width - 0.5f; a += (f.Width - 1.2f) / 6) k.Cylinder(f.At(a, 4.8f) + Vector3.back * 0.35f, 0.28f, 0.24f, h - 5.4f, 10, stone, stone);
            Canopy(k, f, f.Width / 2 - 3, f.Width / 2 + 3, 3.6f, 2.4f, C(H("#b71c1c"), 0.4f), false);
            RoofSign(k, 0, z0 + 1, h + 0.8f, 12f, Row(BuildingType.DepartmentStore), closed);
            Flag(k, x0 + 1, z0 + 1, h + 4, H("#b71c1c")); Flag(k, x1 - 1, z0 + 1, h + 4, H("#b71c1c"));
        }

        static void PhoneStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            // all glass with a white frame, a purple light strip, a giant phone by the door
            var white = C(H("#f4f4f2"), 0.5f);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.4f, white, "gf_store_light", closed, season, Row(BuildingType.PhoneStore), C(H("#6200ea")), 0.03f, 0.97f, -1, 4.0f);
            Neon(k, f, 0, f.Width, 4.0f, H("#7c4dff"), closed, 0.15f);
            var p = f.At(0.9f, 0.06f) + Vector3.back * 1.2f;
            var black = C(H("#1b1b1b"), 0.8f, 0.3f);
            k.Box(p + new Vector3(-0.5f, 0, -0.1f), p + new Vector3(0.5f, 2.0f, 0.1f), black, black);
            k.Box(p + new Vector3(-0.42f, 0.1f, -0.11f), p + new Vector3(0.42f, 1.9f, -0.1f), closed ? C(H("#2b2b2b")) : Lit(H("#7c4dff"), 1.6f), black);
        }

        static void MattressStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.2f, T("metal_panel", new Color(0.9f, 0.93f, 0.98f)), "gf_store_light", closed, season, -1, C(H("#1565c0")));
            Ledge(k, f, -0.1f, f.Width + 0.1f, 3.7f, 5.1f, 0.2f, C(H("#1565c0"), 0.4f));
            Sign(k, f, f.Width / 2, 6f, 3.9f, 0.8f, Row(BuildingType.MattressStore), closed);
            // a mattress leaning on the roof edge, beds in the window
            var white = C(H("#f4f4f2"), 0.4f); var quilt = C(H("#90caf9"), 0.3f);
            k.Box(new Vector3(-2.5f, 5.2f, -d / 2 + 3.2f), new Vector3(2.5f, 7.2f, -d / 2 + 3.6f), white, quilt);
            for (int i = 0; i < 2; i++)
            {
                var p = f.At(2.2f + i * 4.6f, 0) + Vector3.forward * 1.2f;
                k.Box(p + new Vector3(-1, 0, -0.9f), p + new Vector3(1, 0.6f, 0.9f), quilt, white);
            }
        }

        static void ConvenienceStore(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 3.4f, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.95f, 0.94f, 0.9f)), "gf_store_light", closed, season, -1, C(H("#ef6c00")), 0.05f, 0.95f, -1, 3.2f);
            Ledge(k, f, -0.1f, f.Width + 0.1f, 3.3f, 4.5f, 0.3f, C(H("#ef6c00"), 0.4f));
            Sign(k, f, f.Width / 2, 6.6f, 3.45f, 0.75f, Row(BuildingType.ConvenienceStore), closed);
            Sign(k, f, f.Width * 0.85f, 1.2f, 2.0f, 0.4f, SignRow.Open, closed);
            // an ice chest and a newspaper box out front
            var white = C(H("#f4f4f2"), 0.4f); var blue = C(H("#1e88e5"), 0.4f);
            var p = f.At(1.2f, 0.06f) + Vector3.back * 0.7f;
            k.Box(p + new Vector3(-0.6f, 0, -0.4f), p + new Vector3(0.6f, 1.2f, 0.4f), white, blue);
            k.Box(p + new Vector3(1.2f, 0, -0.25f), p + new Vector3(1.7f, 1.0f, 0.25f), blue, blue);
        }

        static void ButcherShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5.2f, T("wall_stucco", new Color(0.98f, 0.97f, 0.94f)), "gf_store_wood", closed, season, Row(BuildingType.Butcher), C(H("#7f0000")));
            Awning(k, f, 0.3f, f.Width - 0.3f, 3.85f, 1.3f, H("#b71c1c"));
            // a band of green tiles below the window, a pig on a bracket sign
            Ledge(k, f, 0, f.Width, 0, 0.7f, 0.05f, C(H("#1b5e20"), 0.6f));
            var pink = C(H("#f4a9a8"), 0.3f);
            var p = f.At(0.3f, 4.4f) + Vector3.back * 0.9f;
            k.Box(p + new Vector3(-0.05f, -0.35f, -0.55f), p + new Vector3(0.05f, 0.25f, 0.45f), pink, pink);
            k.Box(p + new Vector3(-0.05f, -0.15f, -0.75f), p + new Vector3(0.05f, 0.15f, -0.55f), pink, pink);
        }

        static void BakeryShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_tan", "gf_store_wood", 2, ref s, closed, season, out top);
            Ledge(k, f, 0.1f, f.Width - 0.1f, 3.6f, 4.25f, 0.25f, C(H("#f8e1c0"), 0.4f));
            Sign(k, f, f.Width / 2, 5.6f, 3.65f, 0.55f, Row(BuildingType.Bakery), closed);
            PlainAwning(k, f, 0.3f, f.Width - 0.3f, 3.55f, 1.2f, H("#2e6b8c"));
            // loaves in the window, a chimney for the oven, a bench
            var crust = C(H("#c8893f"), 0.3f);
            for (int i = 0; i < 5; i++) k.Blob(f.At(1.4f + i * 1.6f, 1.2f) + Vector3.forward * 0.4f, new Vector3(0.45f, 0.22f, 0.25f), H("#c8893f"));
            Stack(k, w / 2 - 1.5f, d / 2 - 1.8f, top, 0.35f, 2.2f, S("wall_brick_red"));
            PropArt.Bench(k, f.At(f.Width - 1.6f, 0.06f) + Vector3.back * 1.5f, 0);
        }

        static void FarmersMarket(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            // an open market hall: a long roof on timber posts over rows of stalls
            var post = T("wood", new Color(0.5f, 0.38f, 0.28f));
            float x0 = -w / 2 + 1.5f, x1 = w / 2 - 1.5f, z0 = -d / 2 + 4, z1 = d / 2 - 2;
            for (float x = x0; x <= x1 + 0.01f; x += (x1 - x0) / 4)
                foreach (float z in new[] { z0, z1 })
                    k.Box(new Vector3(x - 0.18f, 0.06f, z - 0.18f), new Vector3(x + 0.18f, 4.6f, z + 0.18f), post, post);
            k.Gable(x0, z0, x1, z1, 4.6f, 2.6f, Snowy(T("roof_metal", new Color(0.3f, 0.45f, 0.24f)), season), post, 0.8f);
            var front = new Face { LeftBottom = new Vector3(x0, 0, z0 - 0.82f), Right = Vector3.right, Normal = Vector3.back, Width = x1 - x0 };
            Sign(k, front, front.Width / 2, 8f, 5.1f, 0.9f, Row(BuildingType.FarmersMarket), closed);
            Color32[] produce = { H("#e74c3c"), H("#f39c12"), H("#27ae60"), H("#f1c40f"), H("#8e44ad"), H("#d35400") };
            Color32[] cloth = { H("#e8e2d4"), H("#c0392b"), H("#2e6b8c") };
            var table = T("wood", new Color(0.78f, 0.64f, 0.46f));
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 4; c++)
                {
                    float x = x0 + 1.2f + c * 4.2f, z = z0 + 2 + r * 6f;
                    k.Box(new Vector3(x, 0.8f, z), new Vector3(x + 3.2f, 0.9f, z + 1.4f), table, table);
                    k.Box(new Vector3(x + 0.1f, 0.06f, z + 0.1f), new Vector3(x + 0.25f, 0.8f, z + 1.3f), table, table);
                    k.Box(new Vector3(x + 2.95f, 0.06f, z + 0.1f), new Vector3(x + 3.1f, 0.8f, z + 1.3f), table, table);
                    if (closed) continue;
                    for (int i = 0; i < 4; i++) k.Box(new Vector3(x + 0.2f + i * 0.75f, 0.9f, z + 0.2f), new Vector3(x + 0.85f + i * 0.75f, 1.15f, z + 1.2f), C(produce[(int)(Next(ref s) % 6)], 0.3f), C(produce[(int)(Next(ref s) % 6)], 0.3f));
                    k.Box(new Vector3(x, 2.2f, z - 0.2f), new Vector3(x + 3.2f, 2.3f, z + 1.6f), C(cloth[(r + c) % 3], 0.2f), C(cloth[(r + c) % 3], 0.2f));
                }
            Tree(k, -w / 2 + 1.2f, -d / 2 + 1.5f, 0.8f, season, s);
            Tree(k, w / 2 - 1.2f, -d / 2 + 1.5f, 0.8f, season, s + 4);
        }

        static void FishMarket(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.02f, 3f);
            var blue = T("wall_board", new Color(0.4f, 0.62f, 0.8f));
            var f = GableFront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3, d / 2 - 0.6f, 4f, 2.6f, blue, T("roof_metal", new Color(0.82f, 0.82f, 0.8f)), season)[South];
            Sign(k, f, f.Width / 2, f.Width - 1.6f, 4.3f, 0.75f, Row(BuildingType.FishMarket), closed);
            Rect(k, f, 1, f.Width - 1, 0.9f, 3.4f, Glass(closed), Vector2.zero, new Vector2(0.75f, 1), 0.03f);
            // ice tables out front and a fish over the door
            var ice = C(H("#e1f5fe"), 0.8f); var steel = C(H("#b0b8c0"), 0.6f, 0.8f);
            var p = f.At(1, 0.06f) + Vector3.back * 1.4f;
            k.Box(p + new Vector3(0, 0, -0.45f), p + new Vector3(f.Width - 2, 0.85f, 0.45f), steel, ice);
            for (float a = 0.4f; a < f.Width - 2.3f; a += 0.55f) k.Box(p + new Vector3(a, 0.86f, -0.1f), p + new Vector3(a + 0.4f, 0.98f, 0.12f), C(H("#9aa8b4"), 0.6f, 0.4f), C(H("#9aa8b4"), 0.6f, 0.4f));
            var fish = C(H("#ff8f00"), 0.5f);
            var q = f.At(f.Width / 2, 5.4f) + Vector3.back * 0.2f;
            k.Box(q + new Vector3(-0.9f, -0.3f, -0.1f), q + new Vector3(0.5f, 0.3f, 0.0f), fish, fish);
            k.Box(q + new Vector3(0.5f, -0.45f, -0.1f), q + new Vector3(0.9f, 0.45f, 0.0f), fish, fish);
        }
    }
}
