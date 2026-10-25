using System.Collections.Generic;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>Shortest routes along the road grid, cached until the roads change.</summary>
    public sealed class Nav
    {
        public const float Tile = 10f;
        readonly Town town;
        readonly Dictionary<long, List<Vector2Int>> cache = new Dictionary<long, List<Vector2Int>>();
        int version = -1;
        int[] prev;
        readonly Queue<int> q = new Queue<int>();

        public Nav(Town t) { town = t; }

        public static Vector3 Center(int x, int y) { return new Vector3((x + 0.5f) * Tile, 0, (y + 0.5f) * Tile); }
        public static Vector3 Center(Vector2Int p) { return Center(p.x, p.y); }

        /// <summary>The road tiles from one road tile to another (both included), or null if there's no way.</summary>
        public List<Vector2Int> Route(Vector2Int from, Vector2Int to)
        {
            if (town.MapVersion != version) { cache.Clear(); version = town.MapVersion; }
            long key = ((long)(from.y * town.Cols + from.x) << 20) | (long)(to.y * town.Cols + to.x);
            List<Vector2Int> path;
            if (cache.TryGetValue(key, out path)) return path;
            path = Search(from, to);
            if (cache.Count > 20000) cache.Clear();
            cache[key] = path;
            return path;
        }

        List<Vector2Int> Search(Vector2Int from, Vector2Int to)
        {
            int n = town.Cols * town.Rows;
            if (prev == null || prev.Length != n) prev = new int[n];
            for (int i = 0; i < n; i++) prev[i] = -2;
            int s = from.y * town.Cols + from.x, g = to.y * town.Cols + to.x;
            q.Clear(); q.Enqueue(s); prev[s] = -1;
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                if (i == g) break;
                int x = i % town.Cols, y = i / town.Cols;
                for (int d = 0; d < 4; d++)
                {
                    int nx = x + Town.StepX(d), ny = y + Town.StepY(d);
                    if (!town.IsRoad(nx, ny)) continue;
                    int j = ny * town.Cols + nx;
                    if (prev[j] != -2) continue;
                    prev[j] = i; q.Enqueue(j);
                }
            }
            if (prev[g] == -2) return null;
            var path = new List<Vector2Int>();
            for (int i = g; i != -1; i = prev[i]) path.Add(new Vector2Int(i % town.Cols, i / town.Cols));
            path.Reverse();
            return path;
        }

        /// <summary>Points to walk or drive along: out of the door, down the street keeping to the right, in at the other end.</summary>
        public List<Vector3> Polyline(Building a, Building b, bool car)
        {
            int ax, ay, bx, by;
            if (a == null || b == null || !town.AccessTile(a, out ax, out ay) || !town.AccessTile(b, out bx, out by)) return null;
            var tiles = Route(new Vector2Int(ax, ay), new Vector2Int(bx, by));
            if (tiles == null) return null;
            var pts = new List<Vector3>(tiles.Count + 4);
            pts.Add(Door(a));
            float lane = car ? 2.2f : 4.1f;
            for (int i = 0; i < tiles.Count; i++)
            {
                var c = Center(tiles[i]);
                Vector3 dirIn = i > 0 ? (c - Center(tiles[i - 1])).normalized : Vector3.zero;
                Vector3 dirOut = i < tiles.Count - 1 ? (Center(tiles[i + 1]) - c).normalized : Vector3.zero;
                Vector3 dir = (dirIn + dirOut);
                if (dir.sqrMagnitude < 0.01f) dir = dirIn.sqrMagnitude > 0 ? dirIn : dirOut;
                if (dir.sqrMagnitude < 0.01f) { pts.Add(c); continue; }
                dir.Normalize();
                // keep right of the direction of travel
                var right = new Vector3(dir.z, 0, -dir.x);
                pts.Add(c + right * lane * (Mathf.Abs(dir.x) > 0.1f && Mathf.Abs(dir.z) > 0.1f ? 1.2f : 1f));
            }
            pts.Add(Door(b));
            return pts;
        }

        /// <summary>The middle of a building's street front, just inside the lot.</summary>
        public Vector3 Door(Building b)
        {
            var d = b.Def;
            var c = new Vector3((b.X + d.W * 0.5f) * Tile, 0, (b.Y + d.H * 0.5f) * Tile);
            switch (b.Facing)
            {
                case 0: return c + new Vector3(0, 0, d.H * Tile * 0.5f - 1.2f);
                case 1: return c + new Vector3(d.W * Tile * 0.5f - 1.2f, 0, 0);
                case 2: return c - new Vector3(0, 0, d.H * Tile * 0.5f - 1.2f);
                default: return c - new Vector3(d.W * Tile * 0.5f - 1.2f, 0, 0);
            }
        }

        public static float Length(List<Vector3> pts)
        {
            float l = 0;
            for (int i = 1; i < pts.Count; i++) l += Vector3.Distance(pts[i - 1], pts[i]);
            return l;
        }

        /// <summary>Where along a polyline you are after travelling dist, and which way you face.</summary>
        public static Vector3 At(List<Vector3> pts, float dist, out Vector3 dir)
        {
            dir = Vector3.forward;
            for (int i = 1; i < pts.Count; i++)
            {
                float seg = Vector3.Distance(pts[i - 1], pts[i]);
                if (dist <= seg || i == pts.Count - 1)
                {
                    var d = pts[i] - pts[i - 1];
                    if (d.sqrMagnitude > 0.0001f) dir = d.normalized;
                    return pts[i - 1] + d * (seg > 0 ? Mathf.Clamp01(dist / seg) : 1);
                }
                dist -= seg;
            }
            return pts[pts.Count - 1];
        }
    }
}
