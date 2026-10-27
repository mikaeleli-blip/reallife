using UnityEngine;

namespace RealLife.Game.Art
{
    /// <summary>One side of a building, seen from outside: Right runs left to right, Normal points out.</summary>
    public struct Face
    {
        public Vector3 LeftBottom, Right, Normal;
        public float Width;
        public Vector3 At(float along, float y) { return LeftBottom + Right * along + Vector3.up * y; }
    }

    /// <summary>
    /// A building skin: which texture atlases it wears and the real size of one atlas cell (ported from Mobland's
    /// FacadeCatalog). Upper atlases are 4 bays x 4 floors, ground atlases 4 bays x 1 floor.
    /// </summary>
    public sealed class Skin
    {
        public string Upper = "fac_apt_brick";
        public float Bay = 2.7f, Floor = 3.15f;
        public string Ground = "gf_apt_base";
        public float GroundH = 3.8f, GroundBay = 2.7f;
        public string Wall = "wall_brick_red";
        public float WallTile = 2f;
        public Color32 Coping = new Color32(200, 196, 188, 255);
        public float Glow = 1.5f;
    }

    /// <summary>Helpers for putting facades, ledges, signs and roofs on boxes.</summary>
    public static class Facades
    {
        public const int North = 0, South = 1, East = 2, West = 3;

        /// <summary>The four sides of a box footprint: north (+z), south (-z, the street), east (+x), west (-x).</summary>
        public static Face[] Faces(float x0, float x1, float z0, float z1)
        {
            return new[]
            {
                new Face { LeftBottom = new Vector3(x1, 0, z1), Right = Vector3.left, Normal = Vector3.forward, Width = x1 - x0 },
                new Face { LeftBottom = new Vector3(x0, 0, z0), Right = Vector3.right, Normal = Vector3.back, Width = x1 - x0 },
                new Face { LeftBottom = new Vector3(x1, 0, z0), Right = Vector3.forward, Normal = Vector3.right, Width = z1 - z0 },
                new Face { LeftBottom = new Vector3(x0, 0, z1), Right = Vector3.back, Normal = Vector3.left, Width = z1 - z0 },
            };
        }

        /// <summary>A rectangle on a face from (a0, y0) to (a1, y1), pushed out by `out`, with UVs at its corners.</summary>
        public static void Rect(MeshKit k, Face f, float a0, float a1, float y0, float y1, Material m, Vector2 uv0, Vector2 uv1, float outward = 0f)
        {
            var o = f.Normal * outward;
            k.Quad(f.At(a0, y0) + o, f.At(a0, y1) + o, f.At(a1, y1) + o, f.At(a1, y0) + o, m,
                   new Vector2(uv0.x, uv0.y), new Vector2(uv0.x, uv1.y), new Vector2(uv1.x, uv1.y), new Vector2(uv1.x, uv0.y));
        }

        /// <summary>A textured band of an atlas: one cell is one bay wide and cellHeight tall.</summary>
        public static void Band(MeshKit k, Face f, float y0, float y1, Material m, float bayNominal, float cellHeight, int atlasRows, int uOff, int vOff)
        {
            if (y1 - y0 < 0.01f || f.Width < 0.05f) return;
            int bays = Mathf.Max(1, Mathf.RoundToInt(f.Width / bayNominal));
            float u0 = uOff * 0.25f, u1 = u0 + bays * 0.25f;
            float v0 = (float)vOff / atlasRows, v1 = v0 + (y1 - y0) / cellHeight / atlasRows;
            Rect(k, f, 0, f.Width, y0, y1, m, new Vector2(u0, v0), new Vector2(u1, v1));
        }

        /// <summary>How many bays a band of this face has (for lining geometry up with the windows).</summary>
        public static int Bays(Face f, float bayNominal) { return Mathf.Max(1, Mathf.RoundToInt(f.Width / bayNominal)); }

        /// <summary>A plain wall, world-planar mapped.</summary>
        public static void Plain(MeshKit k, Face f, float y0, float y1, Material m, float tile)
        {
            if (y1 - y0 < 0.01f || f.Width < 0.05f) return;
            k.Quad(f.At(0, y0), f.At(0, y1), f.At(f.Width, y1), f.At(f.Width, y0), m, tile);
        }

        /// <summary>A box sticking out of a face (cornices, belt courses, canopies, sign boards).</summary>
        public static void Ledge(MeshKit k, Face f, float a0, float a1, float y0, float y1, float depth, Material m, Material top = null, float tile = 2f)
        {
            Vector3 p0 = f.At(a0, y0), p1 = f.At(a1, y1) + f.Normal * depth;
            k.Box(Vector3.Min(p0, p1), Vector3.Max(p0, p1), m, top ?? m, tile, true);
        }

        /// <summary>A sign board on a face: a dark frame and a lit panel from the signs atlas (row 0..31).</summary>
        public static void Sign(MeshKit k, Face f, float center, float width, float y, float height, int row, bool dark)
        {
            var frame = MaterialLibrary.Get(new Color32(38, 40, 44, 255), 0.4f, 0.3f);
            Ledge(k, f, center - width / 2 - 0.08f, center + width / 2 + 0.08f, y - 0.08f, y + height + 0.08f, 0.14f, frame);
            // rows past 32 are on the second atlas
            string atlas = row < 32 ? "signs" : "signs2";
            row %= 32;
            var mat = dark ? MaterialLibrary.Dark(atlas, new Color(0.5f, 0.5f, 0.5f)) : MaterialLibrary.Surface(atlas, 2.2f);
            float v1 = 1f - row / 32f, v0 = v1 - 1f / 32f;
            Rect(k, f, center - width / 2, center + width / 2, y, y + height, mat, new Vector2(0.01f, v0 + 0.002f), new Vector2(0.99f, v1 - 0.002f), 0.15f);
        }

        /// <summary>A flat roof with a parapet and coping.</summary>
        public static void FlatRoof(MeshKit k, float x0, float x1, float z0, float z1, float y, Material roof, Material wall, Material coping, float parapet = 0.7f)
        {
            k.Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), roof, 6f);
            const float t = 0.3f;
            float h = parapet;
            k.Box(new Vector3(x0, y, z0), new Vector3(x1, y + h, z0 + t), wall, coping);
            k.Box(new Vector3(x0, y, z1 - t), new Vector3(x1, y + h, z1), wall, coping);
            k.Box(new Vector3(x0, y, z0 + t), new Vector3(x0 + t, y + h, z1 - t), wall, coping);
            k.Box(new Vector3(x1 - t, y, z0 + t), new Vector3(x1, y + h, z1 - t), wall, coping);
            // a cap that hangs over both sides of the parapet
            k.Box(new Vector3(x0 - 0.06f, y + h, z0 - 0.06f), new Vector3(x1 + 0.06f, y + h + 0.12f, z0 + t + 0.06f), coping, coping);
            k.Box(new Vector3(x0 - 0.06f, y + h, z1 - t - 0.06f), new Vector3(x1 + 0.06f, y + h + 0.12f, z1 + 0.06f), coping, coping);
            k.Box(new Vector3(x0 - 0.06f, y + h, z0 + t), new Vector3(x0 + t + 0.06f, y + h + 0.12f, z1 - t), coping, coping);
            k.Box(new Vector3(x1 - t - 0.06f, y + h, z0 + t), new Vector3(x1 + 0.06f, y + h + 0.12f, z1 - t), coping, coping);
        }

        /// <summary>Rooftop plant: air-conditioning units, vents, a stair bulkhead, the odd satellite dish.</summary>
        public static void RoofClutter(MeshKit k, float x0, float x1, float z0, float z1, float y, ref uint s, float amount, Material wall)
        {
            var metal = MaterialLibrary.Get(new Color32(186, 190, 194, 255), 0.45f, 0.6f);
            var dark = MaterialLibrary.Get(new Color32(52, 55, 60, 255), 0.3f, 0.4f);
            if (x1 - x0 < 3 || z1 - z0 < 3) return;
            int units = Mathf.RoundToInt(amount * (1 + (x1 - x0) * (z1 - z0) / 60f));
            for (int i = 0; i < units; i++)
            {
                float x = Mathf.Lerp(x0 + 1.5f, x1 - 1.5f, R(ref s)), z = Mathf.Lerp(z0 + 1.5f, z1 - 1.5f, R(ref s));
                if (R(ref s) < 0.6f)
                {
                    // a packaged AC unit with its fan on top
                    k.Box(new Vector3(x - 0.9f, y, z - 0.6f), new Vector3(x + 0.9f, y + 1.0f, z + 0.6f), metal, metal);
                    k.Cylinder(new Vector3(x + 0.35f, y + 1.0f, z), 0.42f, 0.42f, 0.06f, 10, dark, dark);
                    k.Box(new Vector3(x - 0.8f, y + 0.25f, z - 0.62f), new Vector3(x - 0.1f, y + 0.85f, z - 0.6f), dark, dark);
                }
                else
                {
                    k.Cylinder(new Vector3(x, y, z), 0.18f, 0.18f, 0.9f, 8, metal, metal);
                    k.Cylinder(new Vector3(x, y + 0.9f, z), 0.32f, 0.0f, 0.25f, 8, metal, metal);
                }
            }
            if (amount > 0.5f && x1 - x0 > 8 && z1 - z0 > 6)
            {
                // stair bulkhead with its door
                float bx = Mathf.Lerp(x0 + 2, x1 - 4.5f, R(ref s)), bz = z1 - 4;
                k.Box(new Vector3(bx, y, bz), new Vector3(bx + 2.6f, y + 2.6f, bz + 3), wall, MaterialLibrary.Get(new Color32(170, 172, 170, 255)));
                k.Box(new Vector3(bx + 0.8f, y, bz - 0.05f), new Vector3(bx + 1.8f, y + 2.1f, bz), dark, dark);
            }
            if (R(ref s) < 0.25f)
            {
                float x = Mathf.Lerp(x0 + 1, x1 - 1, R(ref s)), z = Mathf.Lerp(z0 + 1, z1 - 1, R(ref s));
                var white = MaterialLibrary.Get(new Color32(232, 232, 228, 255));
                k.Box(new Vector3(x - 0.05f, y, z - 0.05f), new Vector3(x + 0.05f, y + 0.9f, z + 0.05f), dark, dark);
                k.Cylinder(new Vector3(x, y + 0.9f, z + 0.2f), 0.5f, 0.5f, 0.05f, 12, white, white);
            }
        }

        static float R(ref uint s) { s = s * 1664525u + 1013904223u; return ((s >> 8) & 0xFFFF) / 65535f; }

        /// <summary>A box building wearing a skin: ground floor band on the street side, upper floors above, upper atlas
        /// on the back and (if facadeSides) the sides, blank walls otherwise; a cornice and a flat roof. Returns the roof height.</summary>
        public static float Block(MeshKit k, float x0, float x1, float z0, float z1, Skin skin, int floors, int uOff, int vOff,
                                  bool facadeSides, bool groundBand, string roof = "roof_membrane", int season = 2, bool dark = false)
        {
            var faces = Faces(x0, x1, z0, z1);
            float gh = groundBand ? skin.GroundH : 0f, H = gh + floors * skin.Floor;
            var grey = new Color(0.75f, 0.75f, 0.75f);
            var upper = dark ? MaterialLibrary.Dark(skin.Upper, grey) : MaterialLibrary.Surface(skin.Upper, skin.Glow);
            var groundMat = dark ? MaterialLibrary.Dark(skin.Ground, grey) : MaterialLibrary.Surface(skin.Ground, skin.Glow * 1.2f);
            var wall = MaterialLibrary.Surface(skin.Wall);
            var coping = MaterialLibrary.Get(skin.Coping, 0.3f);
            for (int i = 0; i < 4; i++)
            {
                var f = faces[i];
                bool facade = i == South || i == North || facadeSides;
                if (!facade) { Plain(k, f, 0, H, wall, skin.WallTile); continue; }
                if (i == South && groundBand)
                {
                    Band(k, f, 0, gh, groundMat, skin.GroundBay, skin.GroundH, 1, uOff, 0);
                    Band(k, f, gh, H, upper, skin.Bay, skin.Floor, 4, uOff, vOff);
                    Ledge(k, f, -0.05f, f.Width + 0.05f, gh - 0.1f, gh + 0.12f, 0.12f, coping);
                }
                else Band(k, f, 0, H, upper, skin.Bay, skin.Floor, 4, (uOff + i) % 4, (vOff + i) % 4);
            }
            var roofMat = season == 0 ? MaterialLibrary.Surface("snow") : MaterialLibrary.Surface(roof);
            FlatRoof(k, x0, x1, z0, z1, H, roofMat, wall, coping);
            return H;
        }
    }
}
