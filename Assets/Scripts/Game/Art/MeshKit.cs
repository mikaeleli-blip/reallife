using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RealLife.Game.Art
{
    /// <summary>
    /// Builds flat-shaded, vertex-coloured low-poly meshes. Every face gets its own vertices and normal, so the lighting reads as
    /// crisp facets. Two submeshes: 0 is the solid body, 1 is window glass (lit at night by the window material).
    /// </summary>
    public sealed class MeshKit
    {
        readonly List<Vector3> verts = new List<Vector3>(1024);
        readonly List<Vector3> norms = new List<Vector3>(1024);
        readonly List<Color32> cols = new List<Color32>(1024);
        readonly List<int> body = new List<int>(2048);
        readonly List<int> glass = new List<int>(512);
        Matrix4x4 m = Matrix4x4.identity;
        bool flipped;

        public int VertexCount { get { return verts.Count; } }
        public void Clear() { verts.Clear(); norms.Clear(); cols.Clear(); body.Clear(); glass.Clear(); m = Matrix4x4.identity; flipped = false; stack.Clear(); }

        /// <summary>Everything added after this is moved, turned and scaled by the matrix.</summary>
        public void SetTransform(Matrix4x4 t) { m = t; flipped = t.determinant < 0; }
        public void ResetTransform() { m = Matrix4x4.identity; flipped = false; }
        public void SetTransform(Vector3 pos, float yawDeg) { SetTransform(Matrix4x4.TRS(pos, Quaternion.Euler(0, yawDeg, 0), Vector3.one)); }
        readonly Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
        /// <summary>Moves and turns what's added next, on top of the current transform, until Pop.</summary>
        public void Push(Vector3 pos, float yawDeg) { stack.Push(m); SetTransform(m * Matrix4x4.TRS(pos, Quaternion.Euler(0, yawDeg, 0), Vector3.one)); }
        public void Push(Vector3 pos, float yawDeg, Vector3 scale) { stack.Push(m); SetTransform(m * Matrix4x4.TRS(pos, Quaternion.Euler(0, yawDeg, 0), scale)); }
        public void Pop() { SetTransform(stack.Count > 0 ? stack.Pop() : Matrix4x4.identity); }

        // ------------------------------------------------------------------ faces
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Color32 col, bool window = false)
        {
            a = m.MultiplyPoint3x4(a); b = m.MultiplyPoint3x4(b); c = m.MultiplyPoint3x4(c);
            if (flipped) { var t = b; b = c; c = t; }
            var n = Vector3.Cross(b - a, c - a).normalized;
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            norms.Add(n); norms.Add(n); norms.Add(n);
            cols.Add(col); cols.Add(col); cols.Add(col);
            var l = window ? glass : body;
            l.Add(i); l.Add(i + 1); l.Add(i + 2);
        }

        /// <summary>A flat four-sided face, corners in clockwise order seen from the front.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 col, bool window = false)
        {
            a = m.MultiplyPoint3x4(a); b = m.MultiplyPoint3x4(b); c = m.MultiplyPoint3x4(c); d = m.MultiplyPoint3x4(d);
            if (flipped) { var t = b; b = d; d = t; }
            var n = Vector3.Cross(b - a, c - a).normalized;
            if (n.sqrMagnitude < 0.5f) n = Vector3.Cross(c - a, d - a).normalized;
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            norms.Add(n); norms.Add(n); norms.Add(n); norms.Add(n);
            cols.Add(col); cols.Add(col); cols.Add(col); cols.Add(col);
            var l = window ? glass : body;
            l.Add(i); l.Add(i + 1); l.Add(i + 2); l.Add(i); l.Add(i + 2); l.Add(i + 3);
        }

        /// <summary>A flat rectangle lying on the ground (facing up), from min to max corner.</summary>
        public void Flat(float x0, float z0, float x1, float z1, float y, Color32 col)
        {
            Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0), col);
        }

        /// <summary>A box from min to max. top/side colours; bottom is never seen so it's skipped.</summary>
        public void Box(Vector3 min, Vector3 max, Color32 side, Color32 top, bool skipBottom = true)
        {
            float x0 = min.x, y0 = min.y, z0 = min.z, x1 = max.x, y1 = max.y, z1 = max.z;
            // front (-z), back (+z), left (-x), right (+x)
            Quad(new Vector3(x0, y0, z0), new Vector3(x0, y1, z0), new Vector3(x1, y1, z0), new Vector3(x1, y0, z0), side);
            Quad(new Vector3(x1, y0, z1), new Vector3(x1, y1, z1), new Vector3(x0, y1, z1), new Vector3(x0, y0, z1), Shade(side, 0.86f));
            Quad(new Vector3(x0, y0, z1), new Vector3(x0, y1, z1), new Vector3(x0, y1, z0), new Vector3(x0, y0, z0), Shade(side, 0.93f));
            Quad(new Vector3(x1, y0, z0), new Vector3(x1, y1, z0), new Vector3(x1, y1, z1), new Vector3(x1, y0, z1), Shade(side, 0.93f));
            Quad(new Vector3(x0, y1, z0), new Vector3(x0, y1, z1), new Vector3(x1, y1, z1), new Vector3(x1, y1, z0), top);
            if (!skipBottom) Quad(new Vector3(x0, y0, z1), new Vector3(x0, y0, z0), new Vector3(x1, y0, z0), new Vector3(x1, y0, z1), Shade(side, 0.6f));
        }
        public void Box(Vector3 min, Vector3 max, Color32 col) { Box(min, max, col, Shade(col, 1.08f)); }

        /// <summary>A gable roof over a box footprint, ridge running along x. Overhang in metres.</summary>
        public void Gable(float x0, float z0, float x1, float z1, float y, float rise, Color32 roof, Color32 gable, float overhang = 0.4f)
        {
            float zm = (z0 + z1) / 2;
            float ox0 = x0 - overhang, ox1 = x1 + overhang, oz0 = z0 - overhang, oz1 = z1 + overhang;
            float drop = rise * overhang / ((z1 - z0) / 2);
            // the two slopes
            Quad(new Vector3(ox0, y - drop, oz0), new Vector3(ox0, y + rise, zm), new Vector3(ox1, y + rise, zm), new Vector3(ox1, y - drop, oz0), roof);
            Quad(new Vector3(ox1, y - drop, oz1), new Vector3(ox1, y + rise, zm), new Vector3(ox0, y + rise, zm), new Vector3(ox0, y - drop, oz1), Shade(roof, 0.82f));
            // undersides of the eaves
            Quad(new Vector3(ox0, y - drop, oz0), new Vector3(ox1, y - drop, oz0), new Vector3(ox1, y + rise, zm), new Vector3(ox0, y + rise, zm), Shade(roof, 0.5f));
            Quad(new Vector3(ox1, y - drop, oz1), new Vector3(ox0, y - drop, oz1), new Vector3(ox0, y + rise, zm), new Vector3(ox1, y + rise, zm), Shade(roof, 0.5f));
            // gable ends
            Tri(new Vector3(x0, y, z1), new Vector3(x0, y + rise, zm), new Vector3(x0, y, z0), Shade(gable, 0.93f));
            Tri(new Vector3(x1, y, z0), new Vector3(x1, y + rise, zm), new Vector3(x1, y, z1), Shade(gable, 0.93f));
        }

        /// <summary>A four-sided hip roof (a pyramid when square).</summary>
        public void Hip(float x0, float z0, float x1, float z1, float y, float rise, Color32 roof, float overhang = 0.4f)
        {
            x0 -= overhang; z0 -= overhang; x1 += overhang; z1 += overhang;
            float w = x1 - x0, d = z1 - z0;
            float inset = Mathf.Min(w, d) / 2;
            var r0 = w >= d ? new Vector3(x0 + inset, y + rise, (z0 + z1) / 2) : new Vector3((x0 + x1) / 2, y + rise, z0 + inset);
            var r1 = w >= d ? new Vector3(x1 - inset, y + rise, (z0 + z1) / 2) : new Vector3((x0 + x1) / 2, y + rise, z1 - inset);
            var a = new Vector3(x0, y, z0); var b = new Vector3(x1, y, z0); var c = new Vector3(x1, y, z1); var e = new Vector3(x0, y, z1);
            if (w >= d)
            {
                Quad(a, r0, r1, b, roof);
                Quad(c, r1, r0, e, Shade(roof, 0.82f));
                Tri(e, r0, a, Shade(roof, 0.9f));
                Tri(b, r1, c, Shade(roof, 0.9f));
            }
            else
            {
                Tri(a, r0, b, roof);
                Tri(c, r1, e, Shade(roof, 0.82f));
                Quad(e, r1, r0, a, Shade(roof, 0.9f));
                Quad(b, r0, r1, c, Shade(roof, 0.9f));
            }
        }

        /// <summary>An upright cylinder (or cone, with r1 = 0) with n sides.</summary>
        public void Cylinder(Vector3 basePos, float r0, float r1, float h, int n, Color32 side, Color32 top)
        {
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2 / n, a1 = (i + 1) * Mathf.PI * 2 / n;
                var p0 = basePos + new Vector3(Mathf.Cos(a0) * r0, 0, Mathf.Sin(a0) * r0);
                var p1 = basePos + new Vector3(Mathf.Cos(a1) * r0, 0, Mathf.Sin(a1) * r0);
                var q0 = basePos + new Vector3(Mathf.Cos(a0) * r1, h, Mathf.Sin(a0) * r1);
                var q1 = basePos + new Vector3(Mathf.Cos(a1) * r1, h, Mathf.Sin(a1) * r1);
                var c = Shade(side, 0.82f + 0.18f * Mathf.Abs(Mathf.Cos((a0 + a1) / 2 - 2.2f)));
                if (r1 <= 0.001f) Tri(p0, q0, p1, c);
                else
                {
                    Quad(p0, q0, q1, p1, c);
                    Tri(basePos + Vector3.up * h, q1, q0, top);
                }
            }
        }

        /// <summary>A rough ball: an octahedron-ish blob for tree crowns and heads.</summary>
        public void Blob(Vector3 c, Vector3 r, Color32 col)
        {
            var top = c + new Vector3(0, r.y, 0); var bot = c - new Vector3(0, r.y * 0.7f, 0);
            const int n = 6;
            var ring = new Vector3[n]; var ring2 = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2 / n;
                ring[i] = c + new Vector3(Mathf.Cos(a) * r.x, r.y * 0.15f, Mathf.Sin(a) * r.z);
                float a2 = a + Mathf.PI / n;
                ring2[i] = c + new Vector3(Mathf.Cos(a2) * r.x * 0.8f, -r.y * 0.4f, Mathf.Sin(a2) * r.z * 0.8f);
            }
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Tri(top, ring[j], ring[i], Shade(col, 1.08f - 0.1f * (i % 2)));
                Tri(ring[i], ring[j], ring2[i], Shade(col, 0.92f));
                Tri(ring[j], ring2[j], ring2[i], Shade(col, 0.85f));
                Tri(ring2[i], ring2[j], bot, Shade(col, 0.7f));
            }
        }

        /// <summary>A grid of window panes on a wall facing -z (in local space), between x0..x1 and y0..y1.</summary>
        public void WindowsFront(float x0, float x1, float y0, float y1, float z, int cols, int rows, float paneW, float paneH, uint seed, float lit = 0.55f)
        {
            float sx = (x1 - x0) / cols, sy = (y1 - y0) / rows;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float cx = x0 + sx * (c + 0.5f), cy = y0 + sy * (r + 0.5f);
                    seed = seed * 1664525u + 1013904223u;
                    var glow = (seed >> 24) / 255f < lit ? WindowLight(seed) : new Color32(20, 22, 30, 255);
                    Quad(new Vector3(cx - paneW / 2, cy - paneH / 2, z - 0.03f), new Vector3(cx - paneW / 2, cy + paneH / 2, z - 0.03f),
                         new Vector3(cx + paneW / 2, cy + paneH / 2, z - 0.03f), new Vector3(cx + paneW / 2, cy - paneH / 2, z - 0.03f), glow, true);
                }
        }

        /// <summary>Windows on all four walls of a box.</summary>
        public void WindowsAround(Vector3 min, Vector3 max, float spacing, float floorH, float paneW, float paneH, float yStart, uint seed, float lit = 0.55f, bool skipFront = false)
        {
            float w = max.x - min.x, d = max.z - min.z;
            int rows = Mathf.Max(1, Mathf.FloorToInt((max.y - yStart) / floorH));
            int cw = Mathf.Max(1, Mathf.FloorToInt(w / spacing)), cd = Mathf.Max(1, Mathf.FloorToInt(d / spacing));
            float y1 = yStart + rows * floorH;
            var keep = m; bool keepFlip = flipped;
            var center = new Vector3((min.x + max.x) / 2, 0, (min.z + max.z) / 2);
            for (int side = 0; side < 4; side++)
            {
                if (skipFront && side == 0) continue;
                float half = side % 2 == 0 ? d / 2 : w / 2;
                float span = side % 2 == 0 ? w : d;
                int count = side % 2 == 0 ? cw : cd;
                SetTransform(keep * Matrix4x4.TRS(center, Quaternion.Euler(0, side * 90, 0), Vector3.one));
                WindowsFront(-span / 2, span / 2, yStart, y1, -half, count, rows, paneW, paneH, seed + (uint)side * 7919u, lit);
            }
            m = keep; flipped = keepFlip;
        }

        static Color32 WindowLight(uint s)
        {
            int k = (int)(s >> 16) % 3;
            return k == 0 ? new Color32(255, 214, 140, 255) : k == 1 ? new Color32(255, 236, 190, 255) : new Color32(250, 196, 120, 255);
        }

        // ------------------------------------------------------------------ out
        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetColors(cols);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(body, 0);
            mesh.SetTriangles(glass, 1);
            mesh.RecalculateBounds();
            return mesh;
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
