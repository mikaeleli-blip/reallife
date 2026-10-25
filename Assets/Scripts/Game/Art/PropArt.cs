using UnityEngine;

namespace RealLife.Game.Art
{
    /// <summary>Small things: trees, people, cars, trucks, buses, benches, lamps. Sizes in metres; a tile is 10 m.</summary>
    public static class PropArt
    {
        public static readonly Color32[] CarColors =
        {
            MeshKit.Hex("#c0392b"), MeshKit.Hex("#2c3e50"), MeshKit.Hex("#ecf0f1"), MeshKit.Hex("#7f8c8d"), MeshKit.Hex("#2980b9"),
            MeshKit.Hex("#16a085"), MeshKit.Hex("#f1c40f"), MeshKit.Hex("#1c1c1c"), MeshKit.Hex("#8e44ad"), MeshKit.Hex("#d35400"), MeshKit.Hex("#bdc3c7")
        };
        public static readonly Color32[] ShirtColors =
        {
            MeshKit.Hex("#e74c3c"), MeshKit.Hex("#3498db"), MeshKit.Hex("#2ecc71"), MeshKit.Hex("#f39c12"), MeshKit.Hex("#9b59b6"), MeshKit.Hex("#ecf0f1"),
            MeshKit.Hex("#34495e"), MeshKit.Hex("#1abc9c"), MeshKit.Hex("#e67e22"), MeshKit.Hex("#fd79a8"), MeshKit.Hex("#6c5ce7")
        };
        public static readonly Color32[] SkinColors = { MeshKit.Hex("#f5d0b5"), MeshKit.Hex("#e0ac8a"), MeshKit.Hex("#c68863"), MeshKit.Hex("#8d5a3b"), MeshKit.Hex("#5c3a24") };
        static readonly Color32 Glass = MeshKit.Hex("#2c3a48");

        static Material M(Color32 c, float smooth = 0.2f, float metal = 0f) { return MaterialLibrary.Get(c, smooth, metal); }
        static Material Tire { get { return M(MeshKit.Hex("#1e1e1e"), 0.15f); } }
        static Material Pane { get { return MaterialLibrary.Pane(Glass); } }
        static Material Headlight { get { return MaterialLibrary.Glow(new Color(0.95f, 0.93f, 0.85f), new Color(1f, 0.92f, 0.75f), 2.5f); } }
        static Material Taillight { get { return MaterialLibrary.Glow(new Color(0.6f, 0.08f, 0.06f), new Color(1f, 0.1f, 0.05f), 2f); } }

        // ------------------------------------------------------------------ trees
        /// <summary>A leafy tree. season 0 winter .. 3 autumn.</summary>
        public static void Tree(MeshKit k, Vector3 p, float scale, int season, uint seed)
        {
            float h = 2.2f * scale;
            var bark = M(MeshKit.Hex("#6b4a2f"), 0.1f);
            k.Cylinder(p, 0.22f * scale, 0.16f * scale, h, 5, bark, bark);
            Color32 leaf;
            int v = (int)(seed % 3);
            if (season == 0)
            {
                // bare branches with snow on them
                k.Blob(p + new Vector3(0, h + 0.9f * scale, 0), new Vector3(1.3f, 1.2f, 1.3f) * scale, MeshKit.Hex("#7a6a5a"));
                k.Blob(p + new Vector3(0, h + 1.5f * scale, 0), new Vector3(0.9f, 0.6f, 0.9f) * scale, MeshKit.Hex("#f2f4f7"));
                return;
            }
            if (season == 3) leaf = v == 0 ? MeshKit.Hex("#d9822b") : v == 1 ? MeshKit.Hex("#c0392b") : MeshKit.Hex("#e1b12c");
            else if (season == 1) leaf = v == 0 ? MeshKit.Hex("#6b9e47") : v == 1 ? MeshKit.Hex("#79a852") : MeshKit.Hex("#5d9145");
            else leaf = v == 0 ? MeshKit.Hex("#46793a") : v == 1 ? MeshKit.Hex("#527f3d") : MeshKit.Hex("#3b6b33");
            k.Blob(p + new Vector3(0, h + 1.1f * scale, 0), new Vector3(1.9f, 1.8f, 1.9f) * scale, leaf);
            k.Blob(p + new Vector3(0.6f * scale, h + 2.1f * scale, 0.3f * scale), new Vector3(1.2f, 1.1f, 1.2f) * scale, MeshKit.Shade(leaf, 1.1f));
            k.Blob(p + new Vector3(-0.7f * scale, h + 0.7f * scale, -0.4f * scale), new Vector3(1.1f, 0.9f, 1.1f) * scale, MeshKit.Shade(leaf, 0.9f));
        }

        /// <summary>An evergreen.</summary>
        public static void Pine(MeshKit k, Vector3 p, float scale, int season)
        {
            var bark = M(MeshKit.Hex("#5c4030"), 0.1f);
            k.Cylinder(p, 0.2f * scale, 0.15f * scale, 1.2f * scale, 5, bark, bark);
            var g = MeshKit.Hex("#2f6b3a");
            k.Cylinder(p + new Vector3(0, 1.0f * scale, 0), 1.7f * scale, 0, 2.6f * scale, 6, g, g);
            k.Cylinder(p + new Vector3(0, 2.4f * scale, 0), 1.3f * scale, 0, 2.3f * scale, 6, MeshKit.Shade(g, 1.08f), g);
            if (season == 0) k.Cylinder(p + new Vector3(0, 3.6f * scale, 0), 0.8f * scale, 0, 1.3f * scale, 6, MeshKit.Hex("#eef2f5"), g);
            else k.Cylinder(p + new Vector3(0, 3.6f * scale, 0), 0.8f * scale, 0, 1.4f * scale, 6, MeshKit.Shade(g, 1.15f), g);
        }

        public static void Bush(MeshKit k, Vector3 p, float s, int season)
        {
            var c = season == 0 ? MeshKit.Hex("#9aa59a") : season == 3 ? MeshKit.Hex("#9a7a3a") : MeshKit.Hex("#4c8a3c");
            k.Blob(p + new Vector3(0, 0.45f * s, 0), new Vector3(0.8f, 0.55f, 0.8f) * s, c);
        }

        // ------------------------------------------------------------------ people and vehicles (built around the origin, facing +z)
        public static Model Person(uint seed, bool child)
        {
            var k = new MeshKit();
            float s = child ? 0.65f : 1f;
            var shirt = M(ShirtColors[seed % (uint)ShirtColors.Length], 0.15f);
            var pants = M((seed >> 4) % 3 == 0 ? MeshKit.Hex("#2d3436") : (seed >> 4) % 3 == 1 ? MeshKit.Hex("#34495e") : MeshKit.Hex("#7f6a55"), 0.15f);
            var skin = M(SkinColors[(seed >> 8) % (uint)SkinColors.Length], 0.3f);
            var hair = M((seed >> 12) % 4 == 0 ? MeshKit.Hex("#e8c07a") : (seed >> 12) % 4 == 1 ? MeshKit.Hex("#3b2a1e") : (seed >> 12) % 4 == 2 ? MeshKit.Hex("#1e1a18") : MeshKit.Hex("#8a4b2a"), 0.3f);
            var shoes = M(MeshKit.Hex("#222222"), 0.3f);
            k.Box(new Vector3(-0.19f, 0, -0.14f) * s, new Vector3(-0.03f, 0.1f, 0.16f) * s, shoes);
            k.Box(new Vector3(0.03f, 0, -0.14f) * s, new Vector3(0.19f, 0.1f, 0.16f) * s, shoes);
            k.Box(new Vector3(-0.18f, 0.1f, -0.11f) * s, new Vector3(-0.03f, 0.85f, 0.11f) * s, pants);
            k.Box(new Vector3(0.03f, 0.1f, -0.11f) * s, new Vector3(0.18f, 0.85f, 0.11f) * s, pants);
            k.Box(new Vector3(-0.23f, 0.85f, -0.14f) * s, new Vector3(0.23f, 1.45f, 0.14f) * s, shirt);
            k.Box(new Vector3(-0.32f, 0.88f, -0.09f) * s, new Vector3(-0.23f, 1.42f, 0.09f) * s, shirt);
            k.Box(new Vector3(0.23f, 0.88f, -0.09f) * s, new Vector3(0.32f, 1.42f, 0.09f) * s, shirt);
            k.Box(new Vector3(-0.11f, 1.45f, -0.11f) * s, new Vector3(0.11f, 1.72f, 0.11f) * s, skin);
            k.Box(new Vector3(-0.12f, 1.66f, -0.13f) * s, new Vector3(0.12f, 1.8f, 0.12f) * s, hair);
            return k.ToModel("person");
        }

        public static void Car(MeshKit k, Color32 body)
        {
            var paint = M(body, 0.7f, 0.25f);
            var trim = M(MeshKit.Hex("#2a2d31"), 0.3f);
            k.Box(new Vector3(-0.9f, 0.3f, -2.1f), new Vector3(0.9f, 0.95f, 2.1f), paint, paint);
            k.Box(new Vector3(-0.8f, 0.95f, -1.1f), new Vector3(0.8f, 1.45f, 1.0f), paint, paint);
            k.Box(new Vector3(-0.92f, 0.3f, -2.12f), new Vector3(0.92f, 0.45f, 2.12f), trim, trim);
            // windscreen and windows
            var pane = Pane;
            k.Quad(new Vector3(0.75f, 1.0f, 1.01f), new Vector3(0.7f, 1.4f, 1.01f), new Vector3(-0.7f, 1.4f, 1.01f), new Vector3(-0.75f, 1.0f, 1.01f), pane);
            k.Quad(new Vector3(-0.75f, 1.0f, -1.11f), new Vector3(-0.7f, 1.4f, -1.11f), new Vector3(0.7f, 1.4f, -1.11f), new Vector3(0.75f, 1.0f, -1.11f), pane);
            k.Quad(new Vector3(0.81f, 1.0f, -0.9f), new Vector3(0.81f, 1.38f, -0.9f), new Vector3(0.81f, 1.38f, 0.85f), new Vector3(0.81f, 1.0f, 0.85f), pane);
            k.Quad(new Vector3(-0.81f, 1.0f, 0.85f), new Vector3(-0.81f, 1.38f, 0.85f), new Vector3(-0.81f, 1.38f, -0.9f), new Vector3(-0.81f, 1.0f, -0.9f), pane);
            var tire = Tire;
            foreach (var z in new[] { -1.35f, 1.35f })
                foreach (var x in new[] { -0.95f, 0.75f })
                    k.Box(new Vector3(x, 0, z - 0.35f), new Vector3(x + 0.2f, 0.62f, z + 0.35f), tire, tire);
            // lights
            var head = Headlight; var tail = Taillight;
            k.Box(new Vector3(-0.8f, 0.6f, 2.1f), new Vector3(-0.45f, 0.78f, 2.14f), head, head);
            k.Box(new Vector3(0.45f, 0.6f, 2.1f), new Vector3(0.8f, 0.78f, 2.14f), head, head);
            k.Box(new Vector3(-0.8f, 0.6f, -2.14f), new Vector3(-0.5f, 0.75f, -2.1f), tail, tail);
            k.Box(new Vector3(0.5f, 0.6f, -2.14f), new Vector3(0.8f, 0.75f, -2.1f), tail, tail);
        }

        public static Model CarModel(Color32 body) { var k = new MeshKit(); Car(k, body); return k.ToModel("car"); }

        public static void Truck(MeshKit k, Color32 cab, Color32 box)
        {
            var cabM = M(cab, 0.6f, 0.2f);
            var boxM = M(box, 0.35f);
            k.Box(new Vector3(-1.1f, 0.4f, 1.6f), new Vector3(1.1f, 2.4f, 3.4f), cabM, cabM);
            k.Quad(new Vector3(0.95f, 1.5f, 3.41f), new Vector3(0.95f, 2.25f, 3.41f), new Vector3(-0.95f, 2.25f, 3.41f), new Vector3(-0.95f, 1.5f, 3.41f), Pane);
            k.Box(new Vector3(-1.2f, 0.6f, -4.2f), new Vector3(1.2f, 3.4f, 1.5f), boxM, M(MeshKit.Shade(box, 1.06f), 0.35f));
            var chassis = M(MeshKit.Hex("#2a2d31"), 0.3f);
            k.Box(new Vector3(-1.0f, 0.35f, -4.2f), new Vector3(1.0f, 0.6f, 3.4f), chassis, chassis);
            var tire = Tire;
            foreach (var z in new[] { -3.2f, -2.0f, 2.6f })
                foreach (var x in new[] { -1.25f, 1.0f })
                    k.Box(new Vector3(x, 0, z - 0.45f), new Vector3(x + 0.25f, 0.85f, z + 0.45f), tire, tire);
            var head = Headlight;
            k.Box(new Vector3(-0.95f, 0.7f, 3.4f), new Vector3(-0.6f, 0.9f, 3.44f), head, head);
            k.Box(new Vector3(0.6f, 0.7f, 3.4f), new Vector3(0.95f, 0.9f, 3.44f), head, head);
        }

        public static Model TruckModel(Color32 cab, Color32 box) { var k = new MeshKit(); Truck(k, cab, box); return k.ToModel("truck"); }

        public static void Bus(MeshKit k, Color32 body)
        {
            var paint = M(body, 0.6f, 0.2f);
            k.Box(new Vector3(-1.25f, 0.4f, -5.5f), new Vector3(1.25f, 3.1f, 5.5f), paint, M(MeshKit.Hex("#dfe6e9"), 0.4f));
            var pane = Pane;
            k.Box(new Vector3(-1.27f, 1.6f, -5.0f), new Vector3(1.27f, 2.5f, 5.0f), pane, pane);
            k.Quad(new Vector3(1.1f, 1.3f, 5.51f), new Vector3(1.1f, 2.7f, 5.51f), new Vector3(-1.1f, 2.7f, 5.51f), new Vector3(-1.1f, 1.3f, 5.51f), pane);
            var tire = Tire;
            foreach (var z in new[] { -3.6f, 3.6f })
                foreach (var x in new[] { -1.3f, 1.05f })
                    k.Box(new Vector3(x, 0, z - 0.5f), new Vector3(x + 0.25f, 0.95f, z + 0.5f), tire, tire);
            var head = Headlight;
            k.Box(new Vector3(-1.0f, 0.7f, 5.5f), new Vector3(-0.6f, 0.9f, 5.54f), head, head);
            k.Box(new Vector3(0.6f, 0.7f, 5.5f), new Vector3(1.0f, 0.9f, 5.54f), head, head);
        }

        public static Model BusModel(Color32 body) { var k = new MeshKit(); Bus(k, body); return k.ToModel("bus"); }

        public static void Bench(MeshKit k, Vector3 p, float yaw)
        {
            var wood = M(MeshKit.Hex("#8b5e3c"), 0.25f);
            var iron = M(MeshKit.Hex("#3a3d42"), 0.4f, 0.5f);
            k.Push(p, yaw);
            k.Box(new Vector3(-0.8f, 0.4f, -0.25f), new Vector3(0.8f, 0.5f, 0.25f), wood, wood);
            k.Box(new Vector3(-0.8f, 0.5f, 0.18f), new Vector3(0.8f, 0.95f, 0.26f), wood, wood);
            k.Box(new Vector3(-0.7f, 0, -0.2f), new Vector3(-0.6f, 0.4f, 0.2f), iron, iron);
            k.Box(new Vector3(0.6f, 0, -0.2f), new Vector3(0.7f, 0.4f, 0.2f), iron, iron);
            k.Pop();
        }

        public static void Lamp(MeshKit k, Vector3 p)
        {
            var pole = M(MeshKit.Hex("#3d4248"), 0.45f, 0.5f);
            k.Box(p + new Vector3(-0.07f, 0, -0.07f), p + new Vector3(0.07f, 4.2f, 0.07f), pole, pole);
            k.Box(p + new Vector3(-0.25f, 4.2f, -0.25f), p + new Vector3(0.25f, 4.4f, 0.25f), pole, pole);
            // a lantern under the cap, seen from above and from the side
            var bulb = MaterialLibrary.Glow(new Color(0.95f, 0.9f, 0.8f), new Color(1f, 0.8f, 0.5f), 6f);
            k.Box(p + new Vector3(-0.17f, 3.85f, -0.17f), p + new Vector3(0.17f, 4.2f, 0.17f), bulb, bulb);
        }

        /// <summary>A car parked at p, facing yaw.</summary>
        public static void Parked(MeshKit k, Vector3 p, float yaw, uint seed)
        {
            k.Push(p, yaw);
            Car(k, CarColors[seed % (uint)CarColors.Length]);
            k.Pop();
        }
    }
}
