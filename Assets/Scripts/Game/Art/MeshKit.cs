using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealLife.Game.Art
{
    /// <summary>A mesh with one material per sub-mesh.</summary>
    public struct Model
    {
        public Mesh Mesh;
        public Material[] Materials;
    }

    /// <summary>
    /// Builds flat-shaded low-poly meshes with one sub-mesh per material (the Mobland approach). Faces are either textured
    /// (a surface material with UVs: world-planar by default, or explicit for facades, roofs and signs) or plain colour
    /// (a cached colour material). Every face gets its own vertices and normal so the lighting reads as crisp facets.
    /// Vertex colours are kept as well, for the see-through overlays (zones, previews, selection).
    /// </summary>
    public sealed class MeshKit
    {
        readonly List<Vector3> verts = new List<Vector3>(1024);
        readonly List<Vector3> norms = new List<Vector3>(1024);
        readonly List<Vector2> uvs = new List<Vector2>(1024);
        readonly List<Color32> cols = new List<Color32>(1024);
        readonly List<Material> mats = new List<Material>();
        readonly List<List<int>> tris = new List<List<int>>();
        readonly Dictionary<Material, int> matIndex = new Dictionary<Material, int>();
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        Matrix4x4 m = Matrix4x4.identity;
        bool flipped;

        public int VertexCount { get { return verts.Count; } }
        public void Clear()
        {
            verts.Clear(); norms.Clear(); uvs.Clear(); cols.Clear(); mats.Clear(); tris.Clear(); matIndex.Clear(); stack.Clear();
            m = Matrix4x4.identity; flipped = false;
        }

        // ------------------------------------------------------------------ transform
        public void SetTransform(Matrix4x4 t) { m = t; flipped = t.determinant < 0; }
        public void ResetTransform() { m = Matrix4x4.identity; flipped = false; }
        public void SetTransform(Vector3 pos, float yawDeg) { SetTransform(Matrix4x4.TRS(pos, Quaternion.Euler(0, yawDeg, 0), Vector3.one)); }
        /// <summary>Moves and turns what's added next, on top of the current transform, until Pop.</summary>
        public void Push(Vector3 pos, float yawDeg) { stack.Push(m); SetTransform(m * Matrix4x4.TRS(pos, Quaternion.Euler(0, yawDeg, 0), Vector3.one)); }
        public void Push(Vector3 pos, float yawDeg, Vector3 scale) { stack.Push(m); SetTransform(m * Matrix4x4.TRS(pos, Quaternion.Euler(0, yawDeg, 0), scale)); }
        public void Pop() { SetTransform(stack.Count > 0 ? stack.Pop() : Matrix4x4.identity); }

        List<int> Tris(Material mat)
        {
            int i;
            if (!matIndex.TryGetValue(mat, out i))
            {
                i = mats.Count; mats.Add(mat); tris.Add(new List<int>()); matIndex[mat] = i;
            }
            return tris[i];
        }

        /// <summary>World-planar UV for a point on a face with the given normal, in metres / tile.</summary>
        public static Vector2 PlanarUV(Vector3 p, Vector3 n, float tile)
        {
            float inv = 1f / Mathf.Max(0.0001f, tile);
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ay >= ax && ay >= az) return new Vector2(p.x * inv, p.z * inv);
            if (ax >= az) return new Vector2((n.x > 0 ? p.z : -p.z) * inv, p.y * inv);
            return new Vector2((n.z > 0 ? -p.x : p.x) * inv, p.y * inv);
        }

        static Color32 white = new Color32(255, 255, 255, 255);

        // ------------------------------------------------------------------ textured faces
        /// <summary>A triangle; winding as given (clockwise seen from the front). Planar UVs.</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Material mat, float tile = 2f)
        {
            a = m.MultiplyPoint3x4(a); b = m.MultiplyPoint3x4(b); c = m.MultiplyPoint3x4(c);
            if (flipped) { var t = b; b = c; c = t; }
            var n = Vector3.Cross(b - a, c - a).normalized;
            AddTri(a, b, c, PlanarUV(a, n, tile), PlanarUV(b, n, tile), PlanarUV(c, n, tile), n, mat, white);
        }

        void AddTri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc, Vector3 n, Material mat, Color32 col)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            norms.Add(n); norms.Add(n); norms.Add(n);
            uvs.Add(ua); uvs.Add(ub); uvs.Add(uc);
            cols.Add(col); cols.Add(col); cols.Add(col);
            var l = Tris(mat);
            l.Add(i); l.Add(i + 1); l.Add(i + 2);
        }

        /// <summary>A quad with explicit UVs; corners clockwise seen from the front.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            QuadCol(a, b, c, d, mat, ua, ub, uc, ud, white, false, 0);
        }

        /// <summary>A quad with planar UVs (tile = metres per texture repeat).</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat, float tile = 2f)
        {
            QuadCol(a, b, c, d, mat, default(Vector2), default(Vector2), default(Vector2), default(Vector2), white, true, tile);
        }

        void QuadCol(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material mat, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud, Color32 col, bool planar, float tile)
        {
            a = m.MultiplyPoint3x4(a); b = m.MultiplyPoint3x4(b); c = m.MultiplyPoint3x4(c); d = m.MultiplyPoint3x4(d);
            if (flipped) { var t = b; b = d; d = t; var tu = ub; ub = ud; ud = tu; }
            var n = Vector3.Cross(b - a, c - a).normalized;
            if (n.sqrMagnitude < 0.5f) n = Vector3.Cross(c - a, d - a).normalized;
            if (planar) { ua = PlanarUV(a, n, tile); ub = PlanarUV(b, n, tile); uc = PlanarUV(c, n, tile); ud = PlanarUV(d, n, tile); }
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
            uvs.Add(ua); uvs.Add(ub); uvs.Add(uc); uvs.Add(ud);
            cols.Add(col); cols.Add(col); cols.Add(col); cols.Add(col);
            var l = Tris(mat);
            l.Add(i); l.Add(i + 1); l.Add(i + 2); l.Add(i); l.Add(i + 2); l.Add(i + 3);
        }

        /// <summary>A flat rectangle on the ground (facing up).</summary>
        public void Flat(float x0, float z0, float x1, float z1, float y, Material mat, float tile = 4f)
        {
            Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), mat, tile);
        }

        /// <summary>A box; sides and top can differ. The bottom is skipped unless asked for.</summary>
        public void Box(Vector3 min, Vector3 max, Material side, Material top, float tile = 2f, bool bottom = false)
        {
            float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
            Quad(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0), side, tile);
            Quad(new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1), side, tile);
            Quad(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0), side, tile);
            Quad(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1), side, tile);
            Quad(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), top ?? side, tile);
            if (bottom) Quad(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), side, tile);
        }
        public void Box(Vector3 min, Vector3 max, Material mat, float tile = 2f) { Box(min, max, mat, mat, tile); }

        /// <summary>An upright cylinder (or cone, with r1 = 0) with n sides.</summary>
        public void Cylinder(Vector3 basePos, float r0, float r1, float h, int n, Material side, Material top, float tile = 2f)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
                var p0 = basePos + new Vector3(Mathf.Cos(a0) * r0, 0, Mathf.Sin(a0) * r0);
                var p1 = basePos + new Vector3(Mathf.Cos(a1) * r0, 0, Mathf.Sin(a1) * r0);
                var q0 = basePos + new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1);
                var q1 = basePos + new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
                if (r1 <= 0.001f) Tri(p0, q0, p1, side, tile);
                else
                {
                    // wrap the texture round the cylinder
                    float c = 2 * Mathf.PI * r0 / n / tile, u0 = i * c, u1 = (i + 1) * c, v1 = h / tile;
                    Quad(p0, q0, q1, p1, side, new Vector2(u0, 0), new Vector2(u0, v1), new Vector2(u1, v1), new Vector2(u1, 0));
                    if (top != null) Tri(basePos + Vector3.up * h, q1, q0, top, tile);
                }
            }
        }

        // ------------------------------------------------------------------ plain colour faces (small parts and props)
        static Material Paint(Color32 c) { return MaterialLibrary.Get(c); }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color32 col, bool window = false)
        {
            a = m.MultiplyPoint3x4(a); b = m.MultiplyPoint3x4(b); c = m.MultiplyPoint3x4(c);
            if (flipped) { var t = b; b = c; c = t; }
            var n = Vector3.Cross(b - a, c - a).normalized;
            AddTri(a, b, c, PlanarUV(a, n, 2), PlanarUV(b, n, 2), PlanarUV(c, n, 2), n, window ? MaterialLibrary.Pane(col) : Paint(col), col);
        }

        /// <summary>A flat-colour quad; window panes glow at night if they're a light colour.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 col, bool window = false)
        {
            QuadCol(a, b, c, d, window ? MaterialLibrary.Pane(col) : Paint(col), default(Vector2), default(Vector2), default(Vector2), default(Vector2), col, true, 2f);
        }

        public void Flat(float x0, float z0, float x1, float z1, float y, Color32 col)
        {
            Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), col);
        }

        public void Box(Vector3 min, Vector3 max, Color32 side, Color32 top, bool skipBottom = true)
        {
            Box(min, max, Paint(side), Paint(top), 2f, !skipBottom);
        }
        public void Box(Vector3 min, Vector3 max, Color32 col) { Box(min, max, Paint(col), Paint(col)); }

        /// <summary>A gable roof, ridge along x, with an overhang. Gable-end triangles in the wall material.</summary>
        public void Gable(float x0, float z0, float x1, float z1, float y, float rise, Material roof, Material gable, float overhang = 0.4f, float tile = 2f)
        {
            float zm = (z0 + z1) / 2;
            float ox0 = x0 - overhang, ox1 = x1 + overhang, oz0 = z0 - overhang, oz1 = z1 + overhang;
            float drop = rise * overhang / ((z1 - z0) / 2);
            float slope = Mathf.Sqrt((zm - oz0) * (zm - oz0) + (rise + drop) * (rise + drop));
            // u along the eave, v up the slope, so the shingle rows run level
            Quad(new Vector3(ox0, y - drop, oz0), new Vector3(ox0, y + rise, zm), new Vector3(ox1, y + rise, zm), new Vector3(ox1, y - drop, oz0), roof,
                 new Vector2(ox0 / tile, 0), new Vector2(ox0 / tile, slope / tile), new Vector2(ox1 / tile, slope / tile), new Vector2(ox1 / tile, 0));
            Quad(new Vector3(ox1, y - drop, oz1), new Vector3(ox1, y + rise, zm), new Vector3(ox0, y + rise, zm), new Vector3(ox0, y - drop, oz1), roof,
                 new Vector2(-ox1 / tile, 0), new Vector2(-ox1 / tile, slope / tile), new Vector2(-ox0 / tile, slope / tile), new Vector2(-ox0 / tile, 0));
            // soffits under the eaves
            var soffit = Paint(new Color32(236, 234, 228, 255));
            Quad(new Vector3(ox0, y - drop, oz0), new Vector3(ox1, y - drop, oz0), new Vector3(ox1, y, z0), new Vector3(ox0, y, z0), soffit);
            Quad(new Vector3(ox1, y - drop, oz1), new Vector3(ox0, y - drop, oz1), new Vector3(ox0, y, z1), new Vector3(ox1, y, z1), soffit);
            // fascia boards
            Quad(new Vector3(ox0, y - drop - 0.18f, oz0), new Vector3(ox0, y - drop, oz0), new Vector3(ox1, y - drop, oz0), new Vector3(ox1, y - drop - 0.18f, oz0), soffit);
            Quad(new Vector3(ox1, y - drop - 0.18f, oz1), new Vector3(ox1, y - drop, oz1), new Vector3(ox0, y - drop, oz1), new Vector3(ox0, y - drop - 0.18f, oz1), soffit);
            // gable ends
            Tri(new Vector3(x0, y, z1), new Vector3(x0, y + rise, zm), new Vector3(x0, y, z0), gable, tile);
            Tri(new Vector3(x1, y, z0), new Vector3(x1, y + rise, zm), new Vector3(x1, y, z1), gable, tile);
            // the roof's edge over the gable ends (barge boards)
            Quad(new Vector3(ox0, y - drop, oz0), new Vector3(ox0, y + rise, zm), new Vector3(ox0, y + rise + 0.18f, zm), new Vector3(ox0, y - drop + 0.18f, oz0), soffit);
            Quad(new Vector3(ox0, y - drop + 0.18f, oz1), new Vector3(ox0, y + rise + 0.18f, zm), new Vector3(ox0, y + rise, zm), new Vector3(ox0, y - drop, oz1), soffit);
            Quad(new Vector3(ox1, y - drop + 0.18f, oz0), new Vector3(ox1, y + rise + 0.18f, zm), new Vector3(ox1, y + rise, zm), new Vector3(ox1, y - drop, oz0), soffit);
            Quad(new Vector3(ox1, y - drop, oz1), new Vector3(ox1, y + rise, zm), new Vector3(ox1, y + rise + 0.18f, zm), new Vector3(ox1, y - drop + 0.18f, oz1), soffit);
        }

        /// <summary>A four-sided hip roof (a pyramid when square), with level shingle rows on every slope.</summary>
        public void Hip(float x0, float z0, float x1, float z1, float y, float rise, Material roof, float overhang = 0.4f, float tile = 2f)
        {
            x0 -= overhang; z0 -= overhang; x1 += overhang; z1 += overhang;
            float w = x1 - x0, d = z1 - z0;
            float inset = Mathf.Min(w, d) / 2;
            var r0 = w >= d ? new Vector3(x0 + inset, y + rise, (z0 + z1) / 2) : new Vector3((x0 + x1) / 2, y + rise, z0 + inset);
            var r1 = w >= d ? new Vector3(x1 - inset, y + rise, (z0 + z1) / 2) : new Vector3((x0 + x1) / 2, y + rise, z1 - inset);
            var a = new Vector3(x0, y, z0); var b = new Vector3(x1, y, z0); var c = new Vector3(x1, y, z1); var e = new Vector3(x0, y, z1);
            float sl = Mathf.Sqrt(inset * inset + rise * rise) / tile;
            // each slope: u along its eave, v up the slope
            if (w >= d)
            {
                Quad(a, r0, r1, b, roof, new Vector2(x0 / tile, 0), new Vector2(r0.x / tile, sl), new Vector2(r1.x / tile, sl), new Vector2(x1 / tile, 0));
                Quad(c, r1, r0, e, roof, new Vector2(-x1 / tile, 0), new Vector2(-r1.x / tile, sl), new Vector2(-r0.x / tile, sl), new Vector2(-x0 / tile, 0));
                RoofTri(e, r0, a, roof, sl, tile);
                RoofTri(b, r1, c, roof, sl, tile);
            }
            else
            {
                RoofTri(a, r0, b, roof, sl, tile);
                RoofTri(c, r1, e, roof, sl, tile);
                Quad(e, r1, r0, a, roof, new Vector2(z1 / tile, 0), new Vector2(r1.z / tile, sl), new Vector2(r0.z / tile, sl), new Vector2(z0 / tile, 0));
                Quad(b, r0, r1, c, roof, new Vector2(z0 / tile, 0), new Vector2(r0.z / tile, sl), new Vector2(r1.z / tile, sl), new Vector2(z1 / tile, 0));
            }
            var soffit = Paint(new Color32(236, 234, 228, 255));
            Box(new Vector3(x0, y - 0.2f, z0), new Vector3(x1, y, z0 + 0.06f), soffit, soffit);
            Box(new Vector3(x0, y - 0.2f, z1 - 0.06f), new Vector3(x1, y, z1), soffit, soffit);
            Box(new Vector3(x0, y - 0.2f, z0), new Vector3(x0 + 0.06f, y, z1), soffit, soffit);
            Box(new Vector3(x1 - 0.06f, y - 0.2f, z0), new Vector3(x1, y, z1), soffit, soffit);
            Quad(new Vector3(x0, y - 0.2f, z0), new Vector3(x1, y - 0.2f, z0), new Vector3(x1, y - 0.2f, z1), new Vector3(x0, y - 0.2f, z1), soffit);
        }

        /// <summary>A triangular roof slope: u along its eave (p to q), v up to the apex.</summary>
        void RoofTri(Vector3 p, Vector3 apex, Vector3 q, Material mat, float sl, float tile)
        {
            var eave = q - p; float len = eave.magnitude; var dir = eave / Mathf.Max(0.001f, len);
            float ua = Vector3.Dot(apex - p, dir);
            Vector2 tp = new Vector2(0, 0), ta = new Vector2(ua / tile, sl), tq = new Vector2(len / tile, 0);
            p = m.MultiplyPoint3x4(p); apex = m.MultiplyPoint3x4(apex); q = m.MultiplyPoint3x4(q);
            if (flipped) { var t = apex; apex = q; q = t; var tt = ta; ta = tq; tq = tt; }
            var n = Vector3.Cross(apex - p, q - p).normalized;
            AddTri(p, apex, q, tp, ta, tq, n, mat, white);
        }

        public void Gable(float x0, float z0, float x1, float z1, float y, float rise, Color32 roof, Color32 gable, float overhang = 0.4f)
        {
            Gable(x0, z0, x1, z1, y, rise, Paint(roof), Paint(gable), overhang);
        }
        public void Hip(float x0, float z0, float x1, float z1, float y, float rise, Color32 roof, float overhang = 0.4f)
        {
            Hip(x0, z0, x1, z1, y, rise, Paint(roof), overhang);
        }

        public void Cylinder(Vector3 basePos, float r0, float r1, float h, int n, Color32 side, Color32 top)
        {
            Cylinder(basePos, r0, r1, h, n, Paint(side), Paint(top));
        }

        /// <summary>A rough ball: tree crowns and bushes.</summary>
        public void Blob(Vector3 c, Vector3 r, Color32 col)
        {
            var mat = Paint(col);
            var top = c + new Vector3(0, r.y, 0); var bot = c - new Vector3(0, r.y * 0.7f, 0);
            const int n = 7;
            var ring = new Vector3[n]; var ring2 = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                float wob = 1 + 0.12f * Mathf.Sin(i * 2.3f + c.x);
                ring[i] = c + new Vector3(Mathf.Cos(a) * r.x * wob, r.y * 0.15f, Mathf.Sin(a) * r.z * wob);
                float a2 = a + Mathf.PI / n;
                ring2[i] = c + new Vector3(Mathf.Cos(a2) * r.x * 0.8f, -r.y * 0.4f, Mathf.Sin(a2) * r.z * 0.8f);
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Tri(top, ring[j], ring[i], mat);
                Tri(ring[i], ring[j], ring2[i], mat);
                Tri(ring[j], ring2[j], ring2[i], mat);
                Tri(ring2[i], ring2[j], bot, mat);
            }
        }

        /// <summary>Window panes on a wall facing -z (local space), between x0..x1 and y0..y1; some lit at night.</summary>
        public void WindowsFront(float x0, float x1, float y0, float y1, float z, int columns, int rows, float paneW, float paneH, uint seed, float lit = 0.55f)
        {
            float sx = (x1 - x0) / columns, sy = (y1 - y0) / rows;
            var frame = Paint(new Color32(58, 62, 68, 255));
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    float cx = x0 + sx * (c + 0.5f), cy = y0 + sy * (r + 0.5f);
                    seed = seed * 1664525u + 1013904223u;
                    var glow = (seed >> 24) / 255f < lit ? WindowLight(seed) : new Color32(20, 22, 30, 255);
                    Box(new Vector3(cx - paneW / 2 - 0.06f, cy - paneH / 2 - 0.06f, z - 0.04f), new Vector3(cx + paneW / 2 + 0.06f, cy + paneH / 2 + 0.06f, z), frame, frame);
                    Quad(new Vector3(cx - paneW / 2, cy - paneH / 2, z - 0.05f), new Vector3(cx - paneW / 2, cy + paneH / 2, z - 0.05f),
                         new Vector3(cx + paneW / 2, cy + paneH / 2, z - 0.05f), new Vector3(cx + paneW / 2, cy - paneH / 2, z - 0.05f), glow, true);
                }
        }

        /// <summary>Windows on all four walls of a box.</summary>
        public void WindowsAround(Vector3 min, Vector3 max, float spacing, float floorH, float paneW, float paneH, float yStart, uint seed, float lit = 0.55f, bool skipFront = false)
        {
            float w = max.x - min.x, d = max.z - min.z;
            int rows = Mathf.Max(1, Mathf.FloorToInt((max.y - yStart) / floorH));
            int cw = Mathf.Max(1, Mathf.FloorToInt(w / spacing)), cd = Mathf.Max(1, Mathf.FloorToInt(d / spacing));
            float y1 = yStart + rows * floorH;
            var center = new Vector3((min.x + max.x) / 2, 0, (min.z + max.z) / 2);
            for (int side = 0; side < 4; side++)
            {
                if (skipFront && side == 0) continue;
                float half = side % 2 == 0 ? d / 2 : w / 2;
                float span = side % 2 == 0 ? w : d;
                int count = side % 2 == 0 ? cw : cd;
                Push(center, side * 90);
                WindowsFront(-span / 2, span / 2, yStart, y1, -half, count, rows, paneW, paneH, seed + (uint)side * 7919u, lit);
                Pop();
            }
        }

        static Color32 WindowLight(uint s)
        {
            int k = (int)(s >> 16) % 3;
            return k == 0 ? new Color32(255, 214, 140, 255) : k == 1 ? new Color32(255, 236, 190, 255) : new Color32(250, 196, 120, 255);
        }

        // ------------------------------------------------------------------ out
        public Model ToModel(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(cols);
            mesh.subMeshCount = mats.Count;
            for (int i = 0; i < mats.Count; i++) mesh.SetTriangles(tris[i], i);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return new Model { Mesh = mesh, Materials = mats.ToArray() };
        }

        /// <summary>Everything as one sub-mesh with vertex colours, for an overlay material.</summary>
        public void ToOverlay(Mesh target)
        {
            target.Clear();
            if (verts.Count > 65000) target.indexFormat = IndexFormat.UInt32;
            target.SetVertices(verts);
            target.SetNormals(norms);
            target.SetColors(cols);
            var all = new List<int>();
            foreach (var t in tris) all.AddRange(t);
            target.subMeshCount = 1;
            target.SetTriangles(all, 0);
            target.RecalculateBounds();
        }

        public static Color32 Shade(Color32 c, float f)
        {
            return new Color32((byte)Mathf.Clamp(c.r * f, 0, 255), (byte)Mathf.Clamp(c.g * f, 0, 255), (byte)Mathf.Clamp(c.b * f, 0, 255), c.a);
        }
        public static Color32 Mix(Color32 a, Color32 b, float t)
        {
            return new Color32((byte)Mathf.Lerp(a.r, b.r, t), (byte)Mathf.Lerp(a.g, b.g, t), (byte)Mathf.Lerp(a.b, b.b, t), (byte)Mathf.Lerp(a.a, b.a, t));
        }
        public static Color32 Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }
    }
}
