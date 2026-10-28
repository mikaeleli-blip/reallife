using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>Offices, places to stay and the last few services added later, each its own model.</summary>
    public static partial class BuildingArt
    {
        static void ExtraOffices(MeshKit k, BuildingType t, float w, float d, ref uint s, bool closed, int season, int floors)
        {
            switch (t)
            {
                case BuildingType.LawFirm: LawFirm(k, w, d, ref s, closed, season); break;
                case BuildingType.AccountingFirm: Accountants(k, w, d, ref s, closed, season); break;
                case BuildingType.InsuranceAgency: Insurance(k, w, d, ref s, closed, season); break;
                case BuildingType.RealEstateAgency: Realtors(k, w, d, ref s, closed, season); break;
                case BuildingType.ArchitectureFirm: Architects(k, w, d, ref s, closed, season); break;
                case BuildingType.MarketingAgency: Marketing(k, w, d, ref s, closed, season); break;
                case BuildingType.SoftwareStudio: Software(k, w, d, ref s, closed, season); break;
                case BuildingType.CallCenter: CallCenter(k, w, d, ref s, closed, season); break;
                case BuildingType.EngineeringFirm: Engineers(k, w, d, ref s, closed, season); break;
                case BuildingType.DesignStudio: DesignStudio(k, w, d, ref s, closed, season); break;
                case BuildingType.Motel: Motel(k, w, d, ref s, closed, season); break;
                case BuildingType.Hotel: Hotel(k, w, d, ref s, closed, season); break;
                case BuildingType.BedAndBreakfast: BedAndBreakfast(k, w, d, ref s, closed, season); break;
                case BuildingType.FuneralHome: FuneralHome(k, w, d, ref s, closed, season); break;
                case BuildingType.TaxiCompany: Taxi(k, w, d, ref s, closed, season); break;
                case BuildingType.ValleyValueOffice:
                case BuildingType.RedwoodOffice:
                case BuildingType.BulkworksOffice: ChainOffice(k, w, d, ref s, closed, season, t); break;
                default:
                    // anything still without a model of its own: a plain shop with its sign
                    Sidewalk(k, w, d);
                    Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 4.6f, S("wall_stucco"), "gf_store_light", closed, season, Row(t));
                    break;
            }
        }

        static void Hedge(MeshKit k, float x0, float z0, float x1, float z1, int season)
        {
            k.Box(new Vector3(x0, 0.06f, z0), new Vector3(x1, 0.9f, z1), Paint(season == 0 ? "#d8dde0" : "#2f6b35", 0.1f), Paint(season == 0 ? "#eef1f3" : "#3a7d3f", 0.1f));
        }

        // ================================================================== offices
        static void LawFirm(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 3.4f, z1 = d / 2 - 0.6f;
            var stone = T("wall_stone", new Color(0.88f, 0.85f, 0.78f));
            var f = FlatBox(k, x0, x1, z0, z1, 7.6f, stone, Paint("#e8e0cc", 0.3f), season, 0.7f)[South];
            WindowRow(k, f, 0.4f, 2.9f, 1.0f, 1.0f, 2.0f, 1.5f, ref s, closed);
            WindowRow(k, f, f.Width - 2.9f, f.Width - 0.4f, 1.0f, 1.0f, 2.0f, 1.5f, ref s, closed);
            WindowRow(k, f, 0.4f, f.Width - 0.4f, 4.6f, 1.0f, 2.0f, 1.5f, ref s, closed);
            Door(k, f, f.Width / 2, H("#2b1d14"), 1.4f, 2.8f);
            // a little portico: steps, two columns, the firm's name on the lintel and a pediment
            var white = Paint("#f2ede2", 0.35f);
            for (int i = 0; i < 2; i++) k.Box(new Vector3(-1.8f + i * 0.2f, 0.05f, z0 - 2.2f + i * 0.3f), new Vector3(1.8f - i * 0.2f, 0.2f + i * 0.15f, z0), white, white);
            foreach (float x in new[] { -1.3f, 1.3f }) k.Cylinder(new Vector3(x, 0.35f, z0 - 1.5f), 0.22f, 0.19f, 3.55f, 10, white, white);
            k.Box(new Vector3(-1.7f, 3.9f, z0 - 1.9f), new Vector3(1.7f, 4.5f, z0), white, white);
            var lintel = new Face { LeftBottom = new Vector3(-1.7f, 0, z0 - 1.91f), Right = Vector3.right, Normal = Vector3.back, Width = 3.4f };
            Sign(k, lintel, 1.7f, 3.0f, 4.0f, 0.38f, Row(BuildingType.LawFirm), closed);
            k.Push(new Vector3(0, 0, z0 - 0.95f), 90);
            k.Gable(-0.95f, -1.8f, 0.95f, 1.8f, 4.5f, 0.8f, Snowy(T("roof_metal", new Color(0.4f, 0.45f, 0.44f)), season), white, 0.1f);
            k.Pop();
            Hedge(k, x0, -d / 2 + 1.0f, -2.2f, -d / 2 + 1.8f, season);
            Hedge(k, 2.2f, -d / 2 + 1.0f, x1, -d / 2 + 1.8f, season);
        }

        static void Accountants(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var skin = new Skin { Upper = "fac_civic_brick", Floor = 3.15f, Ground = "gf_lobby", GroundH = 4.2f, Wall = "wall_brick_red", Coping = H("#e2dccd"), Glow = 1.3f };
            float x0 = -w / 2 + 0.4f, x1 = w / 2 - 0.4f, z0 = -d / 2 + 2.4f, z1 = d / 2 - 0.4f;
            float top = Block(k, x0, x1, z0, z1, skin, 1, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var f = Faces(x0, x1, z0, z1)[South];
            PlainAwning(k, f, f.Width / 2 - 1.3f, f.Width / 2 + 1.3f, 3.2f, 1.0f, H("#1b5e20"));
            Sign(k, f, f.Width / 2, 4.6f, 3.45f, 0.55f, Row(BuildingType.AccountingFirm), closed);
            // a clock tower on the roof
            var brick = S("wall_brick_red"); var trim = Paint("#e2dccd", 0.3f);
            float cz = z0 + 0.3f;
            k.Box(new Vector3(-0.9f, top, cz), new Vector3(0.9f, top + 2.2f, cz + 1.4f), brick, trim);
            k.Push(new Vector3(0, 0, cz + 0.7f), 90);
            k.Gable(-0.8f, -1f, 0.8f, 1f, top + 2.2f, 0.7f, Snowy(T("roof_metal", new Color(0.3f, 0.45f, 0.4f)), season), brick, 0.1f);
            k.Pop();
            var c = new Vector3(0, top + 1.15f, cz - 0.02f);
            Disc(k, c, 0.7f, trim, 20);
            Disc(k, c + Vector3.back * 0.01f, 0.6f, closed ? Paint("#f4f4f2") : Lit(H("#fff8e1"), 1.2f), 20);
            var hand = Paint("#111111");
            Bar(k, c + Vector3.back * 0.03f, c + Vector3.back * 0.03f + new Vector3(0, 0.45f, 0), 0.06f, hand);
            Bar(k, c + Vector3.back * 0.03f, c + Vector3.back * 0.03f + new Vector3(0.3f, -0.1f, 0), 0.07f, hand);
        }

        static void Insurance(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.9f, 0.92f, 0.95f)), "gf_store_light", closed, season, Row(BuildingType.InsuranceAgency), Paint("#1565c0"));
            Ledge(k, f, 0, f.Width, 0, 0.5f, 0.05f, Paint("#1565c0"));
            // a big red umbrella over the pavement
            float ux = w / 2 - 1.5f, uz = -d / 2 + 1.3f;
            var pole = Paint("#3a3e44", 0.4f, 0.4f); var red = Paint("#d32f2f", 0.4f);
            k.Box(new Vector3(ux - 0.05f, 1.1f, uz - 0.05f), new Vector3(ux + 0.05f, 3.9f, uz + 0.05f), pole, pole);
            k.Cylinder(new Vector3(ux, 3.3f, uz), 1.5f, 0.05f, 0.8f, 8, red, red);
            k.Cylinder(new Vector3(ux, 4.1f, uz), 0.05f, 0.02f, 0.3f, 6, pole, pole);
            Bar(k, new Vector3(ux, 1.15f, uz), new Vector3(ux - 0.2f, 0.95f, uz), 0.08f, pole);
            Bar(k, new Vector3(ux - 0.2f, 0.95f, uz), new Vector3(ux - 0.4f, 1.15f, uz), 0.08f, pole);
            k.Box(new Vector3(ux - 0.15f, 0.06f, uz - 0.15f), new Vector3(ux + 0.15f, 0.1f, uz + 0.15f), pole, pole);
            k.Box(new Vector3(ux - 0.04f, 0.06f, uz + 0.2f), new Vector3(ux + 0.04f, 1.0f, uz + 0.28f), pole, pole);
        }

        static void Realtors(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.98f, 0.97f, 0.94f)), "gf_store_light", closed, season, Row(BuildingType.RealEstateAgency), Paint("#b71c1c"));
            // listings in the window: cards with little houses on them
            var card = Paint("#fafafa", 0.3f);
            string[] roofs = { "#c62828", "#1565c0", "#2e7d32", "#6d4c41" };
            int i = 0;
            for (int r = 0; r < 2; r++)
                for (float a = 1.0f; a < f.Width - 1.2f; a += 1.1f, i++)
                {
                    if (Mathf.Abs(a + 0.4f - f.Width / 2) < 0.9f) continue;
                    float y = 1.4f + r * 1.0f;
                    Rect(k, f, a, a + 0.8f, y, y + 0.8f, card, Vector2.zero, Vector2.one, 0.05f);
                    var house = f.At(a + 0.4f, y + 0.25f) + f.Normal * 0.07f;
                    var col = Paint(roofs[i % 4]);
                    k.Box(house + new Vector3(-0.2f, 0, -0.01f), house + new Vector3(0.2f, 0.25f, 0.01f), Paint("#e0c9a6"), Paint("#e0c9a6"));
                    Two(k, house + new Vector3(-0.26f, 0.25f, -0.02f), house + new Vector3(0.26f, 0.25f, -0.02f), house + new Vector3(0, 0.45f, -0.02f), col);
                }
            // a For Sale sign by the kerb
            var post = Paint("#fafafa", 0.4f); var red = Paint("#c62828");
            float sx = -w / 2 + 1.0f, sz = -d / 2 + 0.8f;
            k.Box(new Vector3(sx - 0.05f, 0.02f, sz - 0.05f), new Vector3(sx + 0.05f, 2.0f, sz + 0.05f), post, post);
            k.Box(new Vector3(sx - 0.05f, 1.9f, sz - 0.05f), new Vector3(sx + 1.1f, 2.0f, sz + 0.05f), post, post);
            k.Box(new Vector3(sx + 0.2f, 1.1f, sz - 0.03f), new Vector3(sx + 1.0f, 1.85f, sz + 0.03f), post, post);
            k.Box(new Vector3(sx + 0.2f, 1.6f, sz - 0.04f), new Vector3(sx + 1.0f, 1.85f, sz + 0.04f), red, red);
            // a house shape on the parapet
            var hz = z0 + 0.3f;
            k.Box(new Vector3(-0.9f, 5.1f, hz), new Vector3(0.9f, 6.2f, hz + 1.0f), Paint("#fafafa"), Paint("#fafafa"));
            k.Gable(-0.9f, hz, 0.9f, hz + 1.0f, 6.2f, 0.7f, Snowy(Paint("#c62828"), season), Paint("#fafafa"), 0.15f);
            k.Box(new Vector3(-0.2f, 5.1f, hz - 0.02f), new Vector3(0.2f, 5.7f, hz), Paint("#5d4037"), Paint("#5d4037"));
        }

        static void Architects(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            // a glass box with a concrete box cantilevered over it
            float gx0 = -3.4f, gx1 = 3.6f, gz0 = -1f, gz1 = d / 2 - 0.6f;
            var gf = Faces(gx0, gx1, gz0, gz1);
            foreach (var fc in gf) Rect(k, fc, 0, fc.Width, 0, 3.4f, Glass(closed), Vector2.zero, new Vector2(Mathf.Max(1, fc.Width / 2.5f) * 0.25f, 1));
            var conc = T("wall_stucco", new Color(0.72f, 0.72f, 0.7f));
            float ux0 = -4.6f, ux1 = 2.6f, uz0 = -3.6f, uz1 = d / 2 - 0.6f;
            k.Box(new Vector3(ux0, 3.4f, uz0), new Vector3(ux1, 6.8f, uz1), conc, Snowy(S("roof_membrane"), season), 2f, true);
            var uf = new Face { LeftBottom = new Vector3(ux0, 0, uz0), Right = Vector3.right, Normal = Vector3.back, Width = ux1 - ux0 };
            Rect(k, uf, 0.4f, uf.Width - 0.4f, 4.1f, 5.6f, Glass(closed), Vector2.zero, new Vector2(1.25f, 0.4f), 0.02f);
            Sign(k, uf, 1.9f, 3f, 5.95f, 0.38f, Row(BuildingType.ArchitectureFirm), closed);
            var steel = Paint("#3a3e44", 0.5f, 0.6f);
            k.Cylinder(new Vector3(ux0 + 0.3f, 0.06f, uz0 + 0.3f), 0.12f, 0.12f, 3.34f, 8, steel, steel);
            Door(k, gf[South], gf[South].Width / 2, H("#3a3e44"), 1.2f, 2.6f);
            Tree(k, w / 2 - 1.3f, -d / 2 + 1.6f, 0.9f, season, s);
            k.Flat(gx1 + 0.4f, -1f, w / 2 - 0.5f, d / 2 - 0.6f, 0.07f, S("gravel"), 3f);
        }

        static void Marketing(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            var skin = new Skin { Upper = "fac_glass_green", Floor = 3.6f, Bay = 3f, Ground = "gf_lobby", GroundH = 4.2f, Wall = "wall_stucco", Coping = H("#212121"), Glow = 1.4f };
            float x0 = -w / 2 + 0.4f, x1 = w / 2 - 0.4f, z0 = -d / 2 + 2.4f, z1 = d / 2 - 0.4f;
            float top = Block(k, x0, x1, z0, z1, skin, 2, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var f = Faces(x0, x1, z0, z1)[South];
            Ledge(k, f, 0, f.Width, 3.7f, 4.2f, 0.2f, Paint("#212121"));
            Sign(k, Out(f, 0.2f), f.Width / 2, 4f, 3.72f, 0.45f, Row(BuildingType.MarketingAgency), closed);
            // a billboard on the roof, bright blocks of colour
            float bz = d / 2 - 2.2f;
            var steel = Paint("#5d6670", 0.5f, 0.6f);
            foreach (float x in new[] { -2.8f, 2.8f }) k.Box(new Vector3(x - 0.1f, top, bz - 0.1f), new Vector3(x + 0.1f, top + 1.6f, bz + 0.1f), steel, steel);
            k.Box(new Vector3(-4.2f, top + 1.6f, bz - 0.15f), new Vector3(4.2f, top + 4.4f, bz + 0.15f), steel, steel);
            string[] cols = { "#ff1744", "#ffea00", "#00b0ff", "#00e676" };
            for (int i = 0; i < 4; i++)
            {
                var m = closed ? Paint(cols[i]) : Lit(H(cols[i]), 1.3f);
                k.Box(new Vector3(-4.0f + i * 2f, top + 1.8f, bz - 0.2f), new Vector3(-2.05f + i * 2f, top + 4.2f, bz - 0.15f), m, m);
            }
            Disc(k, new Vector3(0, top + 3.0f, bz - 0.22f), 0.8f, Paint("#fafafa"), 20);
            k.Box(new Vector3(-4.2f, top + 1.45f, bz - 0.8f), new Vector3(4.2f, top + 1.55f, bz - 0.2f), steel, steel);
        }

        static void Software(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 3f, z1 = d / 2 - 0.6f, h = 5.6f;
            var brick = S("wall_brick_red");
            var faces = Faces(x0, x1, z0, z1);
            foreach (var fc in faces) Plain(k, fc, 0, h, brick, 2.5f);
            var f = faces[South];
            var frame = Paint("#263238", 0.5f, 0.4f);
            for (float a = 0.8f; a < f.Width - 3f; a += 3.4f)
            {
                if (Mathf.Abs(a + 1.3f - f.Width / 2) < 1.8f) continue;
                Rect(k, f, a, a + 2.6f, 0.9f, 4.4f, Glass(closed), Vector2.zero, new Vector2(1, 1), 0.03f);
                Ledge(k, f, a + 1.25f, a + 1.35f, 0.9f, 4.4f, 0.05f, frame);
                Ledge(k, f, a, a + 2.6f, 2.6f, 2.7f, 0.05f, frame);
                Ledge(k, f, a - 0.1f, a + 2.7f, 4.4f, 4.6f, 0.08f, frame);
            }
            Door(k, f, f.Width / 2, H("#263238"), 2.2f, 2.8f);
            Sign(k, f, f.Width / 2, 5f, 3.3f, 0.6f, Row(BuildingType.SoftwareStudio), closed);
            // a sawtooth roof, glass to the north
            var roof = Snowy(T("roof_metal", new Color(0.55f, 0.57f, 0.6f)), season);
            var glass = closed ? Paint("#37474f", 0.9f) : Lit(H("#b3e5fc"), 1.0f);
            int n = 4; float step = (z1 - z0) / n;
            for (int i = 0; i < n; i++)
            {
                float za = z0 + i * step, zb = za + step;
                k.Quad(new Vector3(x0, h, za), new Vector3(x0, h + 1.5f, zb), new Vector3(x1, h + 1.5f, zb), new Vector3(x1, h, za), roof, 4f);
                k.Quad(new Vector3(x1, h, zb), new Vector3(x1, h + 1.5f, zb), new Vector3(x0, h + 1.5f, zb), new Vector3(x0, h, zb), glass, 4f);
                k.Tri(new Vector3(x0, h, za), new Vector3(x0, h, zb), new Vector3(x0, h + 1.5f, zb), brick);
                k.Tri(new Vector3(x1, h, za), new Vector3(x1, h + 1.5f, zb), new Vector3(x1, h, zb), brick);
            }
            for (int i = 0; i < 4; i++) Bicycle(k, -w / 2 + 1.5f + i * 0.8f, -d / 2 + 1.4f, 0, i % 2 == 0 ? H("#00bcd4") : H("#ff7043"));
            Planter(k, w / 2 - 1.5f, -d / 2 + 1.4f, season);
        }

        static void CallCenter(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            var skin = new Skin { Upper = "fac_precast", Floor = 3.4f, Ground = "gf_lobby", GroundH = 4f, Wall = "wall_stucco", Coping = H("#9aa3ab"), Glow = 1.3f };
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -1.6f, z1 = d / 2 - 0.6f;
            float top = Block(k, x0, x1, z0, z1, skin, 1, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var f = Faces(x0, x1, z0, z1)[South];
            Canopy(k, f, f.Width / 2 + 1f, f.Width / 2 + 5f, 3.4f, 1.6f, Paint("#455a64"), false);
            Sign(k, f, f.Width / 2 + 3f, 5f, 3.55f, 0.55f, Row(BuildingType.CallCenter), closed);
            // satellite dishes and a mast on the roof
            var white = Paint("#eceff1", 0.4f); var steel = Paint("#8d969e", 0.5f, 0.6f);
            foreach (float x in new[] { -6f, -3.5f })
            {
                k.Box(new Vector3(x - 0.08f, top, 1f), new Vector3(x + 0.08f, top + 0.8f, 1.16f), steel, steel);
                k.Cylinder(new Vector3(x, top + 0.8f, 1.08f), 0.15f, 0.9f, 0.4f, 12, white, white);
            }
            k.Box(new Vector3(6f, top, 2f), new Vector3(6.15f, top + 6f, 2.15f), steel, steel);
            for (int i = 1; i < 4; i++) k.Box(new Vector3(5.8f, top + i * 1.5f, 2.05f), new Vector3(6.35f, top + i * 1.5f + 0.06f, 2.1f), steel, steel);
            for (int i = 0; i < 3; i++)
            {
                k.Push(new Vector3(-7f + i * 4.6f, 0.06f, -3.7f), 90);
                PropArt.Car(k, PropArt.CarColors[(s + (uint)i) % (uint)PropArt.CarColors.Length]);
                k.Pop();
            }
        }

        static void Engineers(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var skin = new Skin { Upper = "fac_glass_blue", Floor = 3.6f, Bay = 3f, Ground = "gf_lobby", GroundH = 4.2f, Wall = "wall_stucco", Coping = H("#cfd8dc"), Glow = 1.4f };
            float x0 = -w / 2 + 0.8f, x1 = w / 2 - 0.8f, z0 = -d / 2 + 3.4f, z1 = d / 2 - 0.6f;
            float top = Block(k, x0, x1, z0, z1, skin, 2, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var f = Faces(x0, x1, z0, z1)[South];
            Sign(k, f, f.Width / 2, 6f, 3.5f, 0.6f, Row(BuildingType.EngineeringFirm), closed);
            // exposed steel bracing across the glass
            var steel = Paint("#c62828", 0.5f, 0.5f);
            int bays = 4; float bw = f.Width / bays;
            for (int i = 0; i <= bays; i++)
            {
                var p = f.At(i * bw, 4.2f) + f.Normal * 0.4f;
                k.Box(p + new Vector3(-0.15f, 0, -0.15f), p + new Vector3(0.15f, top - 4.2f + 0.3f, 0.15f), steel, steel);
            }
            for (int i = 0; i < bays; i++)
            {
                var a = f.At(i * bw, 4.2f) + f.Normal * 0.4f; var b = f.At((i + 1) * bw, top) + f.Normal * 0.4f;
                var c = f.At(i * bw, top) + f.Normal * 0.4f; var e = f.At((i + 1) * bw, 4.2f) + f.Normal * 0.4f;
                Bar(k, a, b, 0.18f, steel); Bar(k, c + Vector3.forward * 0.02f, e + Vector3.forward * 0.02f, 0.18f, steel);
            }
            var plant = Paint("#9aa3ab", 0.5f, 0.6f);
            k.Box(new Vector3(x0 + 3f, top, z1 - 4f), new Vector3(x0 + 7f, top + 1.8f, z1 - 1.5f), plant, plant);
            Planter(k, -3, -d / 2 + 1.6f, season); Planter(k, 3, -d / 2 + 1.6f, season);
        }

        static void DesignStudio(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float x0 = -w / 2 + 0.4f, x1 = w / 2 - 0.4f, z0 = -d / 2 + 2.4f, z1 = d / 2 - 0.4f;
            var faces = FlatBox(k, x0, x1, z0, z1, 7.4f, T("wall_stucco", new Color(0.97f, 0.97f, 0.97f)), Paint("#212121"), season, 0.4f);
            var f = faces[South];
            Rect(k, f, 0.4f, f.Width - 0.4f, 0, 3.2f, Glass(closed), Vector2.zero, new Vector2(0.75f, 1), 0.03f);
            Sign(k, f, f.Width / 2, 4.5f, 3.35f, 0.5f, Row(BuildingType.DesignStudio), closed);
            Door(k, f, f.Width / 2, H("#fdd835"), 1.2f, 2.6f);
            // a mosaic of coloured panels across the upper floor
            string[] pal = { "#ff1744", "#ffea00", "#00b0ff", "#00e676", "#d500f9", "#ff9100", "#fafafa", "#212121" };
            int cols = 6, rows = 3; float pw = f.Width / cols, ph = 3.0f / rows;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var m = Paint(pal[Next(ref s) % (uint)pal.Length], 0.5f);
                    float depth = 0.05f + (Next(ref s) % 3) * 0.08f;
                    Ledge(k, f, c * pw + 0.04f, (c + 1) * pw - 0.04f, 4.1f + r * ph + 0.04f, 4.1f + (r + 1) * ph - 0.04f, depth, m);
                }
        }

        /// <summary>A grocery chain's head office: three floors in the chain's colours, its name on the roof, its flags out
        /// front.</summary>
        static void ChainOffice(MeshKit k, float w, float d, ref uint s, bool closed, int season, BuildingType t)
        {
            Sidewalk(k, w, d);
            string upper; Color32 band, accent; int sign;
            if (t == BuildingType.RedwoodOffice) { upper = "fac_apt_cream"; band = H("#b3202a"); accent = H("#f5f6fa"); sign = SignRow.Redwood; }
            else if (t == BuildingType.BulkworksOffice) { upper = "fac_metal_grey"; band = H("#1f2a44"); accent = H("#ff7a1a"); sign = SignRow.Bulkworks; }
            else { upper = "fac_glass_blue"; band = H("#1d4f91"); accent = H("#ffd23f"); sign = SignRow.ValleyValue; }
            var skin = new Skin { Upper = upper, Floor = 3.4f, Bay = 3f, Ground = "gf_lobby", GroundH = 4.2f, Wall = "wall_stucco", Coping = band, Glow = 1.4f };
            float x0 = -w / 2 + 0.5f, x1 = w / 2 - 0.5f, z0 = -d / 2 + 2.4f, z1 = d / 2 - 0.5f;
            float top = Block(k, x0, x1, z0, z1, skin, 2, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var f = Faces(x0, x1, z0, z1)[South];
            var bandM = C(band, 0.4f); var accentM = C(accent, 0.4f);
            Ledge(k, f, -0.1f, f.Width + 0.1f, 3.95f, 4.35f, 0.3f, bandM, accentM);
            Ledge(k, f, -0.1f, f.Width + 0.1f, 3.8f, 3.95f, 0.32f, accentM);
            Canopy(k, f, f.Width / 2 - 1.5f, f.Width / 2 + 1.5f, 3.3f, 1.5f, bandM, false);
            RoofSign(k, 0, z0 + 0.5f, top, 7.6f, sign, closed);
            // flags in the chain's colours either side of the door
            var pole = C(H("#cfd8dc"), 0.7f, 0.8f);
            foreach (float x in new[] { -w / 2 + 1.0f, w / 2 - 1.0f })
            {
                float z = -d / 2 + 0.9f;
                k.Box(new Vector3(x - 0.04f, 0.02f, z - 0.04f), new Vector3(x + 0.04f, 6.5f, z + 0.04f), pole, pole);
                float dir = x < 0 ? 1 : -1;
                TwoQuad(k, new Vector3(x, 5.3f, z), new Vector3(x, 6.4f, z), new Vector3(x + dir * 1.5f, 6.4f, z), new Vector3(x + dir * 1.5f, 5.3f, z), bandM);
                TwoQuad(k, new Vector3(x, 5.62f, z - 0.01f), new Vector3(x, 6.08f, z - 0.01f), new Vector3(x + dir * 1.5f, 6.08f, z - 0.01f), new Vector3(x + dir * 1.5f, 5.62f, z - 0.01f), accentM);
            }
            Planter(k, -2.6f, -d / 2 + 1.3f, season); Planter(k, 2.6f, -d / 2 + 1.3f, season);
        }

        // ================================================================== places to stay and the rest
        static void Motel(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 0.6f, x1 = 5f, z0 = 0.2f, z1 = d / 2 - 0.4f;
            var wall = T("wall_stucco", new Color(0.7f, 0.9f, 0.88f));
            var faces = Faces(x0, x1, z0, z1);
            foreach (var fc in faces) Plain(k, fc, 0, 6f, wall, 2.5f);
            var f = faces[South];
            string[] doors = { "#e65100", "#00838f" };
            for (int fl = 0; fl < 2; fl++)
                for (float a = 0.5f; a < f.Width - 1.5f; a += 2.8f)
                {
                    float y = fl * 3f;
                    Rect(k, f, a, a + 0.9f, y + 0.05f, y + 2.2f, Paint(doors[fl]), Vector2.zero, Vector2.one, 0.03f);
                    Rect(k, f, a + 1.2f, a + 2.4f, y + 1.0f, y + 2.1f, MaterialLibrary.Pane((Next(ref s) % 3 == 0 && !closed) ? new Color32(255, 214, 140, 255) : new Color32(40, 48, 58, 255)), Vector2.zero, Vector2.one, 0.03f);
                }
            // the walkway along the upper floor, its railing and posts, a flat roof and the stair at the end
            var conc = Paint("#e0dcd2", 0.2f); var rail = Paint("#f4f4f2", 0.4f);
            k.Box(new Vector3(x0, 2.85f, z0 - 1.4f), new Vector3(x1, 3.05f, z0), conc, conc);
            k.Box(new Vector3(x0, 4.0f, z0 - 1.45f), new Vector3(x1, 4.08f, z0 - 1.35f), rail, rail);
            for (float x = x0 + 0.1f; x <= x1; x += 2.8f)
            {
                k.Box(new Vector3(x - 0.1f, 0.06f, z0 - 1.5f), new Vector3(x + 0.1f, 2.85f, z0 - 1.3f), rail, rail);
                k.Box(new Vector3(x - 0.03f, 3.05f, z0 - 1.43f), new Vector3(x + 0.03f, 4.0f, z0 - 1.37f), rail, rail);
            }
            k.Box(new Vector3(x0 - 0.3f, 6f, z0 - 1.7f), new Vector3(x1 + 0.3f, 6.3f, z1 + 0.2f), Paint("#ff7043"), Snowy(S("roof_membrane"), season));
            for (int i = 0; i < 10; i++)
                k.Box(new Vector3(x1 + i * 0.3f, 0.06f, z0 - 1.4f), new Vector3(x1 + (i + 1) * 0.3f, 3.0f - i * 0.29f, z0 - 0.2f), conc, conc);
            // a sign on a tall pole: a star, an arrow and the vacancy light
            float px = w / 2 - 1.2f, pz = -d / 2 + 1.0f;
            var pole = Paint("#ff7043", 0.4f);
            k.Box(new Vector3(px - 0.12f, 0.06f, pz - 0.12f), new Vector3(px + 0.12f, 8.5f, pz + 0.12f), pole, pole);
            var board = new Face { LeftBottom = new Vector3(px - 2.6f, 0, pz - 0.15f), Right = Vector3.right, Normal = Vector3.back, Width = 3.2f };
            k.Box(new Vector3(px - 2.7f, 5.2f, pz - 0.14f), new Vector3(px + 0.7f, 6.2f, pz + 0.14f), Paint("#00838f"), Paint("#00838f"));
            Sign(k, board, 1.6f, 3.0f, 5.35f, 0.7f, Row(BuildingType.Motel), closed);
            var arrow = Bulb(closed, "#ffeb3b", 2.6f);
            TwoQuad(k, new Vector3(px - 2.6f, 4.4f, pz - 0.2f), new Vector3(px - 2.6f, 4.8f, pz - 0.2f), new Vector3(px - 0.6f, 4.8f, pz - 0.2f), new Vector3(px - 0.6f, 4.4f, pz - 0.2f), arrow);
            Two(k, new Vector3(px - 3.4f, 4.6f, pz - 0.2f), new Vector3(px - 2.6f, 4.1f, pz - 0.2f), new Vector3(px - 2.6f, 5.1f, pz - 0.2f), arrow);
            var vac = Bulb(closed, "#ff4081", 3f);
            k.Box(new Vector3(px - 1.6f, 3.4f, pz - 0.2f), new Vector3(px - 0.2f, 3.9f, pz - 0.1f), vac, vac);
            var star = Bulb(closed, "#fff59d", 3f);
            var sc = new Vector3(px, 9.1f, pz - 0.15f);
            for (int i = 0; i < 8; i++) Two(k, sc + Dir(i * Mathf.PI / 4 - 0.3f) * 0.18f, sc + Dir(i * Mathf.PI / 4 + 0.3f) * 0.18f, sc + Dir(i * Mathf.PI / 4) * (i % 2 == 0 ? 0.8f : 0.45f), star);
            for (int i = 0; i < 3; i++) PropArt.Parked(k, new Vector3(-7.5f + i * 4.6f, 0.06f, -2.9f), 0, s + (uint)i);
        }

        static void Hotel(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            k.Flat(-5f, -d / 2 + 0.5f, 5f, -3f, 0.07f, S("asphalt"), 6f);
            // a podium of lobby, restaurant and ballroom, the rooms in a tower above
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -3f, z1 = d / 2 - 0.6f;
            var stone = T("wall_stone", new Color(0.86f, 0.82f, 0.74f));
            var pf = FlatBox(k, x0, x1, z0, z1, 5f, stone, Paint("#d4af37", 0.6f, 0.6f), season, 0.5f)[South];
            Rect(k, pf, 1f, pf.Width - 1f, 0.1f, 4.0f, Glass(closed), Vector2.zero, new Vector2(1.75f, 1), 0.03f);
            var skin = new Skin { Upper = "fac_apt_cream", Floor = 3.15f, Wall = "wall_stucco", Coping = H("#d4af37"), Glow = 1.4f };
            k.Push(new Vector3(0, 5f, 0), 0);
            float top = 5f + Block(k, -6.5f, 6.5f, -0.5f, 8f, skin, 6, (int)(Next(ref s) % 4), 0, true, false, "roof_membrane", season, closed);
            k.Pop();
            RoofSign(k, 0, -0.7f, top + 0.5f, 9f, Row(BuildingType.Hotel), closed);
            // a porte-cochere on posts with lights under it, flags by the drive
            var gold = Paint("#d4af37", 0.6f, 0.6f); var dark = Paint("#263238", 0.5f);
            k.Box(new Vector3(-4.2f, 4.0f, -d / 2 + 1.2f), new Vector3(4.2f, 4.5f, z0), dark, Snowy(dark, season));
            k.Box(new Vector3(-4.2f, 3.8f, -d / 2 + 1.2f), new Vector3(4.2f, 4.0f, -d / 2 + 1.4f), gold, gold);
            foreach (float x in new[] { -3.9f, 3.9f }) k.Box(new Vector3(x - 0.15f, 0.06f, -d / 2 + 1.4f), new Vector3(x + 0.15f, 4.0f, -d / 2 + 1.7f), gold, gold);
            var down = Bulb(closed, "#fff3c4", 3f);
            for (float x = -3f; x <= 3f; x += 1.5f) k.Box(new Vector3(x - 0.15f, 3.96f, -6f), new Vector3(x + 0.15f, 4.0f, -5.7f), down, down);
            string[] flags = { "#c62828", "#1565c0", "#2e7d32" };
            for (int i = 0; i < 3; i++)
            {
                float fx = -9f + i * 1.0f, fz = -d / 2 + 1f;
                k.Box(new Vector3(fx - 0.04f, 0.06f, fz - 0.04f), new Vector3(fx + 0.04f, 7f, fz + 0.04f), Paint("#cfd8dc", 0.7f, 0.8f), Paint("#cfd8dc", 0.7f, 0.8f));
                TwoQuad(k, new Vector3(fx, 5.9f, fz), new Vector3(fx, 6.9f, fz), new Vector3(fx + 0.9f, 6.9f, fz), new Vector3(fx + 0.9f, 5.9f, fz), Paint(flags[i]));
            }
            k.Push(new Vector3(-1.5f, 0.06f, -6f), 90);
            PropArt.Car(k, H("#1b1b1b"));
            k.Pop();
            Planter(k, 7f, -d / 2 + 1.5f, season); Planter(k, 8.5f, -d / 2 + 1.5f, season);
        }

        static void BedAndBreakfast(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            k.Flat(-0.6f, -d / 2 + 0.5f, 0.6f, -2.6f, 0.07f, S("pavers"), 2f);
            // a painted two-storey house with a porch and a turret on the corner
            float x0 = -3.8f, x1 = 3.4f, z0 = -1.0f, z1 = d / 2 - 0.8f;
            var siding = T("wall_siding", new Color(0.82f, 0.74f, 0.9f));
            var faces = GableBox(k, x0, x1, z0, z1, 5.6f, 2.4f, siding, T("roof_shingle", new Color(0.35f, 0.3f, 0.4f)), season);
            var f = faces[South];
            WindowRow(k, f, 0.5f, 2.6f, 1.0f, 1.0f, 1.6f, 1.4f, ref s, closed);
            WindowRow(k, f, 4.2f, f.Width - 0.6f, 1.0f, 1.0f, 1.6f, 1.4f, ref s, closed);
            WindowRow(k, f, 0.5f, f.Width - 0.6f, 3.6f, 1.0f, 1.5f, 1.4f, ref s, closed);
            Door(k, f, 3.4f, H("#4a148c"), 1.0f, 2.3f);
            var white = Paint("#fafafa", 0.4f);
            k.Box(new Vector3(x0, 0.06f, z0 - 1.6f), new Vector3(x1 - 1.2f, 0.45f, z0), white, T("wood", new Color(0.7f, 0.6f, 0.5f)));
            for (float x = x0 + 0.1f; x < x1 - 1.2f; x += 1.6f) k.Box(new Vector3(x, 0.45f, z0 - 1.55f), new Vector3(x + 0.12f, 2.9f, z0 - 1.43f), white, white);
            k.Box(new Vector3(x0 - 0.1f, 2.9f, z0 - 1.75f), new Vector3(x1 - 1.1f, 3.1f, z0), white, Snowy(Paint("#5e5470"), season));
            float tx = x1 - 0.2f, tz = z0 - 0.2f;
            k.Cylinder(new Vector3(tx, 0.06f, tz), 1.15f, 1.15f, 7.2f, 10, siding, null);
            k.Cylinder(new Vector3(tx, 7.26f, tz), 1.35f, 0f, 2.6f, 10, Snowy(T("roof_shingle", new Color(0.35f, 0.3f, 0.4f)), season), null);
            var lit = closed ? MaterialLibrary.Pane(new Color32(40, 48, 58, 255)) : MaterialLibrary.Pane(new Color32(255, 214, 140, 255));
            foreach (float y in new[] { 1.4f, 4.4f }) k.Box(new Vector3(tx - 0.35f, y, tz - 1.2f), new Vector3(tx + 0.35f, y + 1.3f, tz - 1.1f), lit, lit);
            // a picket fence and a hanging sign by the gate
            for (float x = -w / 2 + 0.4f; x < w / 2 - 0.3f; x += 0.3f)
            {
                if (Mathf.Abs(x) < 0.7f) continue;
                k.Box(new Vector3(x - 0.05f, 0.06f, -d / 2 + 0.6f), new Vector3(x + 0.05f, 0.9f, -d / 2 + 0.66f), white, white);
            }
            k.Box(new Vector3(-w / 2 + 0.4f, 0.55f, -d / 2 + 0.66f), new Vector3(w / 2 - 0.3f, 0.65f, -d / 2 + 0.72f), white, white);
            float sx = -2.2f, sz = -d / 2 + 1.2f;
            var post = T("wood", new Color(0.4f, 0.3f, 0.22f));
            k.Box(new Vector3(sx - 0.06f, 0.06f, sz - 0.06f), new Vector3(sx + 0.06f, 2.4f, sz + 0.06f), post, post);
            k.Box(new Vector3(sx - 0.06f, 2.3f, sz - 0.04f), new Vector3(sx + 1.6f, 2.4f, sz + 0.04f), post, post);
            var hang = new Face { LeftBottom = new Vector3(sx + 0.15f, 0, sz - 0.06f), Right = Vector3.right, Normal = Vector3.back, Width = 1.4f };
            Sign(k, hang, 0.7f, 1.3f, 1.75f, 0.42f, Row(BuildingType.BedAndBreakfast), closed);
            PropArt.Bush(k, new Vector3(-3.8f, 0.06f, -d / 2 + 1.8f), 0.7f, season);
            PropArt.Bush(k, new Vector3(1.6f, 0.06f, -d / 2 + 1.8f), 0.6f, season);
        }

        static void FuneralHome(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            k.Flat(2.6f, -d / 2 + 0.3f, w / 2 - 0.2f, d / 2 - 0.6f, 0.07f, S("pavers"), 3f);
            // a brick colonial house with a white portico and dark shutters
            float x0 = -w / 2 + 0.6f, x1 = 2.2f, z0 = -1.0f, z1 = d / 2 - 0.6f;
            var faces = GableBox(k, x0, x1, z0, z1, 4.4f, 2f, S("wall_brick_red"), T("roof_shingle", new Color(0.25f, 0.27f, 0.3f)), season);
            var f = faces[South];
            var shutter = Paint("#1b1b1b", 0.3f);
            foreach (float a in new[] { 0.9f, f.Width - 1.9f })
            {
                Rect(k, f, a, a + 1.0f, 1.2f, 3.2f, MaterialLibrary.Pane(closed ? new Color32(40, 48, 58, 255) : new Color32(255, 222, 170, 255)), Vector2.zero, Vector2.one, 0.03f);
                Ledge(k, f, a - 0.45f, a - 0.05f, 1.2f, 3.2f, 0.05f, shutter);
                Ledge(k, f, a + 1.05f, a + 1.45f, 1.2f, 3.2f, 0.05f, shutter);
            }
            Door(k, f, f.Width / 2, H("#111111"), 1.4f, 2.6f);
            var white = Paint("#f2ede2", 0.35f);
            float cx = (x0 + x1) / 2;
            k.Box(new Vector3(cx - 2.2f, 0.06f, z0 - 2.2f), new Vector3(cx + 2.2f, 0.35f, z0), white, white);
            foreach (float x in new[] { cx - 1.8f, cx - 0.6f, cx + 0.6f, cx + 1.8f }) k.Cylinder(new Vector3(x, 0.35f, z0 - 1.9f), 0.17f, 0.15f, 3.6f, 10, white, white);
            k.Box(new Vector3(cx - 2.2f, 3.95f, z0 - 2.2f), new Vector3(cx + 2.2f, 4.4f, z0), white, white);
            k.Push(new Vector3(cx, 0, z0 - 1.1f), 90);
            k.Gable(-1.1f, -2.2f, 1.1f, 2.2f, 4.4f, 1.0f, Snowy(T("roof_shingle", new Color(0.25f, 0.27f, 0.3f)), season), white, 0.1f);
            k.Pop();
            Hedge(k, x0, -d / 2 + 1.9f, cx - 2.6f, -d / 2 + 2.6f, season);
            // a low brick sign by the street and the hearse in the drive
            var brick = S("wall_brick_red");
            k.Box(new Vector3(-3.8f, 0.06f, -d / 2 + 0.6f), new Vector3(-0.6f, 1.2f, -d / 2 + 1.0f), brick, white);
            var mf = new Face { LeftBottom = new Vector3(-3.8f, 0, -d / 2 + 0.6f), Right = Vector3.right, Normal = Vector3.back, Width = 3.2f };
            Sign(k, mf, 1.6f, 2.8f, 0.6f, 0.36f, Row(BuildingType.FuneralHome), closed);
            k.Push(new Vector3(3.7f, 0.08f, -1.8f), 180);
            PropArt.Car(k, H("#141414"), 2);
            k.Pop();
        }

        static void Taxi(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float z0 = -0.6f;
            var f = Storefront(k, -w / 2 + 0.4f, 0.2f, z0, d / 2 - 0.4f, 3.8f, T("wall_stucco", new Color(0.98f, 0.86f, 0.35f)), "gf_store_light", closed, season, Row(BuildingType.TaxiCompany), Paint("#212121"), 0.1f, 0.9f, -1, 2.8f);
            var black = Paint("#111111"); var white = Paint("#fafafa");
            for (int i = 0; i < 16; i++)
            {
                float a = i * f.Width / 16;
                Ledge(k, f, a, a + f.Width / 16, 3.55f, 3.8f, 0.06f, i % 2 == 0 ? black : white);
            }
            // a giant roof light, a radio mast and the cabs
            var lamp = Bulb(closed, "#ffd740", 1.8f);
            float cx = (-w / 2 + 0.4f + 0.2f) / 2;
            k.Box(new Vector3(cx - 1.4f, 4.3f, z0 + 0.6f), new Vector3(cx + 1.4f, 5.1f, z0 + 1.4f), lamp, lamp);
            k.Box(new Vector3(cx - 1.5f, 4.3f, z0 + 0.5f), new Vector3(cx + 1.5f, 4.4f, z0 + 1.5f), black, black);
            var steel = Paint("#8d969e", 0.5f, 0.6f);
            k.Box(new Vector3(-w / 2 + 1f, 4.3f, d / 2 - 1.2f), new Vector3(-w / 2 + 1.12f, 9.5f, d / 2 - 1.08f), steel, steel);
            for (int i = 1; i < 4; i++) k.Box(new Vector3(-w / 2 + 0.8f, 4.3f + i * 1.4f, d / 2 - 1.16f), new Vector3(-w / 2 + 1.32f, 4.35f + i * 1.4f, d / 2 - 1.12f), steel, steel);
            foreach (float x in new[] { 1.6f, 3.8f })
            {
                k.Push(new Vector3(x, 0.06f, -1.6f), 180);
                PropArt.Car(k, H("#ffc107"));
                k.Box(new Vector3(-0.35f, 1.45f, -0.2f), new Vector3(0.35f, 1.7f, 0.2f), lamp, lamp);
                for (int i = 0; i < 8; i++)
                    foreach (float sx in new[] { -0.93f, 0.89f })
                        k.Box(new Vector3(sx, 0.7f + (i % 2) * 0.08f, -1.6f + i * 0.4f), new Vector3(sx + 0.04f, 0.78f + (i % 2) * 0.08f, -1.4f + i * 0.4f), black, black);
                k.Pop();
            }
        }
    }
}
