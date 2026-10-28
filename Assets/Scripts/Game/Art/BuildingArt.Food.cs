using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>The places to eat and drink added later, each its own model.</summary>
    public static partial class BuildingArt
    {
        static void ExtraFood(MeshKit k, BuildingType t, float w, float d, ref uint s, bool closed, int season, int floors)
        {
            switch (t)
            {
                case BuildingType.Pizzeria: Pizzeria(k, w, d, ref s, closed, season); break;
                case BuildingType.BurgerJoint: BurgerJoint(k, w, d, ref s, closed, season); break;
                case BuildingType.Diner: Diner(k, w, d, ref s, closed, season); break;
                case BuildingType.SushiBar: SushiBar(k, w, d, ref s, closed, season); break;
                case BuildingType.TacoShop: TacoShop(k, w, d, ref s, closed, season); break;
                case BuildingType.Steakhouse: Steakhouse(k, w, d, ref s, closed, season); break;
                case BuildingType.NoodleBar: NoodleBar(k, w, d, ref s, closed, season); break;
                case BuildingType.ChickenShack: ChickenShack(k, w, d, ref s, closed, season); break;
                case BuildingType.SandwichShop: SandwichShop(k, w, d, ref s, closed, season); break;
                case BuildingType.FineDining: FineDining(k, w, d, ref s, closed, season); break;
                case BuildingType.BBQJoint: BBQJoint(k, w, d, ref s, closed, season); break;
                case BuildingType.SeafoodRestaurant: Seafood(k, w, d, ref s, closed, season); break;
                case BuildingType.CurryHouse: CurryHouse(k, w, d, ref s, closed, season); break;
                case BuildingType.ThaiRestaurant: ThaiRestaurant(k, w, d, ref s, closed, season); break;
                case BuildingType.TeaHouse: TeaHouse(k, w, d, ref s, closed, season); break;
                case BuildingType.JuiceBar: JuiceBar(k, w, d, ref s, closed, season); break;
                case BuildingType.DonutShop: DonutShop(k, w, d, ref s, closed, season); break;
                case BuildingType.IceCreamParlor: IceCream(k, w, d, ref s, closed, season); break;
                case BuildingType.CandyShop: CandyShop(k, w, d, ref s, closed, season); break;
                case BuildingType.BakeryCafe: BakeryCafe(k, w, d, ref s, closed, season); break;
                case BuildingType.SportsBar: SportsBar(k, w, d, ref s, closed, season); break;
                case BuildingType.WineBar: WineBar(k, w, d, ref s, closed, season); break;
                case BuildingType.Brewpub: Brewpub(k, w, d, ref s, closed, season); break;
                case BuildingType.CocktailLounge: Lounge(k, w, d, ref s, closed, season); break;
                case BuildingType.Nightclub: Nightclub(k, w, d, ref s, closed, season); break;
                case BuildingType.KaraokeBar: Karaoke(k, w, d, ref s, closed, season); break;
                default: ExtraServices(k, t, w, d, ref s, closed, season, floors); break;
            }
        }

        static void Pizzeria(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var brick = S("wall_brick_red");
            var f = GableFront(k, -w / 2 + 0.5f, w / 2 - 0.5f, -d / 2 + 2.6f, d / 2 - 0.5f, 4.6f, 2.4f, brick, T("roof_shingle", new Color(0.72f, 0.36f, 0.26f)), season)[South];
            Rect(k, f, 0.8f, f.Width - 0.8f, 0.3f, 3.3f, Glass(closed), Vector2.zero, new Vector2(0.75f, 1), 0.03f);
            // an awning in green, white and red
            Color32[] it = { H("#2e7d32"), H("#f4f4f2"), H("#c62828") };
            for (int i = 0; i < 3; i++) PlainAwning(k, f, 0.6f + i * (f.Width - 1.2f) / 3, 0.6f + (i + 1) * (f.Width - 1.2f) / 3, 3.75f, 1.2f, it[i]);
            Sign(k, f, f.Width / 2, 5f, 4.7f, 0.55f, Row(BuildingType.Pizzeria), closed);
            Stack(k, w / 2 - 1.6f, d / 2 - 2, 4.6f, 0.45f, 3.2f, brick);
            Patio(k, f, 0.5f, f.Width - 0.5f, 1.9f, H("#2e7d32"));
        }

        static void BurgerJoint(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            // a low box with a big red mansard band, a drive-through window and a burger on a pole
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 2.6f, -d / 2 + 3, d / 2 - 0.6f, 4.0f, T("wall_brick_tan", new Color(1f, 0.95f, 0.85f)), "gf_store_light", closed, season, -1, C(H("#b71c1c")), 0.08f, 0.8f, -1, 3.0f);
            var red = C(H("#c62828"), 0.4f);
            Ledge(k, f, -0.4f, f.Width + 0.4f, 3.0f, 4.0f, 0.6f, red, Snowy(red, season));
            Sign(k, Out(f, 0.6f), f.Width / 2, 5f, 3.15f, 0.65f, Row(BuildingType.BurgerJoint), closed);
            var east = Faces(-w / 2 + 0.6f, w / 2 - 2.6f, -d / 2 + 3, d / 2 - 0.6f)[East];
            Rect(k, east, 2.2f, 3.4f, 1.0f, 2.2f, Glass(closed), Vector2.zero, new Vector2(0.25f, 1), 0.04f);
            Canopy(k, east, 1.8f, 3.8f, 3.0f, 1.2f, red, false);
            var post = C(H("#3a3e44"), 0.4f, 0.4f);
            k.Box(new Vector3(w / 2 - 1.2f, 0.06f, -d / 2 + 1), new Vector3(w / 2 - 0.9f, 5.5f, -d / 2 + 1.3f), post, post);
            var p = new Vector3(w / 2 - 1.05f, 5.5f, -d / 2 + 1.15f);
            k.Cylinder(p, 0.8f, 0.9f, 0.3f, 12, C(H("#e0a040"), 0.3f), null);
            k.Cylinder(p + Vector3.up * 0.3f, 0.95f, 0.95f, 0.25f, 12, C(H("#5b3a1a"), 0.3f), null);
            k.Cylinder(p + Vector3.up * 0.55f, 0.98f, 0.98f, 0.08f, 12, C(H("#43a047"), 0.3f), null);
            k.Cylinder(p + Vector3.up * 0.63f, 0.9f, 0.3f, 0.45f, 12, C(H("#e0a040"), 0.3f), C(H("#e0a040"), 0.3f));
        }

        static void Diner(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            // a stainless dining car: rounded ends, a band of windows, a coloured stripe, a neon sign on top
            var steel = C(H("#d0d6dc"), 0.85f, 0.9f); var stripe = C(H("#d32f2f"), 0.5f);
            float x0 = -w / 2 + 1.2f, x1 = w / 2 - 1.2f, z0 = -d / 2 + 3, z1 = z0 + 4.4f;
            float r = (z1 - z0) / 2, zc = (z0 + z1) / 2;
            k.Box(new Vector3(x0 + r, 0.06f, z0), new Vector3(x1 - r, 3.6f, z1), steel, steel);
            k.Cylinder(new Vector3(x0 + r, 0.06f, zc), r, r, 3.54f, 12, steel, steel);
            k.Cylinder(new Vector3(x1 - r, 0.06f, zc), r, r, 3.54f, 12, steel, steel);
            var f = new Face { LeftBottom = new Vector3(x0 + r, 0, z0), Right = Vector3.right, Normal = Vector3.back, Width = x1 - x0 - 2 * r };
            Ledge(k, f, 0, f.Width, 0.9f, 1.1f, 0.05f, stripe);
            Rect(k, f, 0.2f, f.Width - 0.2f, 1.4f, 2.6f, MaterialLibrary.Pane(closed ? new Color32(26, 30, 38, 255) : new Color32(255, 230, 180, 255)), Vector2.zero, Vector2.one, 0.04f);
            Ledge(k, f, 0, f.Width, 2.8f, 3.0f, 0.05f, stripe);
            k.Box(new Vector3(x0 + r, 3.6f, z0 + 0.3f), new Vector3(x1 - r, 4.1f, z1 - 0.3f), steel, Snowy(steel, season));
            RoofSign(k, 0, z0 + 1.2f, 4.1f, 4.2f, Row(BuildingType.Diner), closed);
            Door(k, f, f.Width / 2, H("#b0b8c0"), 1.0f, 2.3f);
            var back = GableBox(k, -2, 3, z1, d / 2 - 0.6f, 3.2f, 1.2f, S("wall_brick_tan"), T("roof_metal", new Color(0.6f, 0.6f, 0.62f)), season);
        }

        static void SushiBar(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var wood = T("wood", new Color(0.42f, 0.3f, 0.22f));
            var f = Storefront(k, -w / 2 + 0.5f, w / 2 - 0.5f, -d / 2 + 2.6f, d / 2 - 0.5f, 4.8f, T("wall_stucco", new Color(0.9f, 0.88f, 0.84f)), "gf_store_wood", closed, season, Row(BuildingType.SushiBar), C(H("#111111")), 0.2f, 0.8f, 4.4f, 3.2f);
            // dark timber slats, a short noren curtain over the door, red paper lanterns, a tiled hood
            for (float a = 0.1f; a < f.Width * 0.2f; a += 0.35f) Ledge(k, f, a, a + 0.18f, 0, 4.8f, 0.08f, wood);
            for (float a = f.Width * 0.8f; a < f.Width - 0.1f; a += 0.35f) Ledge(k, f, a, a + 0.18f, 0, 4.8f, 0.08f, wood);
            var noren = C(H("#1a237e"), 0.2f);
            for (int i = 0; i < 4; i++) Ledge(k, f, f.Width / 2 - 1.2f + i * 0.62f, f.Width / 2 - 0.62f + i * 0.62f, 2.2f, 3.1f, 0.12f, noren);
            var lantern = closed ? C(H("#7a3030")) : Lit(H("#ff5252"), 2f);
            foreach (float a in new[] { 1.0f, f.Width - 1.0f }) k.Cylinder(f.At(a, 2.4f) + Vector3.back * 0.5f, 0.28f, 0.28f, 0.6f, 8, lantern, lantern);
            var roof = Snowy(T("roof_shingle", new Color(0.25f, 0.25f, 0.27f)), season);
            k.Push(new Vector3(0, 0, -d / 2 + 2.6f), 0);
            k.Gable(-w / 2 + 0.3f, -1.2f, w / 2 - 0.3f, 0.3f, 3.6f, 0.5f, roof, wood, 0.2f);
            k.Pop();
        }

        static void TacoShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var adobe = T("wall_stucco", new Color(1f, 0.62f, 0.3f));
            var f = Storefront(k, -w / 2 + 0.5f, w / 2 - 0.5f, -d / 2 + 3.2f, d / 2 - 0.5f, 4.6f, adobe, "gf_store_wood", closed, season, Row(BuildingType.TacoShop), C(H("#26a69a")), 0.25f, 0.75f, 4.6f);
            // a stepped parapet, turquoise trim, string lights over the patio
            var teal = C(H("#26a69a"), 0.4f);
            Ledge(k, f, f.Width / 2 - 1.6f, f.Width / 2 + 1.6f, 4.6f, 5.6f, 0.25f, adobe, teal);
            Ledge(k, f, f.Width / 2 - 0.8f, f.Width / 2 + 0.8f, 5.6f, 6.2f, 0.25f, adobe, teal);
            foreach (float a in new[] { 0.4f, f.Width - 2.2f }) Rect(k, f, a, a + 1.8f, 1.2f, 2.8f, MaterialLibrary.Pane(closed ? new Color32(26, 30, 38, 255) : new Color32(255, 214, 140, 255)), Vector2.zero, Vector2.one, 0.04f);
            var post = T("wood", new Color(0.45f, 0.32f, 0.22f));
            k.Box(new Vector3(-w / 2 + 0.6f, 0.06f, -d / 2 + 0.5f), new Vector3(-w / 2 + 0.8f, 3.0f, -d / 2 + 0.7f), post, post);
            k.Box(new Vector3(w / 2 - 0.8f, 0.06f, -d / 2 + 0.5f), new Vector3(w / 2 - 0.6f, 3.0f, -d / 2 + 0.7f), post, post);
            Color32[] bulbs = { H("#ff5252"), H("#ffd740"), H("#69f0ae"), H("#40c4ff") };
            for (int i = 0; i < 12; i++)
            {
                float x = -w / 2 + 0.9f + i * (w - 1.8f) / 11, sag = Mathf.Sin(i / 11f * Mathf.PI) * 0.5f;
                k.Cylinder(new Vector3(x, 2.8f - sag, -d / 2 + 0.6f), 0.08f, 0.08f, 0.14f, 6, closed ? C(H("#555555")) : Lit(bulbs[i % 4], 3f), null);
            }
            Patio(k, f, 0.5f, f.Width - 0.5f, 1.8f, H("#ff7043"));
        }

        static void Steakhouse(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            // a log-and-stone lodge with a big timber porch and a steer's horns over the door
            var logs = T("wood", new Color(0.55f, 0.38f, 0.24f));
            var stone = T("wall_stone", new Color(0.78f, 0.74f, 0.68f));
            var f = GableBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 4, d / 2 - 0.6f, 4.6f, 2.4f, logs, T("roof_shingle", new Color(0.3f, 0.32f, 0.3f)), season, 0.6f);
            Ledge(k, f[South], 0, f[South].Width, 0, 1.2f, 0.12f, stone);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.6f, 1.2f, 1.6f, 2.8f, ref s, closed);
            var post = T("wood", new Color(0.42f, 0.3f, 0.2f));
            var front = f[South];
            k.Push(new Vector3(0, 0, -d / 2 + 4), 0);
            k.Gable(-3.2f, -2.6f, 3.2f, 0.4f, 3.6f, 1.6f, Snowy(T("roof_shingle", new Color(0.3f, 0.32f, 0.3f)), season), post, 0.2f);
            k.Pop();
            foreach (float x in new[] { -3f, 3f }) k.Box(new Vector3(x - 0.18f, 0.06f, -d / 2 + 1.5f), new Vector3(x + 0.18f, 3.6f, -d / 2 + 1.86f), post, post);
            Sign(k, front, front.Width / 2, 6f, 5.6f, 0.7f, Row(BuildingType.Steakhouse), closed);
            var horn = C(H("#e8dcc0"), 0.4f);
            var hp = front.At(front.Width / 2, 5.3f) + Vector3.back * 2.8f;
            k.Box(hp + new Vector3(-1.4f, 0, -0.06f), hp + new Vector3(1.4f, 0.16f, 0.06f), horn, horn);
            k.Box(hp + new Vector3(-1.5f, 0.1f, -0.06f), hp + new Vector3(-1.32f, 0.6f, 0.06f), horn, horn);
            k.Box(hp + new Vector3(1.32f, 0.1f, -0.06f), hp + new Vector3(1.5f, 0.6f, 0.06f), horn, horn);
            Stack(k, w / 2 - 3, 2, 4.6f, 0.55f, 3.6f, stone);
        }

        static void NoodleBar(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_grey", "gf_store_dark", 2, ref s, closed, season, out top);
            var red = C(H("#c62828"), 0.4f);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, red);
            Sign(k, Out(f, 0.3f), f.Width / 2, 5.6f, 3.6f, 0.6f, Row(BuildingType.NoodleBar), closed);
            // a row of lanterns, a vertical blade sign and a steam vent
            var lantern = closed ? C(H("#7a3030")) : Lit(H("#ffab40"), 2f);
            for (float a = 0.8f; a < f.Width - 0.5f; a += 1.4f) k.Cylinder(f.At(a, 2.9f) + Vector3.back * 0.6f, 0.22f, 0.22f, 0.5f, 8, lantern, lantern);
            var blade = f.At(0.15f, 5.2f) + Vector3.back * 0.6f;
            k.Box(blade + new Vector3(-0.08f, 0, -0.5f), blade + new Vector3(0.08f, 3.2f, 0.5f), red, red);
            Neon(k, Out(f, 0.3f), 0.25f, f.Width - 0.25f, 3.42f, H("#ffd740"), closed, 0.06f);
            k.Cylinder(new Vector3(w / 2 - 1.5f, top, d / 2 - 1.5f), 0.3f, 0.3f, 1.4f, 8, C(H("#b0b8c0"), 0.6f, 0.8f), C(H("#3a3e44")));
        }

        static void ChickenShack(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var white = T("wall_stucco", new Color(0.98f, 0.98f, 0.96f));
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3, d / 2 - 0.6f, 4.4f, white, "gf_store_light", closed, season, Row(BuildingType.ChickenShack), C(H("#c62828")), 0.15f, 0.85f, -1, 3.0f);
            // red and white stripes, and a bucket on the roof
            for (int i = 0; i < 9; i++) Ledge(k, f, i * f.Width / 9, (i + 1) * f.Width / 9, 3.0f, 4.4f, 0.12f, C(i % 2 == 0 ? H("#c62828") : H("#f4f4f2"), 0.3f));
            Sign(k, Out(f, 0.12f), f.Width / 2, 6.6f, 3.2f, 0.7f, Row(BuildingType.ChickenShack), closed);
            var red = C(H("#c62828"), 0.4f); var whiteC = C(H("#f4f4f2"), 0.3f);
            k.Cylinder(new Vector3(0, 4.4f, 1), 1.2f, 1.6f, 2.4f, 12, red, null);
            k.Cylinder(new Vector3(0, 5.6f, 1), 1.42f, 1.48f, 0.3f, 12, whiteC, null);
            k.Blob(new Vector3(0, 6.9f, 1), new Vector3(1.3f, 0.6f, 1.3f), H("#c8893f"));
        }

        static void SandwichShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 4.6f, T("wall_siding", new Color(0.92f, 0.95f, 0.85f)), "gf_store_light", closed, season, Row(BuildingType.SandwichShop), C(H("#2e7d32")));
            PlainAwning(k, f, 0.3f, f.Width - 0.3f, 3.8f, 1.1f, H("#ffeb3b"));
            Ledge(k, f, 0, f.Width, 0, 0.5f, 0.05f, C(H("#2e7d32"), 0.4f));
            PropArt.Bench(k, f.At(1.6f, 0.06f) + Vector3.back * 1.6f, 0);
            PropArt.Bench(k, f.At(f.Width - 1.6f, 0.06f) + Vector3.back * 1.6f, 0);
            // a sub roll on the parapet
            k.Blob(new Vector3(0, 5.1f, -d / 2 + 2.9f), new Vector3(2.2f, 0.45f, 0.45f), H("#d9a85a"));
            k.Box(new Vector3(-2f, 5.0f, -d / 2 + 2.45f), new Vector3(2f, 5.15f, -d / 2 + 3.35f), C(H("#43a047")), C(H("#43a047")));
        }

        static void FineDining(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            // a white stucco villa: tall arched windows, a canopy to the kerb, clipped topiary
            var stucco = T("wall_stucco", new Color(0.97f, 0.95f, 0.9f));
            var f = FlatBox(k, -w / 2 + 1, w / 2 - 1, -d / 2 + 4, d / 2 - 0.6f, 6.4f, stucco, C(H("#e6d3a3"), 0.4f), season, 0.9f)[South];
            var pane = MaterialLibrary.Pane(closed ? new Color32(26, 30, 38, 255) : new Color32(255, 214, 150, 255));
            for (float a = 1.4f; a < f.Width - 1; a += 2.6f)
            {
                Rect(k, f, a, a + 1.4f, 0.8f, 4.4f, pane, Vector2.zero, Vector2.one, 0.04f);
                k.Cylinder(f.At(a + 0.7f, 4.4f) + Vector3.back * 0.05f, 0.7f, 0.0f, 0.55f, 6, stucco, stucco);
            }
            var black = C(H("#1b1b1b"), 0.5f); var gold = C(H("#e6d3a3"), 0.6f, 0.6f);
            k.Box(new Vector3(-1.4f, 3.2f, -d / 2 + 0.6f), new Vector3(1.4f, 3.45f, -d / 2 + 4), black, black);
            foreach (float x in new[] { -1.3f, 1.3f }) k.Box(new Vector3(x - 0.05f, 0.06f, -d / 2 + 0.6f), new Vector3(x + 0.05f, 3.2f, -d / 2 + 0.7f), gold, gold);
            Sign(k, f, f.Width / 2, 5f, 5.0f, 0.6f, Row(BuildingType.FineDining), closed);
            foreach (float x in new[] { -w / 2 + 2.2f, -w / 2 + 4.4f, w / 2 - 4.4f, w / 2 - 2.2f })
            {
                k.Box(new Vector3(x - 0.4f, 0.06f, -d / 2 + 2.6f), new Vector3(x + 0.4f, 0.7f, -d / 2 + 3.4f), C(H("#7a6a58")), C(H("#7a6a58")));
                k.Blob(new Vector3(x, 1.3f, -d / 2 + 3), new Vector3(0.5f, 0.6f, 0.5f), season == 0 ? H("#d8dde0") : H("#2f6b35"));
            }
        }

        static void BBQJoint(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, season == 0 ? S("snow") : S("gravel"), 0.05f, 4f);
            var board = T("wall_board", new Color(0.62f, 0.22f, 0.18f));
            var f = GableBox(k, -w / 2 + 1, 4, -d / 2 + 3.5f, d / 2 - 0.6f, 4f, 2.2f, board, T("roof_metal", new Color(0.58f, 0.58f, 0.6f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.4f, 1.2f, 1.2f, 2.6f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 4.6f, 3.1f, 0.6f, Row(BuildingType.BBQJoint), closed);
            // the smokers out the side, picnic tables under a lean-to
            var black = C(H("#2b2b2b"), 0.5f, 0.5f);
            for (int i = 0; i < 2; i++)
            {
                float z = -d / 2 + 4 + i * 2.6f;
                k.Box(new Vector3(5, 0.06f, z - 0.1f), new Vector3(5.2f, 0.9f, z + 0.1f), black, black);
                k.Box(new Vector3(8, 0.06f, z - 0.1f), new Vector3(8.2f, 0.9f, z + 0.1f), black, black);
                k.Box(new Vector3(5, 0.9f, z - 0.6f), new Vector3(8.2f, 1.9f, z + 0.6f), black, black);
                k.Cylinder(new Vector3(7.6f, 1.9f, z), 0.15f, 0.15f, 1.6f, 6, black, black);
            }
            var wood = T("wood", new Color(0.7f, 0.56f, 0.4f));
            for (int i = 0; i < 2; i++)
            {
                float x = -w / 2 + 2.5f + i * 4.5f, z = -d / 2 + 1.8f;
                k.Box(new Vector3(x, 0.7f, z - 0.4f), new Vector3(x + 2.2f, 0.8f, z + 0.4f), wood, wood);
                k.Box(new Vector3(x, 0.4f, z - 0.9f), new Vector3(x + 2.2f, 0.48f, z - 0.6f), wood, wood);
                k.Box(new Vector3(x, 0.4f, z + 0.6f), new Vector3(x + 2.2f, 0.48f, z + 0.9f), wood, wood);
                k.Box(new Vector3(x + 1.05f, 0.06f, z - 0.1f), new Vector3(x + 1.15f, 0.7f, z + 0.1f), wood, wood);
            }
            Timber(k, w / 2 - 2.5f, d / 2 - 5, w / 2 - 0.8f, d / 2 - 1, 1.2f);
        }

        static void Seafood(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            // a grey-shingled waterfront house with white trim, a little lighthouse tower and rope posts
            var shingle = T("wall_siding", new Color(0.62f, 0.64f, 0.66f));
            var f = GableBox(k, -w / 2 + 1, w / 2 - 4, -d / 2 + 3.5f, d / 2 - 0.6f, 4.2f, 2.4f, shingle, T("roof_shingle", new Color(0.28f, 0.3f, 0.34f)), season);
            WindowRow(k, f[South], 1, f[South].Width - 1, 1.2f, 1.2f, 1.4f, 2.4f, ref s, closed);
            Sign(k, f[South], f[South].Width / 2, 6f, 3.3f, 0.7f, Row(BuildingType.SeafoodRestaurant), closed);
            var white = C(H("#f4f4f2"), 0.4f); var red = C(H("#c62828"), 0.4f);
            float tx = w / 2 - 2.2f, tz = -d / 2 + 4;
            for (int i = 0; i < 4; i++) k.Cylinder(new Vector3(tx, 0.06f + i * 2f, tz), 1.3f - i * 0.12f, 1.18f - i * 0.12f, 2f, 10, i % 2 == 0 ? white : red, null);
            k.Cylinder(new Vector3(tx, 8.06f, tz), 0.85f, 0.85f, 1.0f, 10, closed ? C(H("#555555")) : Lit(H("#ffe082"), 3f), null);
            k.Cylinder(new Vector3(tx, 9.06f, tz), 1.0f, 0.1f, 0.9f, 10, red, red);
            var rope = C(H("#d8c8a0"), 0.2f); var pile = T("wood", new Color(0.5f, 0.4f, 0.3f));
            for (int i = 0; i < 5; i++) k.Cylinder(new Vector3(-w / 2 + 1.5f + i * 3f, 0.06f, -d / 2 + 1), 0.18f, 0.18f, 1.1f, 6, pile, pile);
            k.Box(new Vector3(-w / 2 + 1.5f, 0.85f, -d / 2 + 0.96f), new Vector3(-w / 2 + 13.5f, 0.92f, -d / 2 + 1.04f), rope, rope);
        }

        static void CurryHouse(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var wall = T("wall_stucco", new Color(1f, 0.82f, 0.56f));
            var f = Storefront(k, -w / 2 + 0.5f, w / 2 - 0.5f, -d / 2 + 2.6f, d / 2 - 0.5f, 5.2f, wall, "gf_store_wood", closed, season, Row(BuildingType.CurryHouse), C(H("#e65100")), 0.25f, 0.75f, 4.6f, 3.4f);
            // pointed arches, a small onion dome on the roof
            var orange = C(H("#e65100"), 0.4f);
            foreach (float a in new[] { 0.5f, f.Width - 1.9f })
            {
                Rect(k, f, a, a + 1.4f, 1.0f, 3.0f, MaterialLibrary.Pane(closed ? new Color32(26, 30, 38, 255) : new Color32(255, 196, 120, 255)), Vector2.zero, Vector2.one, 0.04f);
                k.Cylinder(f.At(a + 0.7f, 3.0f) + Vector3.back * 0.05f, 0.7f, 0f, 0.8f, 6, orange, orange);
            }
            var gold = C(H("#d4a24c"), 0.6f, 0.6f);
            k.Cylinder(new Vector3(0, 5.2f, 1), 1.2f, 1.2f, 0.8f, 12, wall, null);
            k.Cylinder(new Vector3(0, 6.0f, 1), 1.2f, 1.5f, 0.6f, 12, gold, null);
            k.Cylinder(new Vector3(0, 6.6f, 1), 1.5f, 0.6f, 1.0f, 12, gold, null);
            k.Cylinder(new Vector3(0, 7.6f, 1), 0.6f, 0f, 0.9f, 12, gold, null);
        }

        static void ThaiRestaurant(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var wall = T("wall_stucco", new Color(0.96f, 0.92f, 0.84f));
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 3, z1 = d / 2 - 0.6f;
            var faces = Faces(x0, x1, z0, z1);
            foreach (var fc in faces) Plain(k, fc, 0, 3.8f, wall, 2.5f);
            var f = faces[South];
            Rect(k, f, 1, f.Width - 1, 0.3f, 3.0f, Glass(closed), Vector2.zero, new Vector2(0.75f, 1), 0.03f);
            // a tiered temple roof: two stacked gables in purple with gold trim
            var roof = Snowy(T("roof_shingle", new Color(0.42f, 0.2f, 0.55f)), season);
            var gold = C(H("#d4a24c"), 0.6f, 0.6f);
            k.Push(new Vector3(0, 0, (z0 + z1) / 2), 90);
            k.Gable(-(z1 - z0) / 2, -(x1 - x0) / 2, (z1 - z0) / 2, (x1 - x0) / 2, 3.8f, 1.6f, roof, gold, 0.7f);
            k.Gable(-(z1 - z0) / 2 + 0.8f, -(x1 - x0) / 2 + 1.6f, (z1 - z0) / 2 - 0.8f, (x1 - x0) / 2 - 1.6f, 4.8f, 1.9f, roof, gold, 0.5f);
            k.Pop();
            Sign(k, f, f.Width / 2, 5f, 3.25f, 0.5f, Row(BuildingType.ThaiRestaurant), closed);
            Planter(k, -w / 2 + 1, -d / 2 + 1.6f, season); Planter(k, w / 2 - 1, -d / 2 + 1.6f, season);
        }

        static void TeaHouse(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 3f);
            // a low timber house with paper screens and a deep hipped roof; stepping stones and a maple
            var wood = T("wood", new Color(0.55f, 0.4f, 0.28f));
            float x0 = -w / 2 + 1, x1 = w / 2 - 1, z0 = -d / 2 + 3.4f, z1 = d / 2 - 1;
            var faces = Faces(x0, x1, z0, z1);
            foreach (var fc in faces) Plain(k, fc, 0, 3.2f, T("wall_stucco", new Color(0.95f, 0.93f, 0.86f)), 2f);
            var f = faces[South];
            var screen = closed ? C(H("#b8b2a6")) : MaterialLibrary.Glow(new Color(0.95f, 0.92f, 0.84f), new Color(1f, 0.85f, 0.6f), 1.2f);
            for (float a = 0.4f; a < f.Width - 0.6f; a += 1.6f)
            {
                Rect(k, f, a, a + 1.4f, 0.3f, 2.6f, screen, Vector2.zero, Vector2.one, 0.03f);
                Ledge(k, f, a - 0.06f, a + 0.04f, 0.2f, 2.7f, 0.06f, wood);
            }
            k.Hip(x0, z0, x1, z1, 3.2f, 1.8f, Snowy(T("roof_shingle", new Color(0.3f, 0.32f, 0.34f)), season), 1.0f);
            var face = new Face { LeftBottom = new Vector3(-2.2f, 0, -d / 2 + 0.8f), Right = Vector3.right, Normal = Vector3.back, Width = 4.4f };
            var stone = C(H("#9e9a92"), 0.2f);
            for (int i = 0; i < 4; i++) k.Cylinder(new Vector3(0.6f * (i % 2 == 0 ? -1 : 1), 0.06f, -d / 2 + 0.6f + i * 0.7f), 0.3f, 0.3f, 0.06f, 7, stone, stone);
            Pylon(k, -2.6f, -d / 2 + 1.2f, 1.2f, 3.2f, Row(BuildingType.TeaHouse), closed, wood);
            var trunk = C(H("#6b4f3a"));
            k.Box(new Vector3(w / 2 - 1.9f, 0.06f, -d / 2 + 1.4f), new Vector3(w / 2 - 1.7f, 1.6f, -d / 2 + 1.6f), trunk, trunk);
            k.Blob(new Vector3(w / 2 - 1.8f, 2.2f, -d / 2 + 1.5f), new Vector3(1.0f, 0.7f, 1.0f), season == 0 ? H("#8a7a66") : season == 3 ? H("#c0392b") : H("#5e8c3a"));
        }

        static void JuiceBar(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.98f, 0.98f, 0.95f)), "gf_store_light", closed, season, Row(BuildingType.JuiceBar), C(H("#76ff03")));
            Ledge(k, f, 0, f.Width, 0, 0.6f, 0.05f, C(H("#76ff03"), 0.4f));
            // fruit on the parapet: an orange, a lemon, a watermelon slice
            k.Blob(new Vector3(-2.2f, 5.5f, -d / 2 + 3), new Vector3(0.8f, 0.8f, 0.8f), H("#ff9800"));
            k.Blob(new Vector3(0, 5.3f, -d / 2 + 3), new Vector3(0.7f, 0.55f, 0.55f), H("#ffeb3b"));
            k.Cylinder(new Vector3(2.2f, 4.7f, -d / 2 + 3), 0.9f, 0f, 1.6f, 3, C(H("#e53935"), 0.4f), C(H("#43a047"), 0.4f));
            for (int i = 0; i < 3; i++) k.Cylinder(f.At(1.5f + i * 3f, 0.06f) + Vector3.back * 1.4f, 0.25f, 0.25f, 0.75f, 8, C(H("#76ff03"), 0.4f), C(H("#76ff03"), 0.4f));
        }

        static void DonutShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var f = Storefront(k, -w / 2 + 0.5f, w / 2 - 0.5f, -d / 2 + 3, d / 2 - 0.5f, 4.2f, T("wall_stucco", new Color(1f, 0.85f, 0.9f)), "gf_store_light", closed, season, Row(BuildingType.DonutShop), C(H("#6d4c41")));
            // a giant donut standing on the roof: a ring of pink-glazed segments
            var dough = C(H("#d9a85a"), 0.3f); var glaze = C(H("#f48fb1"), 0.6f);
            float cy = 6.8f, R0 = 2.0f, r0 = 0.65f;
            for (int i = 0; i < 16; i++)
            {
                float a0 = i * Mathf.PI * 2 / 16, a1 = (i + 1) * Mathf.PI * 2 / 16;
                var p0 = new Vector3(Mathf.Cos(a0) * R0, cy + Mathf.Sin(a0) * R0, 1.2f);
                var p1 = new Vector3(Mathf.Cos(a1) * R0, cy + Mathf.Sin(a1) * R0, 1.2f);
                var mid = (p0 + p1) / 2;
                k.Box(mid - new Vector3(r0, r0, r0) * 0.9f, mid + new Vector3(r0, r0, r0) * 0.9f, i < 8 ? glaze : dough, i < 8 ? glaze : dough);
            }
            var post = C(H("#3a3e44"), 0.4f, 0.4f);
            k.Box(new Vector3(-0.15f, 4.2f, 1.05f), new Vector3(0.15f, 4.9f, 1.35f), post, post);
        }

        static void IceCream(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.88f, 0.96f, 1f)), "gf_store_light", closed, season, Row(BuildingType.IceCreamParlor), C(H("#f48fb1")));
            Awning(k, f, 0.3f, f.Width - 0.3f, 3.85f, 1.3f, H("#f48fb1"));
            // a giant cone on the roof
            var cone = C(H("#d9a85a"), 0.3f);
            k.Cylinder(new Vector3(0, 4.6f, -d / 2 + 4), 0.1f, 1.1f, 3f, 10, cone, null);
            k.Blob(new Vector3(0, 8.1f, -d / 2 + 4), new Vector3(1.25f, 0.9f, 1.25f), H("#f8bbd0"));
            k.Blob(new Vector3(0, 9.1f, -d / 2 + 4), new Vector3(0.9f, 0.7f, 0.9f), H("#fff3e0"));
            Patio(k, f, 0.5f, f.Width - 0.5f, 1.8f, H("#4fc3f7"));
        }

        static void CandyShop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 5f, T("wall_stucco", new Color(1f, 0.97f, 0.97f)), "gf_store_wood", closed, season, Row(BuildingType.CandyShop), C(H("#d81b60")));
            // candy-striped corners and lollipops by the door
            for (int i = 0; i < 6; i++)
            {
                Ledge(k, f, 0, 0.5f, i * 0.85f, i * 0.85f + 0.42f, 0.1f, C(H("#d81b60"), 0.4f));
                Ledge(k, f, f.Width - 0.5f, f.Width, i * 0.85f, i * 0.85f + 0.42f, 0.1f, C(H("#d81b60"), 0.4f));
            }
            Color32[] pops = { H("#e91e63"), H("#00bcd4"), H("#ffeb3b") };
            for (int i = 0; i < 3; i++)
            {
                var p = f.At(1.4f + i * 0.9f, 0.06f) + Vector3.back * 1.0f;
                k.Box(p + new Vector3(-0.04f, 0, -0.04f), p + new Vector3(0.04f, 1.6f, 0.04f), C(H("#f4f4f2")), C(H("#f4f4f2")));
                k.Cylinder(p + new Vector3(0, 1.6f, -0.05f), 0.4f, 0.4f, 0.1f, 12, C(pops[i], 0.6f), C(pops[i], 0.6f));
            }
        }

        static void BakeryCafe(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            // a timber-framed cottage front with a steep roof and tables out on the pavement
            var plaster = T("wall_stucco", new Color(0.98f, 0.94f, 0.86f));
            var f = GableBox(k, -w / 2 + 0.5f, w / 2 - 0.5f, -d / 2 + 3, d / 2 - 0.5f, 4.4f, 3f, plaster, T("roof_shingle", new Color(0.58f, 0.32f, 0.24f)), season)[South];
            var beam = T("wood", new Color(0.36f, 0.24f, 0.16f));
            foreach (float a in new[] { 0.2f, f.Width / 3, 2 * f.Width / 3, f.Width - 0.4f }) Ledge(k, f, a, a + 0.2f, 0, 4.4f, 0.06f, beam);
            Ledge(k, f, 0, f.Width, 3.4f, 3.6f, 0.06f, beam);
            Rect(k, f, 0.6f, f.Width / 3 - 0.2f, 0.6f, 3.0f, Glass(closed), Vector2.zero, new Vector2(0.5f, 1), 0.03f);
            Rect(k, f, 2 * f.Width / 3 + 0.4f, f.Width - 0.6f, 0.6f, 3.0f, Glass(closed), Vector2.zero, new Vector2(0.5f, 1), 0.03f);
            Door(k, f, f.Width / 2, H("#4e342e"), 1.2f, 2.5f);
            Sign(k, f, f.Width / 2, 4f, 3.75f, 0.5f, Row(BuildingType.BakeryCafe), closed);
            Patio(k, f, 0.5f, f.Width - 0.5f, 1.9f, H("#4e342e"));
        }

        static void SportsBar(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3, d / 2 - 0.6f, 5.2f, S("wall_brick_grey"), "gf_store_dark", closed, season, Row(BuildingType.SportsBar), C(H("#0d1b2a")), 0.1f, 0.9f, 7f, 3.4f);
            // big screens glowing through the windows, neon beer signs, team pennants
            if (!closed)
                for (float a = 2; a < f.Width - 1.5f; a += 4.5f) Ledge(k, f, a, a + 2.2f, 1.6f, 2.9f, 0.05f, Lit(H("#4fc3f7"), 1.6f));
            Neon(k, f, 0.4f, 2.4f, 2.0f, H("#ffd740"), closed);
            Neon(k, f, f.Width - 2.4f, f.Width - 0.4f, 2.0f, H("#ff5252"), closed);
            Color32[] team = { H("#c62828"), H("#1565c0"), H("#f9a825"), H("#2e7d32") };
            for (int i = 0; i < 8; i++)
            {
                var p = f.At(0.6f + i * (f.Width - 1.2f) / 7, 4.9f) + Vector3.back * 0.1f;
                k.Tri(p + new Vector3(-0.35f, 0, -0.02f), p + new Vector3(0.35f, 0, -0.02f), p + new Vector3(0, -0.8f, -0.02f), C(team[i % 4], 0.3f));
            }
            k.Box(new Vector3(w / 2 - 3, 5.2f, 2), new Vector3(w / 2 - 1.5f, 5.3f, 3), C(H("#d7dde2"), 0.6f), C(H("#d7dde2"), 0.6f));
            k.Cylinder(new Vector3(w / 2 - 2.2f, 5.3f, 2.5f), 0.9f, 0.9f, 0.05f, 12, C(H("#e8e8e4"), 0.5f), C(H("#e8e8e4"), 0.5f));
        }

        static void WineBar(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_brick", "gf_store_dark", 2, ref s, closed, season, out top);
            var wine = C(H("#3b0a1a"), 0.4f);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, wine);
            Sign(k, Out(f, 0.3f), f.Width / 2, 5f, 3.62f, 0.55f, Row(BuildingType.WineBar), closed);
            // ivy up the side and fairy lights over a couple of barrel tables
            var ivy = C(season == 0 ? H("#6d6250") : season == 3 ? H("#a0442a") : H("#2e6b2f"), 0.2f);
            Ledge(k, f, 0, 1.2f, 4.3f, top - 0.6f, 0.15f, ivy);
            var barrel = T("wood", new Color(0.55f, 0.36f, 0.22f));
            for (int i = 0; i < 2; i++)
            {
                var p = f.At(2.5f + i * 4.5f, 0.06f) + Vector3.back * 1.4f;
                k.Cylinder(p, 0.45f, 0.45f, 1.0f, 10, barrel, barrel);
            }
            for (int i = 0; i < 10; i++) k.Cylinder(f.At(0.6f + i * (f.Width - 1.2f) / 9, 3.3f - Mathf.Sin(i / 9f * Mathf.PI) * 0.3f) + Vector3.back * 1.4f, 0.06f, 0.06f, 0.1f, 6, closed ? C(H("#555555")) : Lit(H("#fff3c4"), 3f), null);
        }

        static void Brewpub(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var brick = S("wall_brick_brown");
            var f = Storefront(k, -w / 2 + 0.6f, 4, -d / 2 + 3.5f, d / 2 - 0.6f, 6f, brick, "gf_store_dark", closed, season, Row(BuildingType.Brewpub), C(H("#ffca28")), 0.08f, 0.92f, 8f, 4.0f);
            // copper brewing tanks at the back of a beer garden of picnic tables
            k.Flat(4.5f, -d / 2 + 1, w / 2 - 0.6f, d / 2 - 1, 0.07f, season == 0 ? S("snow") : S("gravel"), 3f);
            var copper = C(H("#b87333"), 0.7f, 0.9f);
            foreach (float x in new[] { 5.6f, 7.8f })
            {
                k.Cylinder(new Vector3(x, 0.07f, d / 2 - 1.6f), 0.9f, 0.9f, 3.2f, 10, copper, null);
                k.Cylinder(new Vector3(x, 3.27f, d / 2 - 1.6f), 0.9f, 0.2f, 0.6f, 10, copper, copper);
            }
            var wood = T("wood", new Color(0.7f, 0.56f, 0.4f));
            for (int i = 0; i < 3; i++)
            {
                float x = 5.2f, z = -d / 2 + 1.6f + i * 2.4f;
                k.Box(new Vector3(x, 0.7f, z - 0.4f), new Vector3(x + 3.2f, 0.8f, z + 0.4f), wood, wood);
                k.Box(new Vector3(x, 0.4f, z - 0.9f), new Vector3(x + 3.2f, 0.48f, z - 0.6f), wood, wood);
                k.Box(new Vector3(x, 0.4f, z + 0.6f), new Vector3(x + 3.2f, 0.48f, z + 0.9f), wood, wood);
                k.Box(new Vector3(x + 1.55f, 0.06f, z - 0.1f), new Vector3(x + 1.65f, 0.7f, z + 0.1f), wood, wood);
            }
            Tree(k, w / 2 - 0.9f, -d / 2 + 1.0f, 0.7f, season, s);
        }

        static void Lounge(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            // art deco: black lacquer, gold fins, a vertical neon blade
            var black = T("wall_stucco", new Color(0.1f, 0.1f, 0.12f)); var gold = C(H("#d4af37"), 0.7f, 0.9f);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 7f, black, "gf_store_dark", closed, season, Row(BuildingType.CocktailLounge), gold, 0.3f, 0.7f, 4f, 3.2f);
            for (int i = 0; i < 5; i++) Ledge(k, f, f.Width / 2 - 2 + i, f.Width / 2 - 1.85f + i, 4.6f, 7.4f, 0.25f, gold);
            var blade = f.At(0.4f, 3.8f) + Vector3.back * 0.6f;
            k.Box(blade + new Vector3(-0.1f, 0, -0.45f), blade + new Vector3(0.1f, 3.6f, 0.45f), black, gold);
            k.Box(blade + new Vector3(-0.12f, 0.2f, -0.35f), blade + new Vector3(0.12f, 3.4f, -0.25f), closed ? C(H("#555555")) : Lit(H("#ce93d8"), 3.5f), null);
            k.Box(blade + new Vector3(-0.12f, 0.2f, 0.25f), blade + new Vector3(0.12f, 3.4f, 0.35f), closed ? C(H("#555555")) : Lit(H("#ce93d8"), 3.5f), null);
            Canopy(k, f, f.Width / 2 - 1.4f, f.Width / 2 + 1.4f, 3.0f, 1.6f, gold, false);
        }

        static void Nightclub(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            // a windowless black box banded in neon, searchlights on the roof, a velvet rope by the door
            var black = T("metal_panel", new Color(0.12f, 0.12f, 0.14f));
            var f = FlatBox(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3.5f, d / 2 - 0.6f, 7f, black, C(H("#00e5ff")), season)[South];
            Neon(k, f, 0, f.Width, 6.2f, H("#00e5ff"), closed, 0.2f);
            Neon(k, f, 0, f.Width, 0.4f, H("#ff00e5"), closed, 0.15f);
            for (int i = 0; i < 6; i++) Neon(k, f, 0.8f + i * 3f, 2.2f + i * 3f, 1.4f + (i % 3) * 1.2f, i % 2 == 0 ? H("#ff00e5") : H("#00e5ff"), closed, 0.1f);
            Sign(k, f, f.Width / 2, 5f, 4.6f, 0.9f, Row(BuildingType.Nightclub), closed);
            Door(k, f, f.Width / 2, H("#1b1b1b"), 1.6f, 2.6f);
            var gold = C(H("#d4af37"), 0.7f, 0.9f); var velvet = C(H("#8e1b1b"), 0.4f);
            for (int i = 0; i < 4; i++)
            {
                var p = f.At(f.Width / 2 + 1.4f + i * 1.2f, 0.06f) + Vector3.back * 1.0f;
                k.Cylinder(p, 0.08f, 0.08f, 1.0f, 6, gold, gold);
                if (i < 3) k.Box(p + new Vector3(0, 0.75f, -0.03f), p + new Vector3(1.2f, 0.85f, 0.03f), velvet, velvet);
            }
            if (!closed)
                foreach (float x in new[] { -w / 2 + 3, w / 2 - 3 })
                {
                    var steel = C(H("#5d6670"), 0.5f, 0.6f);
                    k.Box(new Vector3(x - 0.5f, 7f, 2), new Vector3(x + 0.5f, 7.6f, 3), steel, steel);
                    k.Cylinder(new Vector3(x, 7.6f, 2.5f), 0.35f, 0.9f, 12f, 8, MaterialLibrary.Glow(new Color(0.9f, 0.9f, 1f), new Color(0.6f, 0.8f, 1f), 1.2f), null);
                }
        }

        static void Karaoke(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_grey", "gf_store_dark", 2, ref s, closed, season, out top);
            var purple = C(H("#1a0033"), 0.4f);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, purple);
            Sign(k, Out(f, 0.3f), f.Width / 2, 5f, 3.62f, 0.55f, Row(BuildingType.KaraokeBar), closed);
            Neon(k, Out(f, 0.3f), 0.2f, f.Width - 0.2f, 3.42f, H("#ff4081"), closed, 0.06f);
            // a neon star and a microphone up on the roof
            var star = closed ? C(H("#555555")) : Lit(H("#ffd740"), 3f);
            var c = new Vector3(-2, top + 1.6f, -d / 2 + 2.8f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2 / 5 + Mathf.PI / 2, b = a + Mathf.PI / 5;
                Two(k, c, c + Dir(a) * 1.2f, c + Dir(b) * 0.5f, star);
                Two(k, c, c + Dir(b) * 0.5f, c + Dir(a + Mathf.PI * 2 / 5) * 1.2f, star);
            }
            var iron = C(H("#3a3e44"), 0.4f, 0.4f);
            k.Box(new Vector3(c.x - 0.06f, top, c.z + 0.02f), new Vector3(c.x + 0.06f, c.y, c.z + 0.14f), iron, iron);
            var mic = C(H("#2b2b2b"), 0.6f, 0.6f);
            k.Cylinder(new Vector3(2.4f, top, -d / 2 + 3.2f), 0.15f, 0.25f, 1.6f, 8, mic, null);
            k.Blob(new Vector3(2.4f, top + 1.9f, -d / 2 + 3.2f), new Vector3(0.4f, 0.4f, 0.4f), H("#b0b8c0"));
        }
    }
}
