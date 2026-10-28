using RealLife.Simulation;
using UnityEngine;
using static RealLife.Game.Art.Facades;

namespace RealLife.Game.Art
{
    /// <summary>Gyms, salons, fun, health and household services added later, each its own model.</summary>
    public static partial class BuildingArt
    {
        static void ExtraServices(MeshKit k, BuildingType t, float w, float d, ref uint s, bool closed, int season, int floors)
        {
            switch (t)
            {
                case BuildingType.YogaStudio: Yoga(k, w, d, ref s, closed, season); break;
                case BuildingType.BoxingGym: BoxingGym(k, w, d, ref s, closed, season); break;
                case BuildingType.ClimbingGym: ClimbingGym(k, w, d, ref s, closed, season); break;
                case BuildingType.SwimClub: SwimClub(k, w, d, ref s, closed, season); break;
                case BuildingType.MartialArtsDojo: Dojo(k, w, d, ref s, closed, season); break;
                case BuildingType.Barbershop: Barbershop(k, w, d, ref s, closed, season); break;
                case BuildingType.NailSalon: NailSalon(k, w, d, ref s, closed, season); break;
                case BuildingType.DaySpa: DaySpa(k, w, d, ref s, closed, season); break;
                case BuildingType.TattooParlor: Tattoo(k, w, d, ref s, closed, season); break;
                case BuildingType.TanningSalon: Tanning(k, w, d, ref s, closed, season); break;
                case BuildingType.Cinema: Cinema(k, w, d, ref s, closed, season); break;
                case BuildingType.BowlingAlley: Bowling(k, w, d, ref s, closed, season); break;
                case BuildingType.Arcade: Arcade(k, w, d, ref s, closed, season); break;
                case BuildingType.PoolHall: PoolHall(k, w, d, ref s, closed, season); break;
                case BuildingType.EscapeRoom: EscapeRoom(k, w, d, ref s, closed, season); break;
                case BuildingType.MiniGolf: MiniGolf(k, w, d, ref s, closed, season); break;
                case BuildingType.SkatingRink: SkatingRink(k, w, d, ref s, closed, season); break;
                case BuildingType.Theater: Theater(k, w, d, ref s, closed, season); break;
                case BuildingType.ComedyClub: ComedyClub(k, w, d, ref s, closed, season); break;
                case BuildingType.Dentist: Dentist(k, w, d, ref s, closed, season); break;
                case BuildingType.Optician: Optician(k, w, d, ref s, closed, season); break;
                case BuildingType.VetClinic: VetClinic(k, w, d, ref s, closed, season); break;
                case BuildingType.PhysioClinic: Physio(k, w, d, ref s, closed, season); break;
                case BuildingType.Laundromat: Laundromat(k, w, d, ref s, closed, season); break;
                case BuildingType.DryCleaner: DryCleaner(k, w, d, ref s, closed, season); break;
                case BuildingType.Tailor: Tailor(k, w, d, ref s, closed, season); break;
                case BuildingType.PhoneRepair: PhoneRepair(k, w, d, ref s, closed, season); break;
                case BuildingType.CarWash: CarWash(k, w, d, ref s, closed, season); break;
                case BuildingType.Daycare: Daycare(k, w, d, ref s, closed, season); break;
                case BuildingType.MusicSchool: MusicSchool(k, w, d, ref s, closed, season); break;
                case BuildingType.DanceStudio: DanceStudio(k, w, d, ref s, closed, season); break;
                case BuildingType.TutoringCenter: Tutoring(k, w, d, ref s, closed, season); break;
                case BuildingType.DrivingSchool: DrivingSchool(k, w, d, ref s, closed, season); break;
                default: ExtraOffices(k, t, w, d, ref s, closed, season, floors); break;
            }
        }

        // ================================================================== little shape helpers
        static Vector3 Dir(float a) { return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); }
        /// <summary>A triangle seen from both sides.</summary>
        static void Two(MeshKit k, Vector3 a, Vector3 b, Vector3 c, Material m) { k.Tri(a, b, c, m); k.Tri(a, c, b, m); }
        static void TwoQuad(MeshKit k, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material m) { k.Quad(a, b, c, d, m); k.Quad(d, c, b, a, m); }
        /// <summary>A flat disc standing upright facing the street (and away from it).</summary>
        static void Disc(MeshKit k, Vector3 c, float r, Material m, int n = 16)
        {
            for (int i = 0; i < n; i++) Two(k, c, c + Dir(i * Mathf.PI * 2 / n) * r, c + Dir((i + 1) * Mathf.PI * 2 / n) * r, m);
        }
        /// <summary>A flat strip from a to b, upright facing the street.</summary>
        static void Bar(MeshKit k, Vector3 a, Vector3 b, float t, Material m)
        {
            var dir = (b - a).normalized; var n = new Vector3(-dir.y, dir.x, 0) * (t / 2);
            TwoQuad(k, a - n, b - n, b + n, a + n, m);
        }
        /// <summary>A ring of small blocks round a circle facing the street (neon outlines).</summary>
        static void Ring(MeshKit k, Vector3 c, float r, float a0, float a1, int n, float size, Material m)
        {
            for (int i = 0; i <= n; i++)
            {
                var p = c + Dir(a0 + (a1 - a0) * i / n) * r;
                k.Box(p - new Vector3(size, size, 0.05f), p + new Vector3(size, size, 0.05f), m, m);
            }
        }
        /// <summary>A post-and-rail fence round a rectangle, open at the middle of the street side.</summary>
        static void Fence(MeshKit k, float x0, float z0, float x1, float z1, float h, Material m, float gate = 1.4f)
        {
            float cx = (x0 + x1) / 2;
            int nx = Mathf.Max(1, Mathf.RoundToInt((x1 - x0) / 1.5f)), nz = Mathf.Max(1, Mathf.RoundToInt((z1 - z0) / 1.5f));
            for (int i = 0; i <= nx; i++)
                foreach (float z in new[] { z0, z1 })
                {
                    float x = x0 + (x1 - x0) * i / nx;
                    if (z == z0 && Mathf.Abs(x - cx) < gate / 2) continue;
                    k.Box(new Vector3(x - 0.05f, 0.06f, z - 0.05f), new Vector3(x + 0.05f, h, z + 0.05f), m, m);
                }
            for (int i = 1; i < nz; i++)
                foreach (float x in new[] { x0, x1 })
                {
                    float z = z0 + (z1 - z0) * i / nz;
                    k.Box(new Vector3(x - 0.05f, 0.06f, z - 0.05f), new Vector3(x + 0.05f, h, z + 0.05f), m, m);
                }
            foreach (float y in new[] { h * 0.45f, h - 0.08f })
            {
                k.Box(new Vector3(x0, y, z1 - 0.03f), new Vector3(x1, y + 0.07f, z1 + 0.03f), m, m);
                k.Box(new Vector3(x0 - 0.03f, y, z0), new Vector3(x0 + 0.03f, y + 0.07f, z1), m, m);
                k.Box(new Vector3(x1 - 0.03f, y, z0), new Vector3(x1 + 0.03f, y + 0.07f, z1), m, m);
                k.Box(new Vector3(x0, y, z0 - 0.03f), new Vector3(cx - gate / 2, y + 0.07f, z0 + 0.03f), m, m);
                k.Box(new Vector3(cx + gate / 2, y, z0 - 0.03f), new Vector3(x1, y + 0.07f, z0 + 0.03f), m, m);
            }
        }
        /// <summary>A barrel-vault roof spanning z, its ends filled in.</summary>
        static void Vault(MeshKit k, float x0, float x1, float z0, float z1, float y, float rise, Material roof, Material end, int n = 10)
        {
            float cz = (z0 + z1) / 2, hz = (z1 - z0) / 2;
            for (int i = 0; i < n; i++)
            {
                float a0 = Mathf.PI * i / n, a1 = Mathf.PI * (i + 1) / n;
                float za = cz - Mathf.Cos(a0) * hz, ya = y + Mathf.Sin(a0) * rise, zb = cz - Mathf.Cos(a1) * hz, yb = y + Mathf.Sin(a1) * rise;
                k.Quad(new Vector3(x0, ya, za), new Vector3(x0, yb, zb), new Vector3(x1, yb, zb), new Vector3(x1, ya, za), roof, 4f);
                k.Tri(new Vector3(x0, y, cz), new Vector3(x0, yb, zb), new Vector3(x0, ya, za), end);
                k.Tri(new Vector3(x1, y, cz), new Vector3(x1, ya, za), new Vector3(x1, yb, zb), end);
            }
        }
        static Material Paint(string hex, float smooth = 0.4f, float metal = 0f) { return C(H(hex), smooth, metal); }
        static Material Bulb(bool closed, string hex, float i = 3f) { return closed ? C(H("#555555")) : Lit(H(hex), i); }

        // ================================================================== fitness
        static void Yoga(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.8f, T("wall_stucco", new Color(0.86f, 0.9f, 0.82f)), "gf_store_light", closed, season, Row(BuildingType.YogaStudio), Paint("#8d6e63"), 0.1f, 0.9f, -1, 3.4f);
            var wood = T("wood", new Color(0.72f, 0.56f, 0.4f));
            for (float a = 1.2f; a < f.Width - 1f; a += 1.1f) Ledge(k, f, a, a + 0.08f, 0, 3.4f, 0.3f, wood);
            // a lotus on the parapet
            var pink = Paint("#f48fb1", 0.3f); var pale = Paint("#fce4ec", 0.3f);
            var c = new Vector3(0, 5.3f, z0 - 0.12f);
            for (int i = 0; i < 5; i++)
            {
                float a = Mathf.PI * (0.1f + 0.2f * i), r = i == 2 ? 1.5f : 1.15f;
                var m = i % 2 == 0 ? pink : pale;
                Two(k, c, c + Dir(a - 0.24f) * r * 0.55f, c + Dir(a) * r, m);
                Two(k, c, c + Dir(a) * r, c + Dir(a + 0.24f) * r * 0.55f, m);
            }
            // bamboo in planters either side of the door
            var stalk = Paint("#7cb342", 0.3f);
            foreach (float x in new[] { -w / 2 + 1.2f, w / 2 - 1.2f })
            {
                k.Box(new Vector3(x - 0.6f, 0.02f, -d / 2 + 0.9f), new Vector3(x + 0.6f, 0.5f, -d / 2 + 1.7f), Paint("#5d4037"), T("dirt", Color.white));
                for (int i = 0; i < 5; i++) k.Cylinder(new Vector3(x - 0.4f + i * 0.2f, 0.5f, -d / 2 + 1.1f + (i % 2) * 0.35f), 0.045f, 0.04f, 2.2f + (i % 3) * 0.5f, 5, stalk, stalk);
            }
        }

        static void BoxingGym(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 5.2f, S("wall_brick_grey"), "gf_store_dark", closed, season, Row(BuildingType.BoxingGym), Paint("#c62828"), 0.1f, 0.9f, -1, 3.6f);
            Ledge(k, f, 0, f.Width, 4.5f, 4.75f, 0.06f, Paint("#c62828"));
            Ledge(k, f, 0, f.Width, 4.75f, 5.0f, 0.06f, Paint("#1565c0"));
            // a boxing glove on the roof
            float z = z0 + 1.8f;
            var white = Paint("#f4f4f2", 0.3f);
            k.Cylinder(new Vector3(0, 5.2f, z), 0.65f, 0.72f, 0.9f, 10, white, white);
            k.Blob(new Vector3(0, 7.0f, z), new Vector3(1.1f, 1.25f, 0.95f), H("#c62828"));
            k.Blob(new Vector3(-0.95f, 6.6f, z - 0.25f), new Vector3(0.45f, 0.75f, 0.5f), H("#c62828"));
            // a heavy bag hung from a bracket by the door
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            var p = f.At(f.Width - 0.7f, 3.4f);
            k.Box(p + new Vector3(-0.05f, 0, -1.1f), p + new Vector3(0.05f, 0.1f, 0), iron, iron);
            k.Box(p + new Vector3(-0.02f, -0.5f, -1.02f), p + new Vector3(0.02f, 0, -0.98f), iron, iron);
            k.Cylinder(p + new Vector3(0, -1.9f, -1.0f), 0.3f, 0.3f, 1.4f, 10, Paint("#b71c1c", 0.5f), Paint("#212121"));
        }

        static void ClimbingGym(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 2.6f, z1 = d / 2 - 0.6f;
            var faces = FlatBox(k, x0, x1, z0, z1, 9.5f, T("metal_panel", new Color(0.55f, 0.6f, 0.66f)), Paint("#ff6f00"), season);
            var f = faces[South];
            Rect(k, f, 0.8f, f.Width * 0.45f, 0.1f, 3.6f, Glass(closed), Vector2.zero, new Vector2(1.5f, 1), 0.03f);
            Sign(k, f, f.Width * 0.24f, 6.5f, 5f, 0.85f, Row(BuildingType.ClimbingGym), closed);
            // the feature wall: grey panels studded with holds
            float a0 = f.Width * 0.52f, a1 = f.Width - 0.5f;
            Ledge(k, f, a0, a1, 0.3f, 9.2f, 0.12f, Paint("#8f9499", 0.2f));
            Color32[] holds = { H("#ff1744"), H("#ffea00"), H("#00e676"), H("#2979ff"), H("#d500f9"), H("#ff9100") };
            for (int i = 0; i < 70; i++)
            {
                float a = a0 + 0.3f + (Next(ref s) % 1000) / 1000f * (a1 - a0 - 0.6f), y = 0.6f + (Next(ref s) % 1000) / 1000f * 8.2f;
                float r = 0.1f + (Next(ref s) % 3) * 0.05f;
                var p = f.At(a, y) + f.Normal * 0.15f;
                var m = C(holds[i % holds.Length], 0.3f);
                k.Box(p - new Vector3(r, r * 0.8f, 0.06f), p + new Vector3(r, r * 0.8f, 0.06f), m, m);
            }
            // a rope hanging down the wall
            var rope = Paint("#ff6f00", 0.2f);
            k.Box(f.At(a0 + 2f, 0.8f) + f.Normal * 0.3f, f.At(a0 + 2.05f, 9.2f) + f.Normal * 0.25f, rope, rope);
        }

        static void SwimClub(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = 0f, z1 = d / 2 - 0.6f;
            var faces = Faces(x0, x1, z0, z1);
            var wall = T("wall_stucco", new Color(0.92f, 0.95f, 0.97f));
            foreach (var fc in faces) Plain(k, fc, 0, 5f, wall, 2.5f);
            var f = faces[South];
            Rect(k, f, 1, f.Width - 1, 0.2f, 4.0f, Glass(closed), Vector2.zero, new Vector2(4, 1), 0.03f);
            Vault(k, x0 - 0.3f, x1 + 0.3f, z0 - 0.3f, z1 + 0.3f, 5f, 3f, Snowy(T("roof_metal", new Color(0.75f, 0.82f, 0.88f)), season), wall);
            Sign(k, f, f.Width / 2, 7f, 4.15f, 0.7f, Row(BuildingType.SwimClub), closed);
            // the outdoor pool in front: lanes in summer, a cover in winter
            var deck = Paint("#e0dcd2", 0.2f);
            k.Box(new Vector3(-w / 2 + 0.8f, 0.05f, -d / 2 + 0.8f), new Vector3(w / 2 - 0.8f, 0.25f, -0.8f), deck, deck);
            float px0 = -6.5f, px1 = 6.5f, pz0 = -d / 2 + 2.2f, pz1 = -2.6f;
            bool covered = closed || season == 0;
            k.Flat(px0, pz0, px1, pz1, 0.27f, covered ? Paint("#78909c", 0.3f) : Paint("#29b6f6", 0.95f), 4f);
            var coping = Paint("#f4f4f2", 0.3f);
            k.Box(new Vector3(px0 - 0.3f, 0.25f, pz0 - 0.3f), new Vector3(px1 + 0.3f, 0.32f, pz0), coping, coping);
            k.Box(new Vector3(px0 - 0.3f, 0.25f, pz1), new Vector3(px1 + 0.3f, 0.32f, pz1 + 0.3f), coping, coping);
            k.Box(new Vector3(px0 - 0.3f, 0.25f, pz0), new Vector3(px0, 0.32f, pz1), coping, coping);
            k.Box(new Vector3(px1, 0.25f, pz0), new Vector3(px1 + 0.3f, 0.32f, pz1), coping, coping);
            if (!covered)
            {
                var red = Paint("#e53935"); var white = Paint("#fafafa");
                for (int i = 1; i < 4; i++)
                {
                    float z = pz0 + (pz1 - pz0) * i / 4;
                    for (int j = 0; j < 13; j++) k.Box(new Vector3(px0 + j, 0.28f, z - 0.04f), new Vector3(px0 + j + 1, 0.34f, z + 0.04f), j % 2 == 0 ? red : white, j % 2 == 0 ? red : white);
                }
            }
            // a diving board at the deep end and a lifeguard chair
            k.Box(new Vector3(px1 - 0.2f, 0.25f, -5.6f), new Vector3(px1 + 0.6f, 0.9f, -4.8f), deck, deck);
            k.Box(new Vector3(px1 - 1.8f, 0.9f, -5.4f), new Vector3(px1 + 0.4f, 0.98f, -5.0f), Paint("#1e88e5"), Paint("#1e88e5"));
            var chair = Paint("#f4f4f2");
            foreach (float x in new[] { px0 - 1.0f, px0 - 0.4f }) k.Box(new Vector3(x - 0.04f, 0.25f, -4.4f), new Vector3(x + 0.04f, 2.2f, -4.3f), chair, chair);
            k.Box(new Vector3(px0 - 1.05f, 2.2f, -4.6f), new Vector3(px0 - 0.35f, 2.28f, -4.1f), chair, chair);
            for (int i = 0; i < 3; i++) CafeTable(k, new Vector3(-6 + i * 6, 0.25f, -1.6f), H("#ff7043"));
        }

        static void Dojo(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("gravel"), 0.05f, 3f);
            float x0 = -w / 2 + 0.8f, x1 = w / 2 - 0.8f, z0 = -d / 2 + 3.8f, z1 = d / 2 - 0.6f;
            var faces = Faces(x0, x1, z0, z1);
            var plaster = T("wall_stucco", new Color(0.97f, 0.95f, 0.9f)); var dark = T("wood", new Color(0.3f, 0.22f, 0.16f));
            foreach (var fc in faces) Plain(k, fc, 0, 3.6f, plaster, 2f);
            var f = faces[South];
            var screen = closed ? Paint("#b8b2a6") : MaterialLibrary.Glow(new Color(0.95f, 0.92f, 0.84f), new Color(1f, 0.85f, 0.6f), 1.1f);
            Rect(k, f, f.Width / 4 + 0.15f, 3 * f.Width / 4 - 0.15f, 0.5f, 3.1f, screen, Vector2.zero, Vector2.one, 0.02f);
            for (int i = 0; i <= 4; i++) Ledge(k, f, i * f.Width / 4 - 0.1f, i * f.Width / 4 + 0.1f, 0, 3.6f, 0.08f, dark);
            Ledge(k, f, 0, f.Width, 0, 0.5f, 0.08f, dark); Ledge(k, f, 0, f.Width, 3.1f, 3.6f, 0.08f, dark);
            for (float a = f.Width / 4 + 0.6f; a < 3 * f.Width / 4 - 0.3f; a += 0.6f) Ledge(k, f, a - 0.03f, a + 0.03f, 0.5f, 3.1f, 0.04f, dark);
            k.Hip(x0, z0, x1, z1, 3.6f, 2f, Snowy(T("roof_shingle", new Color(0.22f, 0.24f, 0.26f)), season), 1.0f);
            // a gate on the path in, its name board under the lintel
            var red = Paint("#c62828"); var black = Paint("#1b1b1b");
            float gz = -d / 2 + 1.5f;
            foreach (float x in new[] { -1.6f, 1.6f }) k.Cylinder(new Vector3(x, 0.06f, gz), 0.18f, 0.15f, 3.4f, 8, red, red);
            k.Box(new Vector3(-2.0f, 2.7f, gz - 0.12f), new Vector3(2.0f, 2.95f, gz + 0.12f), red, red);
            k.Box(new Vector3(-2.4f, 3.1f, gz - 0.16f), new Vector3(2.4f, 3.35f, gz + 0.16f), red, red);
            k.Box(new Vector3(-2.7f, 3.35f, gz - 0.2f), new Vector3(2.7f, 3.6f, gz + 0.2f), black, black);
            var gate = new Face { LeftBottom = new Vector3(-1.3f, 0, gz - 0.13f), Right = Vector3.right, Normal = Vector3.back, Width = 2.6f };
            Sign(k, gate, 1.3f, 2.4f, 2.1f, 0.42f, Row(BuildingType.MartialArtsDojo), closed);
            k.Flat(-0.7f, gz, 0.7f, z0, 0.07f, S("pavers"), 2f);
        }

        // ================================================================== looks
        static void Barbershop(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_brick", "gf_store_wood", 2, ref s, closed, season, out top);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, Paint("#0d47a1"));
            Sign(k, Out(f, 0.3f), f.Width / 2, 5f, 3.62f, 0.55f, Row(BuildingType.Barbershop), closed);
            Awning(k, f, 0.5f, f.Width - 1.5f, 3.4f, 1.0f, H("#c62828"));
            // the barber's pole by the door
            var p = f.At(f.Width - 0.75f, 1.0f) + f.Normal * 0.4f;
            var steel = Paint("#cfd8dc", 0.7f, 0.8f);
            k.Cylinder(p, 0.2f, 0.2f, 0.12f, 10, steel, steel);
            string[] bands = { "#c62828", "#f4f4f2", "#1565c0", "#f4f4f2" };
            for (int i = 0; i < 12; i++) k.Cylinder(p + Vector3.up * (0.12f + i * 0.12f), 0.15f, 0.15f, 0.12f, 10, closed ? Paint(bands[i % 4], 0.6f) : Lit(H(bands[i % 4]), 1.3f), null);
            k.Cylinder(p + Vector3.up * 1.56f, 0.2f, 0.2f, 0.1f, 10, steel, steel);
            k.Blob(p + Vector3.up * 1.78f, new Vector3(0.17f, 0.17f, 0.17f), H("#cfd8dc"));
            k.Box(p + new Vector3(-0.04f, 0.7f, 0), p + new Vector3(0.04f, 0.8f, 0.4f), steel, steel);
        }

        static void NailSalon(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(1f, 0.88f, 0.92f)), "gf_store_light", closed, season, Row(BuildingType.NailSalon), Paint("#ec407a"));
            Neon(k, f, 0.3f, f.Width - 0.3f, 0.3f, H("#ff4081"), closed, 0.06f);
            // a giant bottle of polish and two little ones on the roof
            float z = z0 + 1.0f, y = 4.6f;
            var cap = Paint("#212121", 0.7f); var label = Paint("#fafafa", 0.3f);
            k.Box(new Vector3(-0.8f, y, z - 0.5f), new Vector3(0.8f, y + 2.0f, z + 0.5f), Paint("#d81b60", 0.9f), Paint("#d81b60", 0.9f));
            k.Box(new Vector3(-0.6f, y + 0.8f, z - 0.53f), new Vector3(0.6f, y + 1.4f, z - 0.5f), label, label);
            k.Box(new Vector3(-0.25f, y + 2.0f, z - 0.2f), new Vector3(0.25f, y + 2.2f, z + 0.2f), Paint("#d81b60", 0.9f), Paint("#d81b60", 0.9f));
            k.Cylinder(new Vector3(0, y + 2.2f, z), 0.3f, 0.24f, 1.5f, 10, cap, cap);
            string[] cols = { "#7e57c2", "#26c6da" };
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -2.2f : 2.2f;
                k.Box(new Vector3(x - 0.4f, y, z - 0.3f), new Vector3(x + 0.4f, y + 1.0f, z + 0.3f), Paint(cols[i], 0.9f), Paint(cols[i], 0.9f));
                k.Cylinder(new Vector3(x, y + 1.0f, z), 0.16f, 0.13f, 0.8f, 8, cap, cap);
            }
        }

        static void DaySpa(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x0 = -w / 2 + 0.6f, x1 = 3.5f, z0 = -d / 2 + 2.6f, z1 = d / 2 - 0.6f;
            var faces = Faces(x0, x1, z0, z1);
            var wall = T("wall_stucco", new Color(0.96f, 0.94f, 0.9f)); var wood = T("wood", new Color(0.6f, 0.45f, 0.32f));
            foreach (var fc in faces) Plain(k, fc, 0, 4.2f, wall, 2.5f);
            var f = faces[South];
            var screen = closed ? Paint("#b8b2a6") : MaterialLibrary.Glow(new Color(0.95f, 0.92f, 0.84f), new Color(1f, 0.85f, 0.6f), 1.1f);
            Rect(k, f, 1f, f.Width - 1f, 0.3f, 3.0f, screen, Vector2.zero, Vector2.one, 0.02f);
            for (float a = 0.3f; a < f.Width * 0.3f; a += 0.45f) Ledge(k, f, a, a + 0.12f, 0, 4.2f, 0.25f, wood);
            for (float a = f.Width * 0.7f; a < f.Width - 0.3f; a += 0.45f) Ledge(k, f, a, a + 0.12f, 0, 4.2f, 0.25f, wood);
            k.Box(new Vector3(x0 - 0.8f, 4.2f, z0 - 1.2f), new Vector3(x1 + 0.8f, 4.5f, z1 + 0.4f), wood, Snowy(Paint("#5d6670", 0.3f), season));
            Sign(k, f, f.Width / 2, 5f, 3.3f, 0.6f, Row(BuildingType.DaySpa), closed);
            // the garden: a pond, rocks, bamboo and a stone lantern behind a low wall
            float gx0 = 4.1f, gx1 = w / 2 - 0.6f;
            k.Flat(gx0, -d / 2 + 0.6f, gx1, d / 2 - 0.6f, 0.07f, GrassMat(season), 4f);
            k.Flat(gx0 + 0.8f, -d / 2 + 2f, gx1 - 0.8f, 1.5f, 0.09f, season == 0 ? S("snow") : Paint("#4fa3c7", 0.95f), 3f);
            for (int i = 0; i < 6; i++)
                k.Blob(new Vector3(gx0 + 0.7f + i * 0.9f, 0.2f, (i % 2 == 0 ? -d / 2 + 1.8f : 1.7f)), new Vector3(0.4f, 0.25f, 0.35f), H("#8d8a84"));
            var stalk = Paint("#7cb342", 0.3f);
            for (int i = 0; i < 12; i++) k.Cylinder(new Vector3(gx0 + 0.4f + i * 0.45f, 0.07f, d / 2 - 1.2f + (i % 3) * 0.2f), 0.05f, 0.04f, 3f + (i % 4) * 0.4f, 5, stalk, stalk);
            var stone = Paint("#9e9a92", 0.2f);
            float lx = gx1 - 1f, lz = 2.6f;
            k.Box(new Vector3(lx - 0.3f, 0.07f, lz - 0.3f), new Vector3(lx + 0.3f, 0.3f, lz + 0.3f), stone, stone);
            k.Box(new Vector3(lx - 0.1f, 0.3f, lz - 0.1f), new Vector3(lx + 0.1f, 0.9f, lz + 0.1f), stone, stone);
            k.Box(new Vector3(lx - 0.25f, 0.9f, lz - 0.25f), new Vector3(lx + 0.25f, 1.3f, lz + 0.25f), stone, Bulb(closed, "#ffcc80", 2f));
            k.Box(new Vector3(lx - 0.4f, 1.3f, lz - 0.4f), new Vector3(lx + 0.4f, 1.45f, lz + 0.4f), stone, stone);
            k.Box(new Vector3(gx0, 0.06f, -d / 2 + 0.6f), new Vector3(gx1, 1.0f, -d / 2 + 0.85f), wall, Paint("#5d6670", 0.3f));
        }

        static void Tattoo(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 5f, T("wall_stucco", new Color(0.12f, 0.12f, 0.13f)), "gf_store_dark", closed, season, Row(BuildingType.TattooParlor), Paint("#b71c1c"), 0.15f, 0.85f, -1, 3.4f);
            Neon(k, f, f.Width * 0.15f, f.Width * 0.85f, 0.2f, H("#ff1744"), closed, 0.06f);
            // a neon heart with a banner across it, up on the roof
            var heart = Bulb(closed, "#ff1744", 2.2f);
            var c = new Vector3(0, 7.1f, z0 - 0.1f);
            Disc(k, c + new Vector3(-0.55f, 0, 0), 0.65f, heart); Disc(k, c + new Vector3(0.55f, 0, 0), 0.65f, heart);
            Two(k, c + new Vector3(-1.17f, -0.2f, 0), c + new Vector3(1.17f, -0.2f, 0), c + new Vector3(0, -1.6f, 0), heart);
            TwoQuad(k, c + new Vector3(-1.17f, -0.2f, 0), c + new Vector3(-1.17f, 0f, 0), c + new Vector3(1.17f, 0f, 0), c + new Vector3(1.17f, -0.2f, 0), heart);
            var cream = Paint("#f3e5c0", 0.3f);
            k.Box(c + new Vector3(-1.6f, -0.65f, -0.1f), c + new Vector3(1.6f, -0.25f, -0.04f), cream, cream);
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(new Vector3(-0.06f, 5.5f, z0 + 0.0f), new Vector3(0.06f, 7.2f, z0 + 0.12f), iron, iron);
        }

        static void Tanning(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(1f, 0.8f, 0.5f)), "gf_store_light", closed, season, Row(BuildingType.TanningSalon), Paint("#ff6f00"));
            PlainAwning(k, f, 0.4f, f.Width - 0.4f, 3.85f, 1.0f, H("#ff8f00"));
            // a sun on the roof
            var sun = closed ? Paint("#f9a825", 0.3f) : Lit(H("#ffca28"), 1.6f);
            var c = new Vector3(0, 7.0f, z0 - 0.1f);
            Disc(k, c, 0.9f, sun, 20);
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6;
                Two(k, c + Dir(a - 0.13f) * 1.05f, c + Dir(a + 0.13f) * 1.05f, c + Dir(a) * 1.7f, sun);
            }
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(new Vector3(-0.06f, 5.1f, z0), new Vector3(0.06f, 7f, z0 + 0.12f), iron, iron);
        }

        // ================================================================== fun
        static void Cinema(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 5f, z1 = d / 2 - 0.6f;
            k.Flat(x0, -d / 2 + 0.5f, x1, z0, 0.07f, S("pavers"), 3f);
            var faces = FlatBox(k, x0, x1, z0, z1, 10f, T("wall_stucco", new Color(0.55f, 0.18f, 0.2f)), Paint("#d4af37", 0.6f, 0.6f), season);
            var f = faces[South];
            Rect(k, f, f.Width / 2 - 4f, f.Width / 2 + 4f, 0.1f, 3.6f, Glass(closed), Vector2.zero, new Vector2(1.5f, 1), 0.03f);
            // the marquee: a deep canopy carrying a lit letter board, bulbs round its edge
            var gold = Paint("#d4af37", 0.6f, 0.6f);
            float my = 3.9f, depth = 3.0f, mx0 = -5f, mx1 = 5f, fz = z0;
            k.Box(new Vector3(mx0, my, fz - depth), new Vector3(mx1, my + 0.3f, fz), gold, Snowy(gold, season));
            var board = closed ? Paint("#cfcfcf", 0.3f) : Lit(H("#fff8e1"), 1.4f);
            k.Box(new Vector3(mx0, my + 0.3f, fz - depth), new Vector3(mx1, my + 1.7f, fz - depth + 0.25f), board, gold);
            var ink = Paint("#212121");
            for (int r = 0; r < 2; r++)
                for (float x = mx0 + 0.5f; x < mx1 - 0.6f; x += 0.42f)
                {
                    if (Next(ref s) % 5 == 0) continue;
                    k.Box(new Vector3(x, my + 0.5f + r * 0.6f, fz - depth - 0.02f), new Vector3(x + 0.3f, my + 0.85f + r * 0.6f, fz - depth + 0.01f), ink, ink);
                }
            var bulb = Bulb(closed, "#ffe082", 3.5f);
            for (float x = mx0 + 0.15f; x < mx1; x += 0.45f)
            {
                k.Box(new Vector3(x - 0.06f, my + 0.05f, fz - depth - 0.08f), new Vector3(x + 0.06f, my + 0.2f, fz - depth), bulb, bulb);
                k.Box(new Vector3(x - 0.06f, my + 1.75f, fz - depth + 0.05f), new Vector3(x + 0.06f, my + 1.88f, fz - depth + 0.17f), bulb, bulb);
            }
            Sign(k, f, f.Width / 2, 10f, 6.3f, 1.3f, Row(BuildingType.Cinema), closed);
            // posters either side of the doors
            string[] posters = { "#1e88e5", "#e53935", "#43a047", "#8e24aa" };
            float[] at = { 1.4f, 3.4f, f.Width - 4.8f, f.Width - 2.8f };
            for (int i = 0; i < 4; i++) Rect(k, f, at[i], at[i] + 1.4f, 0.6f, 2.9f, closed ? Paint(posters[i]) : Lit(H(posters[i]), 1.2f), Vector2.zero, Vector2.one, 0.05f);
            // a blade sign up the corner
            var blade = new Vector3(x0 + 1.2f, 4.2f, fz);
            k.Box(blade + new Vector3(-0.12f, 0, -1.3f), blade + new Vector3(0.12f, 5.6f, 0), Paint("#7f1d1d"), gold);
            for (float y = 0.4f; y < 5.4f; y += 0.6f) k.Box(blade + new Vector3(-0.16f, y, -1.0f), blade + new Vector3(0.16f, y + 0.35f, -0.3f), bulb, bulb);
        }

        static void Bowling(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 6.5f, z1 = d / 2 - 0.6f;
            var faces = FlatBox(k, x0, x1, z0, z1, 6.5f, T("wall_stucco", new Color(0.95f, 0.93f, 0.88f)), Paint("#1565c0"), season);
            var f = faces[South];
            Rect(k, f, f.Width / 2 - 3f, f.Width / 2 + 3f, 0.1f, 3.4f, Glass(closed), Vector2.zero, new Vector2(1, 1), 0.03f);
            Ledge(k, f, 0, f.Width, 3.6f, 5.2f, 0.3f, Paint("#1565c0"));
            Sign(k, Out(f, 0.3f), f.Width / 2, 8f, 3.95f, 0.9f, Row(BuildingType.BowlingAlley), closed);
            // starbursts along the band
            var star = Bulb(closed, "#ffeb3b", 2.4f);
            foreach (float a in new[] { 2f, f.Width - 2f })
            {
                var c = f.At(a, 4.4f) + f.Normal * 0.32f;
                for (int i = 0; i < 8; i++) Two(k, c + Dir(i * Mathf.PI / 4 - 0.25f) * 0.2f, c + Dir(i * Mathf.PI / 4 + 0.25f) * 0.2f, c + Dir(i * Mathf.PI / 4) * 0.75f, star);
            }
            // a giant pin and ball on the roof
            float px = 5f, pz = 2f, y = 6.5f, sc = 2.2f;
            var white = Paint("#f4f4f2", 0.6f); var red = Paint("#c62828", 0.5f);
            float[] r = { 0.5f, 0.9f, 0.36f, 0.4f, 0.42f, 0.45f, 0.5f, 0.3f, 0f };
            float[] h = { 1.2f, 1.3f, 0.12f, 0.12f, 0.12f, 0.25f, 0.3f, 0.15f };
            float yy = y;
            for (int i = 0; i < h.Length; i++)
            {
                k.Cylinder(new Vector3(px, yy, pz), r[i] * sc, r[i + 1] * sc, h[i] * sc, 14, i == 2 || i == 4 ? red : white, null);
                yy += h[i] * sc;
            }
            k.Blob(new Vector3(px - 3f, y + 1.3f, pz - 0.4f), new Vector3(1.3f, 1.3f, 1.3f), H("#1a237e"));
            for (int i = 0; i < 4; i++) PropArt.Parked(k, new Vector3(i < 2 ? -8f + i * 2.6f : 5.4f + (i - 2) * 2.6f, 0.06f, -d / 2 + 3.2f), 0, s + (uint)i);
        }

        static void Arcade(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.15f, 0.1f, 0.25f)), "gf_store_dark", closed, season, Row(BuildingType.Arcade), Paint("#7c4dff"));
            Neon(k, f, 0.2f, f.Width - 0.2f, 0.25f, H("#00e5ff"), closed, 0.08f);
            Neon(k, f, 0.2f, f.Width - 0.2f, 4.42f, H("#ff4081"), closed, 0.08f);
            // a pixel alien on a board on the roof
            string[] px = { "..X.....X..", "...X...X...", "..XXXXXXX..", ".XX.XXX.XX.", "XXXXXXXXXXX", "X.XXXXXXX.X", "X.X.....X.X", "...XX.XX..." };
            float sz = 0.24f;
            var board = Paint("#120a1f", 0.3f);
            k.Box(new Vector3(-1.7f, 5.1f, z0 + 0.05f), new Vector3(1.7f, 7.6f, z0 + 0.25f), board, board);
            var c0 = new Vector3(-11 * sz / 2, 5.4f, z0 + 0.05f);
            var lit = Bulb(closed, "#76ff03", 2.6f);
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 11; c++)
                    if (px[r][c] == 'X')
                        k.Box(c0 + new Vector3(c * sz, (7 - r) * sz, -0.06f), c0 + new Vector3((c + 1) * sz - 0.02f, (8 - r) * sz - 0.02f, 0f), lit, lit);
        }

        static void PoolHall(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_tan", "gf_store_dark", 2, ref s, closed, season, out top);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, Paint("#1b5e20"));
            Sign(k, Out(f, 0.3f), f.Width / 2, 5f, 3.62f, 0.55f, Row(BuildingType.PoolHall), closed);
            Neon(k, Out(f, 0.3f), 0.2f, f.Width - 0.2f, 3.42f, H("#69f0ae"), closed, 0.06f);
            // an eight ball hung on a bracket
            var p = f.At(0.9f, 5.6f) + f.Normal * 1.1f;
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(p + new Vector3(-0.04f, 0.85f, 0), p + new Vector3(0.04f, 0.95f, 1.1f), iron, iron);
            k.Blob(p, new Vector3(0.7f, 0.7f, 0.7f), H("#111111"));
            var white = Paint("#f4f4f2", 0.5f); var black = Paint("#111111", 0.8f);
            Disc(k, p + new Vector3(0, 0.1f, -0.72f), 0.32f, white, 14);
            Disc(k, p + new Vector3(0, 0.2f, -0.74f), 0.1f, black, 10); Disc(k, p + new Vector3(0, 0.0f, -0.74f), 0.12f, black, 10);
            Disc(k, p + new Vector3(0, 0.2f, -0.76f), 0.05f, white, 8); Disc(k, p + new Vector3(0, 0.0f, -0.76f), 0.06f, white, 8);
            // two crossed cues over the windows
            var cue = Paint("#c8a165", 0.4f);
            Bar(k, f.At(f.Width / 2 - 2.5f, 5.0f) + f.Normal * 0.1f, f.At(f.Width / 2 + 2.5f, 6.8f) + f.Normal * 0.1f, 0.1f, cue);
            Bar(k, f.At(f.Width / 2 - 2.5f, 6.8f) + f.Normal * 0.12f, f.At(f.Width / 2 + 2.5f, 5.0f) + f.Normal * 0.12f, 0.1f, cue);
        }

        static void EscapeRoom(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.8f, T("wall_stone", new Color(0.6f, 0.58f, 0.55f)), "gf_store_dark", closed, season, Row(BuildingType.EscapeRoom), Paint("#3e2723"), 0.25f, 0.75f, -1, 3.2f);
            // a big brass padlock on the roof
            var brass = Paint("#c9a227", 0.7f, 0.9f); var steel = Paint("#b0b8c0", 0.7f, 0.9f);
            float z = z0 + 0.9f, y = 4.8f;
            k.Box(new Vector3(-1.1f, y, z - 0.35f), new Vector3(1.1f, y + 2.0f, z + 0.35f), brass, brass);
            k.Box(new Vector3(-0.85f, y + 2.0f, z - 0.15f), new Vector3(-0.55f, y + 3.1f, z + 0.15f), steel, steel);
            k.Box(new Vector3(0.55f, y + 2.0f, z - 0.15f), new Vector3(0.85f, y + 3.1f, z + 0.15f), steel, steel);
            k.Box(new Vector3(-0.85f, y + 3.1f, z - 0.15f), new Vector3(0.85f, y + 3.4f, z + 0.15f), steel, steel);
            var black = Paint("#111111");
            Disc(k, new Vector3(0, y + 1.3f, z - 0.37f), 0.22f, black, 10);
            TwoQuad(k, new Vector3(-0.18f, y + 0.6f, z - 0.37f), new Vector3(-0.08f, y + 1.25f, z - 0.37f), new Vector3(0.08f, y + 1.25f, z - 0.37f), new Vector3(0.18f, y + 0.6f, z - 0.37f), black);
            // torches either side of the door
            var flame = Bulb(closed, "#ff9100", 3f);
            foreach (float a in new[] { f.Width * 0.25f - 0.4f, f.Width * 0.75f + 0.4f })
            {
                var p = f.At(a, 2.2f) + f.Normal * 0.3f;
                k.Box(p + new Vector3(-0.05f, 0, 0), p + new Vector3(0.05f, 0.1f, 0.3f), black, black);
                k.Cylinder(p, 0.08f, 0.12f, 0.5f, 6, Paint("#4e342e"), null);
                k.Blob(p + Vector3.up * 0.65f, new Vector3(0.12f, 0.2f, 0.12f), H("#ff9100"));
                k.Box(p + new Vector3(-0.08f, 0.5f, -0.08f), p + new Vector3(0.08f, 0.62f, 0.08f), flame, flame);
            }
        }

        static void MiniGolf(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, GrassMat(season), 0.05f, 6f);
            // the hut where you pay and get a putter
            var hut = GableBox(k, -w / 2 + 0.8f, -w / 2 + 4.8f, -d / 2 + 0.8f, -d / 2 + 3.6f, 2.8f, 1.2f, T("wall_board", new Color(0.95f, 0.85f, 0.5f)), T("roof_shingle", new Color(0.75f, 0.25f, 0.2f)), season);
            Rect(k, hut[South], 0.8f, 3.2f, 1.0f, 2.2f, Glass(closed), Vector2.zero, new Vector2(0.5f, 1), 0.03f);
            Pylon(k, 1f, -d / 2 + 0.9f, 3.2f, 4.4f, Row(BuildingType.MiniGolf), closed);
            // the greens, edged in timber
            var border = T("wood", new Color(0.6f, 0.45f, 0.3f));
            var felt = season == 0 ? S("snow") : Paint("#2fae58", 0.1f);
            float[,] greens = { { -8.5f, -4.6f, -3f, -2.8f }, { -8.5f, -1.8f, -6.6f, 4.5f }, { -5.2f, -1f, 0.8f, 1f }, { 2f, -6.5f, 4f, 3f }, { 5f, -7.5f, 9f, -5.5f }, { -4.5f, 3f, 5.5f, 5f }, { 6f, -3.5f, 8.5f, 8.5f }, { -8.5f, 6f, 4.5f, 8.5f } };
            var black = Paint("#111111"); var flagC = Paint("#e53935"); var pole = Paint("#fafafa");
            for (int i = 0; i < greens.GetLength(0); i++)
            {
                float gx0 = greens[i, 0], gz0 = greens[i, 1], gx1 = greens[i, 2], gz1 = greens[i, 3];
                k.Box(new Vector3(gx0, 0.05f, gz0), new Vector3(gx1, 0.22f, gz1), border, border);
                k.Flat(gx0 + 0.12f, gz0 + 0.12f, gx1 - 0.12f, gz1 - 0.12f, 0.23f, felt, 3f);
                float hx = (gx1 - gx0 > gz1 - gz0) ? gx1 - 0.6f : (gx0 + gx1) / 2, hz = (gx1 - gx0 > gz1 - gz0) ? (gz0 + gz1) / 2 : gz1 - 0.6f;
                k.Flat(hx - 0.1f, hz - 0.1f, hx + 0.1f, hz + 0.1f, 0.235f, black, 1f);
                k.Box(new Vector3(hx - 0.02f, 0.23f, hz - 0.02f), new Vector3(hx + 0.02f, 1.2f, hz + 0.02f), pole, pole);
                Two(k, new Vector3(hx, 1.2f, hz), new Vector3(hx, 0.9f, hz), new Vector3(hx + 0.4f, 1.05f, hz), flagC);
            }
            // a windmill whose sails face the street
            float wx = -2.6f, wz = 6.2f;
            var boards = T("wall_board", new Color(0.92f, 0.9f, 0.82f));
            k.Cylinder(new Vector3(wx, 0.2f, wz), 1.3f, 0.9f, 4.2f, 8, boards, null);
            k.Cylinder(new Vector3(wx, 4.4f, wz), 1.1f, 0f, 1.4f, 8, Snowy(T("roof_shingle", new Color(0.75f, 0.25f, 0.2f)), season), null);
            var sail = Paint("#f4f4f2", 0.3f);
            var hub = new Vector3(wx, 3.6f, wz - 1.1f);
            k.Box(hub + new Vector3(-0.12f, -2.5f, -0.08f), hub + new Vector3(0.12f, 2.5f, 0f), border, border);
            k.Box(hub + new Vector3(-2.5f, -0.12f, -0.08f), hub + new Vector3(2.5f, 0.12f, 0f), border, border);
            k.Box(hub + new Vector3(0.12f, 0.5f, -0.06f), hub + new Vector3(0.7f, 2.5f, -0.02f), sail, sail);
            k.Box(hub + new Vector3(-0.7f, -2.5f, -0.06f), hub + new Vector3(-0.12f, -0.5f, -0.02f), sail, sail);
            k.Box(hub + new Vector3(0.5f, -0.7f, -0.06f), hub + new Vector3(2.5f, -0.12f, -0.02f), sail, sail);
            k.Box(hub + new Vector3(-2.5f, 0.12f, -0.06f), hub + new Vector3(-0.5f, 0.7f, -0.02f), sail, sail);
            // a little castle and a giant ball on a tee
            var stone = T("wall_stone", new Color(0.75f, 0.72f, 0.68f));
            k.Box(new Vector3(6.2f, 0.2f, 4f), new Vector3(8.3f, 2.2f, 6f), stone, stone);
            for (int i = 0; i < 4; i++) k.Box(new Vector3(6.2f + i * 0.6f, 2.2f, 4f), new Vector3(6.5f + i * 0.6f, 2.6f, 4.3f), stone, stone);
            k.Box(new Vector3(6.9f, 0.2f, 3.95f), new Vector3(7.6f, 1.2f, 4.0f), black, black);
            k.Cylinder(new Vector3(8.6f, 0.06f, -d / 2 + 1.2f), 0.12f, 0.35f, 1.4f, 8, Paint("#fafafa"), Paint("#fafafa"));
            k.Blob(new Vector3(8.6f, 2.1f, -d / 2 + 1.2f), new Vector3(0.7f, 0.7f, 0.7f), H("#fafafa"));
            Fence(k, -w / 2 + 0.4f, -d / 2 + 0.4f, w / 2 - 0.4f, d / 2 - 0.4f, 1.0f, T("wood", new Color(0.92f, 0.92f, 0.9f)), 2.4f);
        }

        static void SkatingRink(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -1f, z1 = d / 2 - 0.6f;
            var faces = Faces(x0, x1, z0, z1);
            var wall = T("metal_panel", new Color(0.78f, 0.86f, 0.95f));
            foreach (var fc in faces) Plain(k, fc, 0, 4.5f, wall, 2.5f);
            Vault(k, x0 - 0.3f, x1 + 0.3f, z0 - 0.3f, z1 + 0.3f, 4.5f, 4f, Snowy(T("roof_metal", new Color(0.85f, 0.88f, 0.92f)), season), wall);
            var f = faces[South];
            Rect(k, f, f.Width / 2 - 3f, f.Width / 2 + 3f, 0.1f, 3.2f, Glass(closed), Vector2.zero, new Vector2(1, 1), 0.03f);
            Sign(k, f, f.Width / 2, 7f, 3.45f, 0.75f, Row(BuildingType.SkatingRink), closed);
            // the outdoor rink: ice in winter, a roller floor the rest of the year
            float rx0 = -8.5f, rx1 = 8.5f, rz0 = -d / 2 + 1.2f, rz1 = -3f;
            k.Flat(rx0, rz0, rx1, rz1, 0.08f, season == 0 ? Paint("#e3f2fd", 0.95f) : Paint("#c49a6c", 0.5f), 4f);
            var boards = Paint("#f4f4f2", 0.4f); var trim = Paint("#1565c0", 0.4f);
            k.Box(new Vector3(rx0 - 0.15f, 0.06f, rz0 - 0.15f), new Vector3(rx1 + 0.15f, 1.1f, rz0), boards, trim);
            k.Box(new Vector3(rx0 - 0.15f, 0.06f, rz0), new Vector3(rx0, 1.1f, rz1), boards, trim);
            k.Box(new Vector3(rx1, 0.06f, rz0), new Vector3(rx1 + 0.15f, 1.1f, rz1), boards, trim);
            k.Box(new Vector3(rx0 - 0.15f, 0.06f, rz1), new Vector3(-1f, 1.1f, rz1 + 0.15f), boards, trim);
            k.Box(new Vector3(1f, 0.06f, rz1), new Vector3(rx1 + 0.15f, 1.1f, rz1 + 0.15f), boards, trim);
            // floodlights at the corners
            var steel = Paint("#8d969e", 0.5f, 0.6f); var lamp = Bulb(closed, "#fffde7", 3f);
            foreach (float x in new[] { rx0 - 0.6f, rx1 + 0.6f })
            {
                k.Box(new Vector3(x - 0.08f, 0.06f, rz0 - 0.08f), new Vector3(x + 0.08f, 6f, rz0 + 0.08f), steel, steel);
                k.Box(new Vector3(x - 0.5f, 6f, rz0 - 0.2f), new Vector3(x + 0.5f, 6.4f, rz0 + 0.2f), steel, lamp);
            }
        }

        static void Theater(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x0 = -w / 2 + 1f, x1 = w / 2 - 1f, z0 = -d / 2 + 5f, z1 = d / 2 - 0.6f;
            var stone = T("wall_stone", new Color(0.92f, 0.88f, 0.8f));
            var faces = FlatBox(k, x0, x1, z0, z1, 12f, stone, Paint("#e8e0cc", 0.3f), season, 0.8f);
            var f = faces[South];
            foreach (float a in new[] { f.Width / 2 - 3f, f.Width / 2, f.Width / 2 + 3f }) Door(k, f, a, H("#8e1b1b"), 1.6f, 3.0f);
            var red = Paint("#8e1b1b", 0.3f);
            Ledge(k, f, 0.6f, 1.8f, 4f, 10.8f, 0.1f, red); Ledge(k, f, f.Width - 1.8f, f.Width - 0.6f, 4f, 10.8f, 0.1f, red);
            // the portico: steps, six columns, the entablature with the name on it, a pediment
            var white = Paint("#f2ede2", 0.35f);
            float pz0 = -d / 2 + 1.6f, pz1 = z0;
            for (int i = 0; i < 3; i++) k.Box(new Vector3(-6.6f, 0.05f, pz0 - 0.9f + i * 0.3f), new Vector3(6.6f, 0.2f + i * 0.15f, pz1), white, white);
            for (int i = 0; i < 6; i++)
            {
                float x = -5.5f + i * 2.2f;
                k.Cylinder(new Vector3(x, 0.5f, pz0 + 0.6f), 0.42f, 0.36f, 7f, 12, white, white);
                k.Box(new Vector3(x - 0.5f, 7.5f - 0.3f, pz0 + 0.1f), new Vector3(x + 0.5f, 7.5f, pz0 + 1.1f), white, white);
            }
            k.Box(new Vector3(-6.4f, 7.5f, pz0), new Vector3(6.4f, 8.5f, pz1), white, white);
            var front = new Face { LeftBottom = new Vector3(-6.4f, 0, pz0 - 0.01f), Right = Vector3.right, Normal = Vector3.back, Width = 12.8f };
            Sign(k, front, 6.4f, 8f, 7.65f, 0.75f, Row(BuildingType.Theater), closed);
            k.Push(new Vector3(0, 0, (pz0 + pz1) / 2), 90);
            k.Gable(-(pz1 - pz0) / 2 - 0.1f, -6.6f, (pz1 - pz0) / 2, 6.6f, 8.5f, 2.2f, Snowy(T("roof_metal", new Color(0.5f, 0.6f, 0.58f)), season), white, 0.2f);
            k.Pop();
            foreach (float x in new[] { -7.6f, 7.6f }) PropArt.Lamp(k, new Vector3(x, 0.06f, -d / 2 + 1f));
        }

        static void ComedyClub(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 5.4f, S("wall_brick_red"), "gf_store_dark", closed, season, -1, Paint("#212121"), 0.2f, 0.8f, -1, 3.2f);
            // a marquee over the door, ringed with bulbs
            var red = Paint("#b71c1c");
            float my = 3.4f, mx0 = -w / 2 + 1.4f, mx1 = w / 2 - 1.4f;
            k.Box(new Vector3(mx0, my, z0 - 1.6f), new Vector3(mx1, my + 0.25f, z0), red, Snowy(red, season));
            k.Box(new Vector3(mx0, my + 0.25f, z0 - 1.6f), new Vector3(mx1, my + 1.25f, z0 - 1.4f), red, red);
            var mf = new Face { LeftBottom = new Vector3(mx0, 0, z0 - 1.6f), Right = Vector3.right, Normal = Vector3.back, Width = mx1 - mx0 };
            Sign(k, mf, mf.Width / 2, mf.Width - 0.6f, my + 0.4f, 0.7f, Row(BuildingType.ComedyClub), closed);
            var bulb = Bulb(closed, "#ffe082", 3.5f);
            for (float x = mx0 + 0.15f; x < mx1; x += 0.35f)
            {
                k.Box(new Vector3(x - 0.05f, my + 0.08f, z0 - 1.68f), new Vector3(x + 0.05f, my + 0.2f, z0 - 1.6f), bulb, bulb);
                k.Box(new Vector3(x - 0.05f, my + 1.25f, z0 - 1.6f), new Vector3(x + 0.05f, my + 1.37f, z0 - 1.48f), bulb, bulb);
            }
            // a neon laughing face on the parapet
            var c = new Vector3(0, 7.4f, z0 - 0.1f);
            var neon = Bulb(closed, "#ffeb3b", 2.6f);
            k.Box(new Vector3(-1.3f, 5.9f, z0 + 0.0f), new Vector3(1.3f, 8.9f, z0 + 0.15f), Paint("#1b1b1b"), Paint("#1b1b1b"));
            Ring(k, c, 1.1f, 0, Mathf.PI * 2, 28, 0.06f, neon);
            Ring(k, c + new Vector3(0, -0.1f, 0), 0.6f, Mathf.PI * 1.1f, Mathf.PI * 1.9f, 10, 0.06f, neon);
            Ring(k, c + new Vector3(-0.38f, 0.35f, 0), 0.15f, 0, Mathf.PI, 5, 0.05f, neon);
            Ring(k, c + new Vector3(0.38f, 0.35f, 0), 0.15f, 0, Mathf.PI, 5, 0.05f, neon);
        }

        // ================================================================== health
        static void Dentist(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var f = Storefront(k, -w / 2 + 0.6f, w / 2 - 0.6f, -d / 2 + 3.4f, d / 2 - 0.6f, 4.4f, T("wall_stucco", new Color(0.97f, 0.98f, 1f)), "gf_lobby", closed, season, Row(BuildingType.Dentist), Paint("#29b6f6"), 0.2f, 0.8f, -1, 3.2f);
            Ledge(k, f, 0, f.Width, 0, 0.5f, 0.05f, Paint("#29b6f6"));
            // a giant tooth on a post by the path
            float tx = w / 2 - 1.4f, tz = -d / 2 + 1.4f;
            var post = Paint("#90a4ae", 0.5f, 0.6f); var white = Paint("#fbfbfb", 0.6f);
            k.Box(new Vector3(tx - 0.08f, 0.06f, tz - 0.08f), new Vector3(tx + 0.08f, 2.4f, tz + 0.08f), post, post);
            k.Cylinder(new Vector3(tx - 0.35f, 2.2f, tz), 0.06f, 0.3f, 0.9f, 8, white, null);
            k.Cylinder(new Vector3(tx + 0.35f, 2.2f, tz), 0.06f, 0.3f, 0.9f, 8, white, null);
            k.Blob(new Vector3(tx, 3.3f, tz), new Vector3(0.75f, 0.55f, 0.55f), H("#fbfbfb"));
            k.Blob(new Vector3(tx - 0.35f, 3.72f, tz), new Vector3(0.36f, 0.32f, 0.42f), H("#fbfbfb"));
            k.Blob(new Vector3(tx + 0.35f, 3.72f, tz), new Vector3(0.36f, 0.32f, 0.42f), H("#fbfbfb"));
            Planter(k, -w / 2 + 1.4f, -d / 2 + 1.6f, season);
            PropArt.Bench(k, new Vector3(-0.6f, 0.06f, -d / 2 + 2.2f), 180);
        }

        static void Optician(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.6f, T("wall_stucco", new Color(0.9f, 0.93f, 0.96f)), "gf_store_light", closed, season, Row(BuildingType.Optician), Paint("#37474f"));
            // giant spectacles up on the roof
            var frame = Paint("#212121", 0.7f); var lens = Paint("#b3e5fc", 0.95f, 0.1f);
            float y = 5.7f, z = z0 - 0.1f;
            foreach (float cx in new[] { -1.15f, 1.15f })
            {
                k.Box(new Vector3(cx - 0.8f, y, z - 0.08f), new Vector3(cx + 0.8f, y + 0.12f, z), frame, frame);
                k.Box(new Vector3(cx - 0.8f, y + 1.0f, z - 0.08f), new Vector3(cx + 0.8f, y + 1.12f, z), frame, frame);
                k.Box(new Vector3(cx - 0.8f, y, z - 0.08f), new Vector3(cx - 0.68f, y + 1.12f, z), frame, frame);
                k.Box(new Vector3(cx + 0.68f, y, z - 0.08f), new Vector3(cx + 0.8f, y + 1.12f, z), frame, frame);
                k.Box(new Vector3(cx - 0.68f, y + 0.12f, z - 0.05f), new Vector3(cx + 0.68f, y + 1.0f, z - 0.03f), lens, lens);
                k.Box(new Vector3(cx - 0.05f, 5.1f, z + 0.02f), new Vector3(cx + 0.05f, y, z + 0.12f), frame, frame);
            }
            k.Box(new Vector3(-0.35f, y + 0.72f, z - 0.08f), new Vector3(0.35f, y + 0.84f, z), frame, frame);
            k.Box(new Vector3(-1.95f, y + 0.9f, z), new Vector3(-1.85f, y + 1.0f, z + 1.6f), frame, frame);
            k.Box(new Vector3(1.85f, y + 0.9f, z), new Vector3(1.95f, y + 1.0f, z + 1.6f), frame, frame);
        }

        static void VetClinic(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x1 = 1.6f;
            Storefront(k, -w / 2 + 0.5f, x1, -d / 2 + 3f, d / 2 - 0.5f, 4.2f, T("wall_siding", new Color(0.85f, 0.93f, 0.85f)), "gf_lobby", closed, season, Row(BuildingType.VetClinic), Paint("#2e7d32"), 0.15f, 0.85f, -1, 3.0f);
            // a fenced run for dogs with a kennel and a dog in it
            float rx0 = 2.1f, rx1 = w / 2 - 0.4f, rz0 = -d / 2 + 1.2f, rz1 = d / 2 - 0.6f;
            k.Flat(rx0, rz0, rx1, rz1, 0.07f, GrassMat(season), 3f);
            Fence(k, rx0, rz0, rx1, rz1, 1.2f, Paint("#9e9e9e", 0.5f, 0.6f), 0.01f);
            var kennel = GableBox(k, rx0 + 0.5f, rx0 + 1.8f, rz1 - 1.6f, rz1 - 0.4f, 0.8f, 0.5f, T("wall_board", new Color(0.8f, 0.3f, 0.2f)), T("roof_shingle", new Color(0.4f, 0.4f, 0.42f)), season, 0.15f);
            Rect(k, kennel[South], 0.4f, 0.9f, 0, 0.6f, Paint("#1b1b1b"), Vector2.zero, Vector2.one, 0.02f);
            var dogC = H("#a1662f");
            k.Blob(new Vector3(rx1 - 1.2f, 0.45f, -0.5f), new Vector3(0.45f, 0.22f, 0.2f), dogC);
            k.Blob(new Vector3(rx1 - 0.7f, 0.7f, -0.5f), new Vector3(0.17f, 0.17f, 0.15f), dogC);
            foreach (float dx in new[] { -0.3f, 0.25f })
                foreach (float dz in new[] { -0.1f, 0.1f })
                    k.Box(new Vector3(rx1 - 1.2f + dx - 0.04f, 0.07f, -0.5f + dz - 0.04f), new Vector3(rx1 - 1.2f + dx + 0.04f, 0.4f, -0.5f + dz + 0.04f), C(dogC), C(dogC));
            // a paw print on a post by the street
            var paw = Paint("#2e7d32"); var post = Paint("#3a3e44", 0.4f, 0.4f);
            var c = new Vector3(-w / 2 + 1.4f, 2.4f, -d / 2 + 1.0f);
            k.Box(new Vector3(c.x - 0.06f, 0.06f, c.z + 0.02f), new Vector3(c.x + 0.06f, c.y + 0.6f, c.z + 0.14f), post, post);
            Disc(k, c, 0.45f, paw);
            Disc(k, c + new Vector3(-0.55f, 0.5f, 0), 0.18f, paw, 10); Disc(k, c + new Vector3(-0.2f, 0.75f, 0), 0.18f, paw, 10);
            Disc(k, c + new Vector3(0.2f, 0.75f, 0), 0.18f, paw, 10); Disc(k, c + new Vector3(0.55f, 0.5f, 0), 0.18f, paw, 10);
        }

        static void Physio(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            var skin = new Skin { Upper = "fac_clinic", Floor = 3.15f, Ground = "gf_lobby", GroundH = 4.2f, Wall = "wall_stucco", Coping = H("#26a69a"), Glow = 1.4f };
            float x0 = -w / 2 + 0.6f, x1 = w / 2 - 0.6f, z0 = -d / 2 + 3.6f, z1 = d / 2 - 0.6f;
            Block(k, x0, x1, z0, z1, skin, 1, (int)(Next(ref s) % 4), 0, true, true, "roof_membrane", season, closed);
            var f = Faces(x0, x1, z0, z1)[South];
            Ledge(k, f, 0, f.Width, 3.9f, 4.6f, 0.25f, Paint("#26a69a"));
            Sign(k, Out(f, 0.25f), f.Width / 2, 5f, 3.98f, 0.52f, Row(BuildingType.PhysioClinic), closed);
            // a ramp up to a landing at the door, with handrails
            var conc = Paint("#cfcac0", 0.2f); var rail = Paint("#b0bec5", 0.6f, 0.8f);
            float zA = z0 - 1.5f, zB = z0;
            k.Quad(new Vector3(-4f, 0.06f, zA), new Vector3(-4f, 0.06f, zB), new Vector3(0f, 0.45f, zB), new Vector3(0f, 0.45f, zA), conc);
            k.Tri(new Vector3(-4f, 0.06f, zA), new Vector3(0f, 0.45f, zA), new Vector3(0f, 0.06f, zA), conc);
            k.Box(new Vector3(0f, 0.06f, zA), new Vector3(2.2f, 0.45f, zB), conc, conc);
            Bar(k, new Vector3(-4f, 0.96f, zA), new Vector3(0f, 1.35f, zA), 0.06f, rail);
            k.Box(new Vector3(0f, 1.32f, zA - 0.03f), new Vector3(2.2f, 1.38f, zA + 0.03f), rail, rail);
            foreach (float x in new[] { -4f, -2f, 0f, 2.2f })
            {
                float y = x <= 0 ? 0.06f + (x + 4f) / 4f * 0.39f : 0.45f;
                k.Box(new Vector3(x - 0.03f, y, zA - 0.03f), new Vector3(x + 0.03f, y + 0.9f, zA + 0.03f), rail, rail);
            }
            Planter(k, 3.6f, -d / 2 + 1.2f, season);
            PropArt.Bench(k, new Vector3(-2f, 0.06f, -d / 2 + 0.9f), 180);
        }

        // ================================================================== household
        static void Laundromat(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 5.0f, T("wall_stucco", new Color(0.75f, 0.86f, 0.98f)), "gf_store_light", closed, season, -1, Paint("#1565c0"), 0.06f, 0.94f, -1, 3.2f);
            Sign(k, f, f.Width / 2, 4.2f, 3.6f, 0.75f, Row(BuildingType.Laundromat), closed);
            // washer doors along the fascia either side of the sign
            var white = Paint("#fafafa", 0.6f); var door = closed ? Paint("#37474f", 0.9f) : Lit(H("#81d4fa"), 1.2f);
            foreach (float a in new[] { 0.6f, 1.55f, f.Width - 1.55f, f.Width - 0.6f })
            {
                var c = f.At(a, 4.0f) + f.Normal * 0.04f;
                Disc(k, c, 0.42f, white);
                Disc(k, c + f.Normal * 0.02f, 0.3f, door);
            }
            // a basket of washing and a bench out front
            PropArt.Bench(k, f.At(f.Width - 1.8f, 0.02f) + f.Normal * 1.0f, 180);
            k.Box(f.At(1.0f, 0.02f) + f.Normal * 0.7f + new Vector3(-0.35f, 0, -0.25f), f.At(1.0f, 0.45f) + f.Normal * 0.7f + new Vector3(0.35f, 0, 0.25f), Paint("#1e88e5"), Paint("#fafafa"));
        }

        static void DryCleaner(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.8f, S("wall_brick_tan"), "gf_store_light", closed, season, Row(BuildingType.DryCleaner), Paint("#0d47a1"));
            Awning(k, f, 0.4f, f.Width - 0.4f, 3.85f, 1.1f, H("#0d47a1"));
            // a giant coat hanger on the roof, a shirt on it
            var steel = Paint("#b0b8c0", 0.7f, 0.9f);
            var c = new Vector3(0, 8.6f, z0 + 0.3f);
            Bar(k, c + new Vector3(0, -0.6f, 0), c, 0.08f, steel);
            Bar(k, c, c + new Vector3(0.3f, 0.2f, 0), 0.08f, steel);
            Bar(k, c + new Vector3(0.3f, 0.2f, 0), c + new Vector3(0.2f, 0.45f, 0), 0.08f, steel);
            Bar(k, c + new Vector3(0, -0.6f, 0), c + new Vector3(-1.6f, -1.4f, 0), 0.1f, steel);
            Bar(k, c + new Vector3(0, -0.6f, 0), c + new Vector3(1.6f, -1.4f, 0), 0.1f, steel);
            Bar(k, c + new Vector3(-1.6f, -1.4f, 0), c + new Vector3(1.6f, -1.4f, 0), 0.1f, steel);
            var shirt = Paint("#bbdefb", 0.3f);
            k.Box(c + new Vector3(-0.85f, -3.1f, -0.06f), c + new Vector3(0.85f, -1.2f, 0.06f), shirt, shirt);
            k.Box(c + new Vector3(-1.5f, -1.9f, -0.05f), c + new Vector3(-0.85f, -1.25f, 0.05f), shirt, shirt);
            k.Box(c + new Vector3(0.85f, -1.9f, -0.05f), c + new Vector3(1.5f, -1.25f, 0.05f), shirt, shirt);
            var collar = Paint("#fafafa");
            Two(k, c + new Vector3(-0.35f, -1.15f, -0.08f), c + new Vector3(0.35f, -1.15f, -0.08f), c + new Vector3(0, -1.6f, -0.08f), collar);
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(new Vector3(-0.05f, 5.3f, z0 + 0.4f), new Vector3(0.05f, 8.0f, z0 + 0.5f), iron, iron);
        }

        static void Tailor(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_cream", "gf_store_wood", 2, ref s, closed, season, out top);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, Paint("#4e342e"));
            Sign(k, Out(f, 0.3f), f.Width / 2, 5f, 3.62f, 0.55f, Row(BuildingType.Tailor), closed);
            // a spool of thread, a needle through it, hung on a bracket
            var p = f.At(f.Width - 0.9f, 4.7f) + f.Normal * 1.0f;
            var wood = T("wood", new Color(0.78f, 0.6f, 0.42f)); var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(p + new Vector3(-0.04f, 1.5f, 0), p + new Vector3(0.04f, 1.6f, 1.0f), iron, iron);
            k.Box(p + new Vector3(-0.02f, 1.3f, -0.02f), p + new Vector3(0.02f, 1.5f, 0.02f), iron, iron);
            k.Cylinder(p, 0.5f, 0.5f, 0.14f, 12, wood, wood);
            k.Cylinder(p + Vector3.up * 0.14f, 0.36f, 0.36f, 0.92f, 12, Paint("#c62828", 0.3f), null);
            k.Cylinder(p + Vector3.up * 1.06f, 0.5f, 0.5f, 0.14f, 12, wood, wood);
            Bar(k, p + new Vector3(-0.9f, 0.1f, -0.55f), p + new Vector3(0.8f, 1.3f, -0.55f), 0.06f, Paint("#cfd8dc", 0.8f, 0.9f));
            // a dressmaker's dummy by the door
            var dm = f.At(0.9f, 0.02f) + f.Normal * 0.6f;
            k.Cylinder(dm, 0.25f, 0.25f, 0.05f, 8, iron, iron);
            k.Box(dm + new Vector3(-0.03f, 0, -0.03f), dm + new Vector3(0.03f, 1.0f, 0.03f), iron, iron);
            k.Blob(dm + Vector3.up * 1.4f, new Vector3(0.3f, 0.45f, 0.22f), H("#e0c9a6"));
        }

        static void PhoneRepair(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.4f, T("wall_stucco", new Color(0.2f, 0.3f, 0.5f)), "gf_store_light", closed, season, Row(BuildingType.PhoneRepair), Paint("#00e5ff"));
            Neon(k, f, 0.3f, f.Width - 0.3f, 0.25f, H("#00e5ff"), closed, 0.06f);
            // a giant phone on the roof, its screen cracked
            float z = z0 + 0.5f, y = 4.9f;
            var black = Paint("#1b1b1b", 0.8f, 0.3f);
            k.Box(new Vector3(-0.85f, y, z - 0.15f), new Vector3(0.85f, y + 3.0f, z + 0.15f), black, black);
            var scr = closed ? Paint("#263238", 0.9f) : Lit(H("#4fc3f7"), 1.3f);
            k.Box(new Vector3(-0.72f, y + 0.25f, z - 0.17f), new Vector3(0.72f, y + 2.75f, z - 0.15f), scr, scr);
            string[] apps = { "#ff5252", "#ffd740", "#69f0ae", "#448aff", "#e040fb", "#ff6e40" };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    var m = Paint(apps[(r * 3 + c) % 6], 0.5f);
                    k.Box(new Vector3(-0.6f + c * 0.42f, y + 2.25f - r * 0.42f, z - 0.19f), new Vector3(-0.32f + c * 0.42f, y + 2.53f - r * 0.42f, z - 0.17f), m, m);
                }
            var crack = Paint("#ffffff", 0.6f);
            float cz = z - 0.2f;
            Bar(k, new Vector3(-0.72f, y + 1.7f, cz), new Vector3(-0.1f, y + 1.3f, cz), 0.04f, crack);
            Bar(k, new Vector3(-0.1f, y + 1.3f, cz), new Vector3(0.3f, y + 1.55f, cz), 0.04f, crack);
            Bar(k, new Vector3(0.3f, y + 1.55f, cz), new Vector3(0.72f, y + 0.9f, cz), 0.04f, crack);
            Bar(k, new Vector3(-0.1f, y + 1.3f, cz), new Vector3(0.05f, y + 0.5f, cz), 0.04f, crack);
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(new Vector3(-0.06f, 4.4f, z + 0.15f), new Vector3(0.06f, y + 2.4f, z + 0.9f), iron, iron);
        }

        static void CarWash(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("concrete"), 0.05f, 4f);
            float x0 = -4.5f, x1 = 4.5f, z0 = -d / 2 + 3.2f, z1 = d / 2 - 0.6f, zc = (z0 + z1) / 2;
            var wall = T("wall_stucco", new Color(0.95f, 0.97f, 1f));
            var faces = FlatBox(k, x0, x1, z0, z1, 4.6f, wall, Paint("#1565c0"), season);
            var f = faces[South];
            Rect(k, f, 1.0f, f.Width - 1.0f, 1.4f, 2.6f, Glass(closed), Vector2.zero, new Vector2(2, 1), 0.03f);
            Ledge(k, f, 0, f.Width, 2.9f, 3.2f, 0.06f, Paint("#29b6f6"));
            Sign(k, f, f.Width / 2, 6.5f, 3.45f, 0.75f, Row(BuildingType.CarWash), closed);
            // the tunnel mouths, a gantry of brushes at the way in and a clean car coming out
            foreach (int side in new[] { West, East })
            {
                var e = faces[side];
                Rect(k, e, e.Width / 2 - 1.6f, e.Width / 2 + 1.6f, 0, 3.2f, Paint("#1a1f26"), Vector2.zero, Vector2.one, 0.02f);
            }
            var steel = Paint("#b0bec5", 0.6f, 0.7f); var brush = Paint("#1e88e5", 0.1f);
            float gx = x0 - 1.2f;
            foreach (float z in new[] { zc - 1.9f, zc + 1.9f }) k.Box(new Vector3(gx - 0.1f, 0.06f, z - 0.1f), new Vector3(gx + 0.1f, 3.6f, z + 0.1f), steel, steel);
            k.Box(new Vector3(gx - 0.15f, 3.6f, zc - 2.0f), new Vector3(gx + 0.15f, 3.9f, zc + 2.0f), steel, steel);
            foreach (float z in new[] { zc - 1.3f, zc + 1.3f }) k.Cylinder(new Vector3(gx, 0.3f, z), 0.4f, 0.4f, 3.2f, 10, brush, brush);
            k.Push(new Vector3(x1 + 2.4f, 0.06f, zc), 90);
            PropArt.Car(k, H("#c62828"));
            k.Pop();
            // vacuum posts
            var yellow = Paint("#fdd835", 0.5f);
            foreach (float x in new[] { -8.5f, -6.8f })
            {
                k.Box(new Vector3(x - 0.3f, 0.06f, -d / 2 + 1f), new Vector3(x + 0.3f, 1.4f, -d / 2 + 1.5f), yellow, yellow);
                Bar(k, new Vector3(x, 1.4f, -d / 2 + 1.25f), new Vector3(x + 0.6f, 2.2f, -d / 2 + 1.25f), 0.08f, Paint("#212121"));
                Bar(k, new Vector3(x + 0.6f, 2.2f, -d / 2 + 1.25f), new Vector3(x + 1.0f, 0.8f, -d / 2 + 1.25f), 0.08f, Paint("#212121"));
            }
        }

        // ================================================================== children and lessons
        static void Daycare(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("pavers"), 0.05f, 3f);
            float x0 = -w / 2 + 0.6f, x1 = 1.5f, z0 = -d / 2 + 2.4f, z1 = d / 2 - 0.6f;
            var faces = GableBox(k, x0, x1, z0, z1, 3.8f, 1.8f, T("wall_stucco", new Color(1f, 0.95f, 0.75f)), T("roof_shingle", new Color(0.85f, 0.3f, 0.25f)), season);
            var f = faces[South];
            string[] prim = { "#e53935", "#1e88e5", "#fdd835", "#43a047" };
            var pane = MaterialLibrary.Pane(closed ? new Color32(26, 30, 38, 255) : new Color32(255, 236, 190, 255));
            int n = 0;
            for (float a = 0.8f; a < f.Width - 1.5f; a += 2.2f)
            {
                if (Mathf.Abs(a + 0.7f - f.Width / 2) < 1.4f) continue;
                Ledge(k, f, a - 0.15f, a + 1.55f, 0.85f, 2.75f, 0.06f, Paint(prim[n++ % 4]));
                Rect(k, f, a, a + 1.4f, 1.0f, 2.6f, pane, Vector2.zero, Vector2.one, 0.08f);
            }
            Door(k, f, f.Width / 2, H("#1e88e5"), 1.4f, 2.3f);
            Sign(k, f, f.Width / 2, 4.6f, 2.95f, 0.55f, Row(BuildingType.Daycare), closed);
            // the playground: a soft floor, a fence, a slide, swings and a sandpit
            float px0 = 2.1f, px1 = w / 2 - 0.5f, pz0 = -d / 2 + 0.6f, pz1 = d / 2 - 0.6f;
            k.Flat(px0, pz0, px1, pz1, 0.07f, season == 0 ? S("snow") : Paint("#4caf50", 0.1f), 3f);
            Fence(k, px0, pz0, px1, pz1, 1.1f, Paint("#f4f4f2"), 0.01f);
            var red = Paint("#e53935", 0.5f); var blue = Paint("#1e88e5", 0.5f); var yel = Paint("#fdd835", 0.5f);
            float sx = px0 + 1.3f, sz = pz1 - 1.4f;
            foreach (float dx in new[] { -0.5f, 0.45f })
                foreach (float dz in new[] { -0.5f, 0.45f })
                    k.Box(new Vector3(sx + dx, 0.07f, sz + dz), new Vector3(sx + dx + 0.07f, 2.4f, sz + dz + 0.07f), yel, yel);
            k.Box(new Vector3(sx - 0.55f, 1.4f, sz - 0.55f), new Vector3(sx + 0.55f, 1.5f, sz + 0.55f), blue, blue);
            k.Cylinder(new Vector3(sx, 2.4f, sz), 0.85f, 0f, 0.7f, 4, red, red);
            TwoQuad(k, new Vector3(sx - 0.3f, 1.5f, sz - 0.55f), new Vector3(sx + 0.3f, 1.5f, sz - 0.55f), new Vector3(sx + 0.3f, 0.2f, sz - 2.8f), new Vector3(sx - 0.3f, 0.2f, sz - 2.8f), red);
            float wx = px1 - 2.6f, wz = 1.6f;
            foreach (float x in new[] { wx - 1.1f, wx + 1.1f }) k.Box(new Vector3(x - 0.06f, 0.07f, wz - 0.06f), new Vector3(x + 0.06f, 2.2f, wz + 0.06f), blue, blue);
            k.Box(new Vector3(wx - 1.2f, 2.2f, wz - 0.07f), new Vector3(wx + 1.2f, 2.32f, wz + 0.07f), blue, blue);
            foreach (float x in new[] { wx - 0.45f, wx + 0.45f })
            {
                var chain = Paint("#9e9e9e", 0.6f, 0.8f);
                k.Box(new Vector3(x - 0.22f, 0.6f, wz - 0.01f), new Vector3(x - 0.2f, 2.2f, wz + 0.01f), chain, chain);
                k.Box(new Vector3(x + 0.2f, 0.6f, wz - 0.01f), new Vector3(x + 0.22f, 2.2f, wz + 0.01f), chain, chain);
                k.Box(new Vector3(x - 0.25f, 0.55f, wz - 0.12f), new Vector3(x + 0.25f, 0.62f, wz + 0.12f), red, red);
            }
            var wood = T("wood", new Color(0.75f, 0.6f, 0.42f));
            k.Box(new Vector3(px1 - 2.6f, 0.07f, pz0 + 0.8f), new Vector3(px1 - 0.8f, 0.35f, pz0 + 2.6f), wood, T("dirt", new Color(0.95f, 0.85f, 0.6f)));
        }

        static void MusicSchool(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_civic_brick", "gf_store_wood", 2, ref s, closed, season, out top);
            // piano keys along the fascia, the name sign over them
            var white = Paint("#fafafa", 0.6f); var black = Paint("#111111", 0.8f);
            int n = Mathf.FloorToInt(f.Width / 0.42f); float kw = f.Width / n;
            for (int i = 0; i < n; i++) Ledge(k, f, i * kw + 0.02f, (i + 1) * kw - 0.02f, 3.4f, 4.3f, 0.25f, white);
            for (int i = 0; i < n - 1; i++)
            {
                int m = i % 7;
                if (m == 2 || m == 6) continue;
                Ledge(k, f, (i + 1) * kw - kw * 0.3f, (i + 1) * kw + kw * 0.3f, 3.85f, 4.3f, 0.32f, black);
            }
            // the name on the roof, a big quaver beside it
            float z = -d / 2 + 2.3f;
            RoofSign(k, -1.7f, z, top, 4.6f, Row(BuildingType.MusicSchool), closed);
            var note = Paint("#212121", 0.7f);
            k.Blob(new Vector3(1.4f, top + 0.6f, z + 0.4f), new Vector3(0.5f, 0.38f, 0.2f), H("#212121"));
            k.Blob(new Vector3(3.2f, top + 0.9f, z + 0.4f), new Vector3(0.5f, 0.38f, 0.2f), H("#212121"));
            k.Box(new Vector3(1.8f, top + 0.6f, z + 0.32f), new Vector3(1.92f, top + 3.0f, z + 0.48f), note, note);
            k.Box(new Vector3(3.6f, top + 0.9f, z + 0.32f), new Vector3(3.72f, top + 3.3f, z + 0.48f), note, note);
            Bar(k, new Vector3(1.8f, top + 2.85f, z + 0.4f), new Vector3(3.72f, top + 3.15f, z + 0.4f), 0.32f, note);
        }

        static void DanceStudio(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float top;
            var f = ShopBlock(k, -w / 2 + 0.4f, w / 2 - 0.4f, -d / 2 + 2.4f, d / 2 - 0.4f, "fac_apt_cream", "gf_store_light", 2, ref s, closed, season, out top);
            Ledge(k, f, 0, f.Width, 3.5f, 4.3f, 0.3f, Paint("#ad1457"));
            Sign(k, Out(f, 0.3f), f.Width / 2, 5f, 3.62f, 0.55f, Row(BuildingType.DanceStudio), closed);
            Neon(k, Out(f, 0.3f), 0.2f, f.Width - 0.2f, 3.42f, H("#ff80ab"), closed, 0.06f);
            // a disco ball hung from a bracket, ballet shoes painted up the side
            var p = f.At(0.9f, 5.2f) + f.Normal * 1.1f;
            var iron = Paint("#3a3e44", 0.4f, 0.4f);
            k.Box(p + new Vector3(-0.04f, 1.2f, 0), p + new Vector3(0.04f, 1.3f, 1.1f), iron, iron);
            k.Box(p + new Vector3(-0.02f, 0.5f, -0.02f), p + new Vector3(0.02f, 1.2f, 0.02f), iron, iron);
            k.Blob(p, new Vector3(0.5f, 0.5f, 0.5f), H("#e0e0e0"));
            if (!closed)
                for (int i = 0; i < 8; i++)
                {
                    var q = p + new Vector3(Mathf.Cos(i * 0.8f) * 0.45f, Mathf.Sin(i * 1.7f) * 0.35f, -0.25f);
                    k.Box(q - new Vector3(0.06f, 0.06f, 0.06f), q + new Vector3(0.06f, 0.06f, 0.06f), Lit(H("#ffffff"), 4f), Lit(H("#ffffff"), 4f));
                }
            var pink = Paint("#f8bbd0", 0.4f);
            Bar(k, f.At(f.Width - 2.5f, top - 1.0f) + f.Normal * 0.05f, f.At(f.Width - 1.0f, top - 3.2f) + f.Normal * 0.05f, 0.05f, pink);
            Bar(k, f.At(f.Width - 1.0f, top - 1.0f) + f.Normal * 0.05f, f.At(f.Width - 2.5f, top - 3.2f) + f.Normal * 0.05f, 0.05f, pink);
        }

        static void Tutoring(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Sidewalk(k, w, d);
            float z0 = -d / 2 + 2.4f;
            var f = Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, z0, d / 2 - 0.4f, 4.6f, T("wall_siding", new Color(0.96f, 0.94f, 0.86f)), "gf_store_light", closed, season, Row(BuildingType.TutoringCenter), Paint("#c62828"));
            // a giant pencil lying on the parapet
            float y = 5.3f, z = z0 + 0.1f, x0 = -3.2f, x1 = 2.2f;
            var yel = Paint("#fbc02d", 0.4f); var wood = Paint("#e8c9a0", 0.3f); var lead = Paint("#424242", 0.5f);
            k.Box(new Vector3(x0, y, z - 0.3f), new Vector3(x1, y + 0.6f, z + 0.3f), yel, yel);
            k.Box(new Vector3(x0 - 0.35f, y - 0.02f, z - 0.32f), new Vector3(x0, y + 0.62f, z + 0.32f), Paint("#b0bec5", 0.7f, 0.8f), Paint("#b0bec5", 0.7f, 0.8f));
            k.Box(new Vector3(x0 - 0.85f, y, z - 0.3f), new Vector3(x0 - 0.35f, y + 0.6f, z + 0.3f), Paint("#f48fb1"), Paint("#f48fb1"));
            var tip = new Vector3(x1 + 1.2f, y + 0.3f, z);
            Vector3[] sq = { new Vector3(x1, y, z - 0.3f), new Vector3(x1, y + 0.6f, z - 0.3f), new Vector3(x1, y + 0.6f, z + 0.3f), new Vector3(x1, y, z + 0.3f) };
            for (int i = 0; i < 4; i++) Two(k, sq[i], sq[(i + 1) % 4], tip, wood);
            for (int i = 0; i < 4; i++) Two(k, Vector3.Lerp(sq[i], tip, 0.7f), Vector3.Lerp(sq[(i + 1) % 4], tip, 0.7f), tip, lead);
            // a chalkboard on the pavement
            var board = Paint("#2e4d3a", 0.2f); var frame = T("wood", new Color(0.6f, 0.45f, 0.3f));
            var bp = f.At(1.3f, 0.02f) + f.Normal * 1.2f;
            TwoQuad(k, bp + new Vector3(-0.4f, 0, -0.3f), bp + new Vector3(-0.4f, 1.0f, 0), bp + new Vector3(0.4f, 1.0f, 0), bp + new Vector3(0.4f, 0, -0.3f), board);
            TwoQuad(k, bp + new Vector3(-0.4f, 0, 0.3f), bp + new Vector3(-0.4f, 1.0f, 0), bp + new Vector3(0.4f, 1.0f, 0), bp + new Vector3(0.4f, 0, 0.3f), frame);
        }

        static void DrivingSchool(MeshKit k, float w, float d, ref uint s, bool closed, int season)
        {
            Lot(k, w, d, S("asphalt"), 0.05f, 8f);
            Storefront(k, -w / 2 + 0.4f, w / 2 - 0.4f, 0.4f, d / 2 - 0.4f, 4.4f, T("wall_stucco", new Color(0.98f, 0.97f, 0.94f)), "gf_store_light", closed, season, Row(BuildingType.DrivingSchool), Paint("#c62828"));
            // the learner car with its roof sign, and cones to practise round
            k.Push(new Vector3(-2.2f, 0.06f, -2.2f), 90);
            PropArt.Car(k, H("#fafafa"));
            k.Pop();
            var red = Paint("#c62828"); var white = Paint("#fafafa");
            k.Box(new Vector3(-2.6f, 1.5f, -2.5f), new Vector3(-1.8f, 1.95f, -1.9f), red, white);
            k.Box(new Vector3(-2.4f, 1.6f, -2.52f), new Vector3(-2.3f, 1.85f, -2.5f), white, white);
            k.Box(new Vector3(-2.4f, 1.6f, -2.52f), new Vector3(-2.15f, 1.68f, -2.5f), white, white);
            var cone = Paint("#ff6d00", 0.4f);
            for (int i = 0; i < 4; i++)
            {
                var p = new Vector3(1.5f + i * 0.9f, 0.06f, -3.6f + (i % 2) * 0.9f);
                k.Box(p + new Vector3(-0.2f, 0, -0.2f), p + new Vector3(0.2f, 0.05f, 0.2f), cone, cone);
                k.Cylinder(p, 0.15f, 0.03f, 0.6f, 8, cone, cone);
                k.Cylinder(p + Vector3.up * 0.3f, 0.09f, 0.08f, 0.08f, 8, white, null);
            }
        }
    }
}
