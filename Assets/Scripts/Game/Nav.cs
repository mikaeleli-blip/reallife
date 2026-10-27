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
            Lane(tiles, car ? CarLane : WalkLane, pts, false);
            pts.Add(Door(b));
            return pts;
        }

        public const float CarLane = 2.2f, WalkLane = 4.1f;

        /// <summary>A point for each tile, kept to the right of the direction of travel. A loop's first and last tiles are the same.</summary>
        static void Lane(List<Vector2Int> tiles, float lane, List<Vector3> pts, bool loop)
        {
            int n = tiles.Count;
            for (int i = 0; i < n; i++)
            {
                var c = Center(tiles[i]);
                Vector3 dirIn = i > 0 ? (c - Center(tiles[i - 1])).normalized : loop && n > 2 ? (c - Center(tiles[n - 2])).normalized : Vector3.zero;
                Vector3 dirOut = i < n - 1 ? (Center(tiles[i + 1]) - c).normalized : loop && n > 2 ? (Center(tiles[1]) - c).normalized : Vector3.zero;
                Vector3 dir = (dirIn + dirOut);
                if (dir.sqrMagnitude < 0.01f) dir = dirIn.sqrMagnitude > 0 ? dirIn : dirOut;
                if (dir.sqrMagnitude < 0.01f) { pts.Add(c); continue; }
                dir.Normalize();
                var right = new Vector3(dir.z, 0, -dir.x);
                pts.Add(c + right * lane * (Mathf.Abs(dir.x) > 0.1f && Mathf.Abs(dir.z) > 0.1f ? 1.2f : 1f));
            }
        }

        /// <summary>A drive from one building to another: leave (from where the car stands to the street, may be empty), along
        /// the streets keeping right, and arrive (from the street to where it parks, may be empty: it stops at the kerb).</summary>
        public List<Vector3> Drive(Building a, Building b, List<Vector3> leave, List<Vector3> arrive)
        {
            int ax, ay, bx, by;
            if (a == null || b == null || !town.AccessTile(a, out ax, out ay) || !town.AccessTile(b, out bx, out by)) return null;
            var tiles = Route(new Vector2Int(ax, ay), new Vector2Int(bx, by));
            if (tiles == null) return null;
            var pts = new List<Vector3>(tiles.Count + 8);
            if (leave != null) pts.AddRange(leave);
            Lane(tiles, CarLane, pts, false);
            if (arrive != null) pts.AddRange(arrive);
            return pts;
        }

        /// <summary>Where a car pulls up at the kerb outside a building (the driving lane by its access tile).</summary>
        public Vector3 Kerb(Building b, bool arriving)
        {
            int ax, ay;
            if (b == null || !town.AccessTile(b, out ax, out ay)) return Door(b);
            var c = Center(ax, ay);
            var door = Door(b);
            var toDoor = door - c; toDoor.y = 0;
            // on the building's side of the street, along it a little
            var side = toDoor.sqrMagnitude > 0.01f ? toDoor.normalized : Vector3.forward;
            return c + side * CarLane;
        }

        /// <summary>On foot along the pavements from one building to another, starting and ending at the given points.</summary>
        public List<Vector3> Walk(Vector3 start, Building a, Building b, Vector3 end)
        {
            if (a == b) return new List<Vector3> { start, end };
            int ax, ay, bx, by;
            if (a == null || b == null || !town.AccessTile(a, out ax, out ay) || !town.AccessTile(b, out bx, out by)) return new List<Vector3> { start, end };
            var tiles = Route(new Vector2Int(ax, ay), new Vector2Int(bx, by));
            if (tiles == null) return new List<Vector3> { start, end };
            var pts = new List<Vector3>(tiles.Count + 4) { start, Door(a) };
            Lane(tiles, WalkLane, pts, false);
            pts.Add(Door(b));
            pts.Add(end);
            return pts;
        }

        /// <summary>On foot from a building's door to a spot at a bus stop.</summary>
        public List<Vector3> WalkToStop(Building a, Vector2Int stop, Vector3 spot)
        {
            int ax, ay;
            if (a == null || !town.AccessTile(a, out ax, out ay)) return null;
            var tiles = Route(new Vector2Int(ax, ay), stop);
            if (tiles == null) return null;
            var pts = new List<Vector3>(tiles.Count + 2) { Door(a) };
            Lane(tiles, WalkLane, pts, false);
            pts.Add(spot);
            return pts;
        }

        /// <summary>On foot from a spot at a bus stop to a building's door.</summary>
        public List<Vector3> WalkFromStop(Vector2Int stop, Vector3 spot, Building b)
        {
            int bx, by;
            if (b == null || !town.AccessTile(b, out bx, out by)) return null;
            var tiles = Route(stop, new Vector2Int(bx, by));
            if (tiles == null) return null;
            var pts = new List<Vector3>(tiles.Count + 2) { spot };
            Lane(tiles, WalkLane, pts, false);
            pts.Add(Door(b));
            return pts;
        }

        /// <summary>A bus's loop through its stops and back to the first, in the driving lane. at gets the point index of each
        /// stop along it (the first is 0, and the loop ends back there). Null if a stop can't be reached.</summary>
        public List<Vector3> Loop(List<Vector2Int> stops, List<int> at)
        {
            var tiles = new List<Vector2Int>();
            at.Clear();
            for (int i = 0; i < stops.Count; i++)
            {
                var seg = Route(stops[i], stops[(i + 1) % stops.Count]);
                if (seg == null || seg.Count == 0) return null;
                at.Add(tiles.Count == 0 ? 0 : tiles.Count - 1);
                for (int j = tiles.Count == 0 ? 0 : 1; j < seg.Count; j++) tiles.Add(seg[j]);
            }
            if (tiles.Count < 2) return null;
            var pts = new List<Vector3>(tiles.Count);
            Lane(tiles, CarLane, pts, true);
            return pts;
        }

        /// <summary>The distance along a polyline to each of its points.</summary>
        public static float[] Along(List<Vector3> pts)
        {
            var d = new float[pts.Count];
            for (int i = 1; i < pts.Count; i++) d[i] = d[i - 1] + Vector3.Distance(pts[i - 1], pts[i]);
            return d;
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
