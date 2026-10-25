using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// Draws the town: textured ground, water, woods, roads, zoned lots and every building, rebuilt when the map changes;
    /// the country round it; the selection outline and the placement preview. The sky and light are the Atmosphere's.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        public const float Tile = Nav.Tile;
        const int Chunk = 16;
        const float Country = 2200f;   // how far the land runs past the edge of the map

        Town town;
        CameraRig rig;
        Atmosphere atmo;
        Material overlay, zoneMat;

        readonly Dictionary<int, GameObject> chunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> treeChunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, int> treeHash = new Dictionary<int, int>();
        readonly Dictionary<int, GameObject> zoneChunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, BuildingObj> buildings = new Dictionary<int, BuildingObj>();
        GameObject countryside;
        int mapVersion = -1, season = -1, countrySeason = -1;
        bool zoneStrong, zoneStrongShown;
        GameObject ghost, highlight, hover;
        Mesh ghostMesh, highlightMesh, hoverMesh;

        sealed class BuildingObj { public GameObject Go; public string Key; public Mesh Mesh; }

        public Nav Nav { get; private set; }
        public float Night { get { return atmo != null ? atmo.Night : 0f; } }

        // ================================================================== setup
        void Awake()
        {
            overlay = MaterialLibrary.Overlay();
            zoneMat = MaterialLibrary.Overlay();
            ghost = Obj("Ghost", overlay, out ghostMesh);
            highlight = Obj("Selection", overlay, out highlightMesh);
            hover = Obj("Hover", overlay, out hoverMesh);
        }

        GameObject Obj(string name, Material mat, out Mesh mesh)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return go;
        }

        public void Bind(Town t, CameraRig r)
        {
            town = t; rig = r;
            if (atmo == null)
            {
                atmo = gameObject.AddComponent<Atmosphere>();
                atmo.Init(r.Cam);
            }
            Nav = new Nav(t);
            foreach (var c in chunks.Values) Destroy(c);
            foreach (var c in treeChunks.Values) Destroy(c);
            foreach (var c in zoneChunks.Values) Destroy(c);
            foreach (var b in buildings.Values) { Destroy(b.Go); if (b.Mesh != null) Destroy(b.Mesh); }
            if (countryside != null) Destroy(countryside);
            chunks.Clear(); treeChunks.Clear(); treeHash.Clear(); zoneChunks.Clear(); buildings.Clear();
            mapVersion = -1; season = -1; countrySeason = -1;
            var agents = GetComponent<Agents>();
            if (agents == null) agents = gameObject.AddComponent<Agents>();
            agents.Bind(t, this);
        }

        public void SetZoneEmphasis(bool on) { zoneStrong = on; }

        void LateUpdate()
        {
            if (town == null) return;
            bool all = town.Season != season;
            if (all || town.MapVersion != mapVersion || zoneStrong != zoneStrongShown)
            {
                season = town.Season;
                if (all) treeHash.Clear();
                mapVersion = town.MapVersion; zoneStrongShown = zoneStrong;
                RebuildGround();
                RebuildZones();
                SyncBuildings(all);
                if (countrySeason != season) RebuildCountry();
            }
            else if (Time.frameCount % 15 == 0) SyncBuildings(false);
            if (atmo != null) atmo.Apply(town.Hour, season, rig != null ? rig.Distance : 300f);
            float zl = Mathf.Lerp(1f, 0.32f, Night);
            zoneMat.SetColor("_Color", new Color(zl, zl, zl, Mathf.Lerp(1f, 0.6f, Night)));
        }

        // ================================================================== materials
        static Material S(string n) { return MaterialLibrary.Surface(n); }

        Material Grass(bool woods)
        {
            if (season == 0) return S("snow");
            var tint = BuildingArt.GrassTint(season);
            if (woods) tint = new Color(tint.r * 0.86f, tint.g * 0.86f, tint.b * 0.86f, 1f);
            return MaterialLibrary.Tinted("grass", tint);
        }

        Material Water { get { return season == 0 ? MaterialLibrary.Tinted("snow", new Color(0.86f, 0.92f, 1f)) : S("water"); } }
        Material Bank { get { return season == 0 ? S("snow") : S("sand"); } }
        Material Asphalt { get { return season == 0 ? MaterialLibrary.Tinted("asphalt", new Color(1.2f, 1.22f, 1.26f)) : S("asphalt"); } }
        Material Walk { get { return season == 0 ? S("snow") : S("sidewalk"); } }
        Material Curb { get { return S("concrete"); } }

        // ================================================================== ground, water, roads and trees
        void RebuildGround()
        {
            int cw = (town.Cols + Chunk - 1) / Chunk, ch = (town.Rows + Chunk - 1) / Chunk;
            var k = new MeshKit();
            for (int cy = 0; cy < ch; cy++)
                for (int cx = 0; cx < cw; cx++)
                {
                    int key = cy * cw + cx;
                    k.Clear();
                    for (int y = cy * Chunk; y < Mathf.Min(town.Rows, (cy + 1) * Chunk); y++)
                        for (int x = cx * Chunk; x < Mathf.Min(town.Cols, (cx + 1) * Chunk); x++)
                        {
                            GroundTile(k, x, y);
                            if (town.IsRoad(x, y)) RoadTile(k, x, y);
                        }
                    SetChunk(chunks, key, "Ground " + key, k, false, false);
                    // trees only change when the woods do
                    int hash = 17;
                    for (int y = cy * Chunk; y < Mathf.Min(town.Rows, (cy + 1) * Chunk); y++)
                        for (int x = cx * Chunk; x < Mathf.Min(town.Cols, (cx + 1) * Chunk); x++)
                            if (town.HasTrees(x, y) && town.BuildingAt(x, y) == null && !town.IsRoad(x, y)) hash = hash * 31 + y * town.Cols + x;
                    int old;
                    if (treeHash.TryGetValue(key, out old) && old == hash) continue;
                    treeHash[key] = hash;
                    k.Clear();
                    for (int y = cy * Chunk; y < Mathf.Min(town.Rows, (cy + 1) * Chunk); y++)
                        for (int x = cx * Chunk; x < Mathf.Min(town.Cols, (cx + 1) * Chunk); x++)
                            if (town.HasTrees(x, y) && town.BuildingAt(x, y) == null && !town.IsRoad(x, y)) Trees(k, x, y);
                    SetChunk(treeChunks, key, "Trees " + key, k, true, false);
                }
        }

        void SetChunk(Dictionary<int, GameObject> set, int key, string name, MeshKit k, bool shadows, bool isOverlay)
        {
            GameObject go;
            if (!set.TryGetValue(key, out go))
            {
                go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                if (isOverlay) { r.sharedMaterial = zoneMat; r.receiveShadows = false; }
                set[key] = go;
            }
            var mf = go.GetComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>();
            if (isOverlay)
            {
                if (mf.sharedMesh == null) mf.sharedMesh = new Mesh { name = name };
                k.ToOverlay(mf.sharedMesh);
                return;
            }
            if (mf.sharedMesh != null) Destroy(mf.sharedMesh);
            if (k.VertexCount == 0) { mf.sharedMesh = null; return; }
            var m = k.ToModel(name);
            mf.sharedMesh = m.Mesh;
            mr.sharedMaterials = m.Materials;
        }

        static uint Hash(int x, int y) { uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return h; }

        void GroundTile(MeshKit k, int x, int y)
        {
            float x0 = x * Tile, z0 = y * Tile, x1 = x0 + Tile, z1 = z0 + Tile;
            int t = town.Terrain(x, y);
            if (t == Town.TileBits.Water)
            {
                k.Flat(x0, z0, x1, z1, -0.9f, Water, 12f);
                // banks down to the water
                var bank = Bank;
                if (!town.IsWater(x, y - 1) && y > 0) k.Quad(new Vector3(x1, -0.9f, z0), new Vector3(x1, 0, z0), new Vector3(x0, 0, z0), new Vector3(x0, -0.9f, z0), bank, 3f);
                if (!town.IsWater(x, y + 1) && y < town.Rows - 1) k.Quad(new Vector3(x0, -0.9f, z1), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1), new Vector3(x1, -0.9f, z1), bank, 3f);
                if (!town.IsWater(x - 1, y) && x > 0) k.Quad(new Vector3(x0, -0.9f, z0), new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), new Vector3(x0, -0.9f, z1), bank, 3f);
                if (!town.IsWater(x + 1, y) && x < town.Cols - 1) k.Quad(new Vector3(x1, -0.9f, z1), new Vector3(x1, 0, z1), new Vector3(x1, 0, z0), new Vector3(x1, -0.9f, z0), bank, 3f);
                return;
            }
            var m = t == Town.TileBits.Sand ? Bank : Grass(t == Town.TileBits.Trees);
            k.Flat(x0, z0, x1, z1, 0, m, 8f);
        }

        void Trees(MeshKit k, int x, int y)
        {
            uint h = Hash(x, y);
            int n = 1 + (int)(h % 3);
            for (int i = 0; i < n; i++)
            {
                h = h * 1664525u + 1013904223u;
                float ox = ((h >> 8) & 255) / 255f * 7f + 1.5f, oz = ((h >> 16) & 255) / 255f * 7f + 1.5f;
                var p = new Vector3(x * Tile + ox, 0, y * Tile + oz);
                float s = 0.8f + ((h >> 4) & 15) / 30f;
                if ((h >> 24) % 3 == 0) PropArt.Pine(k, p, s * 1.2f, season);
                else PropArt.Tree(k, p, s, season, h);
            }
        }

        bool RoadAt(int x, int y)
        {
            if (!town.InBounds(x, y)) return x < 0 && town.IsRoad(0, y);   // the highway runs off the west edge
            return town.IsRoad(x, y);
        }

        static readonly Color32 Paint = MeshKit.Hex("#f4f1e4"), Yellow = MeshKit.Hex("#e8c547");

        void RoadTile(MeshKit k, int x, int y)
        {
            float x0 = x * Tile, z0 = y * Tile, x1 = x0 + Tile, z1 = z0 + Tile, cx = x0 + Tile / 2, cz = z0 + Tile / 2;
            const float sw = 1.8f, sh = 0.2f;
            bool n = RoadAt(x, y + 1), e = RoadAt(x + 1, y), s = RoadAt(x, y - 1), w = RoadAt(x - 1, y);
            int links = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);
            k.Flat(x0, z0, x1, z1, 0.05f, Asphalt, 8f);
            var curb = Curb; var walk = Walk;
            // pavements on the sides with no road, and the four corners
            if (!n) k.Box(new Vector3(x0, 0, z1 - sw), new Vector3(x1, sh, z1), curb, walk, 2f);
            if (!s) k.Box(new Vector3(x0, 0, z0), new Vector3(x1, sh, z0 + sw), curb, walk, 2f);
            if (!e) k.Box(new Vector3(x1 - sw, 0, z0), new Vector3(x1, sh, z1), curb, walk, 2f);
            if (!w) k.Box(new Vector3(x0, 0, z0), new Vector3(x0 + sw, sh, z1), curb, walk, 2f);
            if (n && e) k.Box(new Vector3(x1 - sw, 0, z1 - sw), new Vector3(x1, sh, z1), curb, walk, 2f);
            if (n && w) k.Box(new Vector3(x0, 0, z1 - sw), new Vector3(x0 + sw, sh, z1), curb, walk, 2f);
            if (s && e) k.Box(new Vector3(x1 - sw, 0, z0), new Vector3(x1, sh, z0 + sw), curb, walk, 2f);
            if (s && w) k.Box(new Vector3(x0, 0, z0), new Vector3(x0 + sw, sh, z0 + sw), curb, walk, 2f);
            if (season != 0)
            {
                // markings: dashes down straight roads, zebra crossings at junctions
                if (links == 2 && n && s) for (float z = z0 + 1; z < z1; z += 3.4f) k.Flat(cx - 0.1f, z, cx + 0.1f, z + 1.7f, 0.07f, Yellow);
                else if (links == 2 && e && w) for (float xx = x0 + 1; xx < x1; xx += 3.4f) k.Flat(xx, cz - 0.1f, xx + 1.7f, cz + 0.1f, 0.07f, Yellow);
                else if (links >= 3)
                {
                    if (n) Zebra(k, x0 + sw, z1 - 1.5f, x1 - sw, z1 - 0.3f, true);
                    if (s) Zebra(k, x0 + sw, z0 + 0.3f, x1 - sw, z0 + 1.5f, true);
                    if (e) Zebra(k, x1 - 1.5f, z0 + sw, x1 - 0.3f, z1 - sw, false);
                    if (w) Zebra(k, x0 + 0.3f, z0 + sw, x0 + 1.5f, z1 - sw, false);
                }
            }
            // street lights on every other tile
            if (links <= 2 && ((x + y) & 1) == 0)
            {
                if (!n) PropArt.Lamp(k, new Vector3(cx, sh, z1 - 0.6f));
                else if (!e) PropArt.Lamp(k, new Vector3(x1 - 0.6f, sh, cz));
                else if (!s) PropArt.Lamp(k, new Vector3(cx, sh, z0 + 0.6f));
                else if (!w) PropArt.Lamp(k, new Vector3(x0 + 0.6f, sh, cz));
            }
        }

        static void Zebra(MeshKit k, float x0, float z0, float x1, float z1, bool acrossX)
        {
            if (acrossX) for (float x = x0 + 0.2f; x < x1 - 0.3f; x += 0.9f) k.Flat(x, z0, x + 0.5f, z1, 0.07f, Paint);
            else for (float z = z0 + 0.2f; z < z1 - 0.3f; z += 0.9f) k.Flat(x0, z, x1, z + 0.5f, 0.07f, Paint);
        }

        // ================================================================== the country round the map
        /// <summary>Farmland and woods running off to the horizon on every side, rivers and the highway carried on out.</summary>
        void RebuildCountry()
        {
            countrySeason = season;
            var k = new MeshKit();
            float W = town.Cols * Tile, Hh = town.Rows * Tile, L = Country;
            var grass = Grass(false);
            // corners
            k.Flat(-L, -L, 0, 0, 0, grass, 8f); k.Flat(W, -L, W + L, 0, 0, grass, 8f);
            k.Flat(-L, Hh, 0, Hh + L, 0, grass, 8f); k.Flat(W, Hh, W + L, Hh + L, 0, grass, 8f);
            // the four edges, a strip per tile so rivers and the highway carry on out of the map
            for (int x = 0; x < town.Cols; x++)
            {
                EdgeStrip(k, x, 0, new Vector3(x * Tile, 0, 0), new Vector3((x + 1) * Tile, 0, 0), Vector3.back, WaterIn(x - 1, 0), WaterIn(x + 1, 0));
                EdgeStrip(k, x, town.Rows - 1, new Vector3((x + 1) * Tile, 0, Hh), new Vector3(x * Tile, 0, Hh), Vector3.forward, WaterIn(x + 1, town.Rows - 1), WaterIn(x - 1, town.Rows - 1));
            }
            for (int y = 0; y < town.Rows; y++)
            {
                EdgeStrip(k, 0, y, new Vector3(0, 0, (y + 1) * Tile), new Vector3(0, 0, y * Tile), Vector3.left, WaterIn(0, y + 1), WaterIn(0, y - 1));
                EdgeStrip(k, town.Cols - 1, y, new Vector3(W, 0, y * Tile), new Vector3(W, 0, (y + 1) * Tile), Vector3.right, WaterIn(town.Cols - 1, y - 1), WaterIn(town.Cols - 1, y + 1));
            }
            if (countryside == null)
            {
                countryside = new GameObject("Countryside");
                countryside.transform.SetParent(transform, false);
                countryside.AddComponent<MeshFilter>();
                countryside.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            var mf = countryside.GetComponent<MeshFilter>();
            if (mf.sharedMesh != null) Destroy(mf.sharedMesh);
            var m = k.ToModel("Countryside");
            mf.sharedMesh = m.Mesh;
            countryside.GetComponent<MeshRenderer>().sharedMaterials = m.Materials;
        }

        /// <summary>The land beyond one edge tile: from a to b along the map edge (b to the right looking out), out along `outward`.</summary>
        void EdgeStrip(MeshKit k, int tx, int ty, Vector3 a, Vector3 b, Vector3 outward, bool waterLeft, bool waterRight)
        {
            float L = Country;
            Vector3 fa = a + outward * L, fb = b + outward * L;
            bool water = town.IsWater(tx, ty);
            bool road = town.IsRoad(tx, ty) && RoadAt(tx + (int)outward.x, ty + (int)outward.z);
            if (water)
            {
                var down = Vector3.down * 0.9f;
                Quad(k, a + down, fa + down, fb + down, b + down, Water, 12f);
                var bank = Bank;
                if (!waterLeft) Wall(k, a, fa, -0.9f, 0f, (b - a).normalized, bank);
                if (!waterRight) Wall(k, b, fb, -0.9f, 0f, (a - b).normalized, bank);
                return;
            }
            if (road)
            {
                // the highway: asphalt, a verge either side, a dashed centre line, lamps for a stretch
                Vector3 side = (b - a).normalized;
                Quad(k, a + Vector3.up * 0.05f, fa + Vector3.up * 0.05f, fb + Vector3.up * 0.05f, b + Vector3.up * 0.05f, Asphalt, 8f);
                var walk = Walk;
                Quad(k, a + Vector3.up * 0.2f, fa + Vector3.up * 0.2f, fa + side * 1.8f + Vector3.up * 0.2f, a + side * 1.8f + Vector3.up * 0.2f, walk, 2f);
                Quad(k, b - side * 1.8f + Vector3.up * 0.2f, fb - side * 1.8f + Vector3.up * 0.2f, fb + Vector3.up * 0.2f, b + Vector3.up * 0.2f, walk, 2f);
                Vector3 mid = (a + b) / 2;
                if (season != 0)
                    for (float d = 1; d < 600; d += 3.4f)
                    {
                        Vector3 p = mid + outward * d + Vector3.up * 0.07f;
                        Quad(k, p - side * 0.1f, p - side * 0.1f + outward * 1.7f, p + side * 0.1f + outward * 1.7f, p + side * 0.1f, MaterialLibrary.Get(Yellow), 2f);
                    }
                for (float d = 10; d < 300; d += 20) PropArt.Lamp(k, a + side * 0.6f + outward * d + Vector3.up * 0.2f);
                return;
            }
            Quad(k, a, fa, fb, b, Grass(false), 8f);
            // woods thinning out with distance
            uint h = Hash(tx * 7 + (int)(outward.x * 3), ty * 7 + (int)(outward.z * 3));
            for (int i = 0; i < 3; i++)
            {
                h = h * 1664525u + 1013904223u;
                float along = ((h >> 8) & 255) / 255f, dist = 6f + Mathf.Pow(((h >> 16) & 255) / 255f, 1.6f) * 160f;
                if (((h >> 4) & 15) < 4) continue;
                var p = Vector3.Lerp(a, b, 0.15f + along * 0.7f) + outward * dist;
                float s = 0.85f + ((h >> 24) & 15) / 30f;
                if ((h >> 28) % 2 == 0) PropArt.Pine(k, p, s * 1.2f, season);
                else PropArt.Tree(k, p, s, season, h);
            }
        }

        /// <summary>An upright quad along p..q between two heights, facing `facing`.</summary>
        static void Wall(MeshKit k, Vector3 p, Vector3 q, float y0, float y1, Vector3 facing, Material m)
        {
            Vector3 a = new Vector3(p.x, y0, p.z), b = new Vector3(p.x, y1, p.z), c = new Vector3(q.x, y1, q.z), d = new Vector3(q.x, y0, q.z);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), facing) >= 0) k.Quad(a, b, c, d, m, 3f);
            else k.Quad(d, c, b, a, m, 3f);
        }

        bool WaterIn(int x, int y) { return town.InBounds(x, y) && town.IsWater(x, y); }

        /// <summary>A quad from four corners given anticlockwise seen from above (a, then along the left side, ...); faces up.</summary>
        static void Quad(MeshKit k, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Material m, float tile)
        {
            var n = Vector3.Cross(b - a, c - a);
            if (n.y >= 0) k.Quad(a, b, c, d, m, tile);
            else k.Quad(a, d, c, b, m, tile);
        }

        // ================================================================== zoned land
        public static Color ZoneColor(Zone z)
        {
            switch (z)
            {
                case Zone.Residential: return new Color(0.35f, 0.85f, 0.4f);
                case Zone.Commercial: return new Color(0.3f, 0.6f, 1f);
                case Zone.Industrial: return new Color(1f, 0.78f, 0.25f);
                default: return Color.clear;
            }
        }

        void RebuildZones()
        {
            int cw = (town.Cols + Chunk - 1) / Chunk, ch = (town.Rows + Chunk - 1) / Chunk;
            var k = new MeshKit();
            byte a = (byte)(zoneStrong ? 120 : 55);
            for (int cy = 0; cy < ch; cy++)
                for (int cx = 0; cx < cw; cx++)
                {
                    k.Clear();
                    for (int y = cy * Chunk; y < Mathf.Min(town.Rows, (cy + 1) * Chunk); y++)
                        for (int x = cx * Chunk; x < Mathf.Min(town.Cols, (cx + 1) * Chunk); x++)
                        {
                            var z = town.ZoneAt(x, y);
                            if (z == Zone.None || town.BuildingAt(x, y) != null) continue;
                            Color32 c = ZoneColor(z); c.a = a;
                            k.Flat(x * Tile + 0.6f, y * Tile + 0.6f, x * Tile + Tile - 0.6f, y * Tile + Tile - 0.6f, 0.12f, c);
                            Color32 edge = ZoneColor(z); edge.a = (byte)(a + 60);
                            k.Flat(x * Tile + 0.6f, y * Tile + 0.6f, x * Tile + Tile - 0.6f, y * Tile + 1.0f, 0.13f, edge);
                        }
                    SetChunk(zoneChunks, cy * cw + cx, "Zones", k, false, true);
                }
        }

        // ================================================================== buildings
        public static Vector3 FootprintCenter(Building b)
        {
            var d = b.Def;
            return new Vector3((b.X + d.W * 0.5f) * Tile, 0, (b.Y + d.H * 0.5f) * Tile);
        }

        public static float Yaw(int facing) { return facing == 0 ? 180 : facing == 1 ? -90 : facing == 2 ? 0 : 90; }

        void SyncBuildings(bool rebuildAll)
        {
            var seen = new HashSet<int>();
            foreach (var b in town.Buildings)
            {
                seen.Add(b.Id);
                var d = b.Def;
                int progressStep = b.Built ? 10 : Mathf.FloorToInt(b.BuildFraction * 8);
                string key = b.Type + "|" + b.Built + "|" + progressStep + "|" + b.Started + "|" + b.Closed + "|" + b.Facing + "|" + season;
                BuildingObj o;
                if (!buildings.TryGetValue(b.Id, out o))
                {
                    o = new BuildingObj { Go = new GameObject(d.Name + " " + b.Id) };
                    o.Go.transform.SetParent(transform, false);
                    o.Go.AddComponent<MeshFilter>();
                    o.Go.AddComponent<MeshRenderer>();
                    buildings[b.Id] = o;
                }
                if (o.Key == key && !rebuildAll) continue;
                o.Key = key;
                bool sideways = b.Facing == 1 || b.Facing == 3;
                float w = (sideways ? d.H : d.W) * Tile, dep = (sideways ? d.W : d.H) * Tile;
                if (o.Mesh != null) Destroy(o.Mesh);
                var model = b.Built ? BuildingArt.Build(b.Type, w, dep, (uint)(b.Id * 2654435761u), d.Floors, b.Closed && Town.IsBusiness(b), season)
                                    : BuildingArt.Site(w, dep, b.BuildFraction, BuildingArt.Height(b.Type, d.Floors), b.Started);
                o.Mesh = model.Mesh;
                o.Go.GetComponent<MeshFilter>().sharedMesh = model.Mesh;
                o.Go.GetComponent<MeshRenderer>().sharedMaterials = model.Materials;
                o.Go.transform.localPosition = FootprintCenter(b);
                o.Go.transform.localRotation = Quaternion.Euler(0, Yaw(b.Facing), 0);
            }
            if (seen.Count != buildings.Count)
            {
                var gone = new List<int>();
                foreach (var kv in buildings) if (!seen.Contains(kv.Key)) gone.Add(kv.Key);
                foreach (var id in gone) { Destroy(buildings[id].Go); if (buildings[id].Mesh != null) Destroy(buildings[id].Mesh); buildings.Remove(id); }
            }
        }

        // ================================================================== outlines and previews
        static void Outline(MeshKit k, float x0, float z0, float x1, float z1, float y, float t, Color32 c)
        {
            k.Flat(x0, z0, x1, z0 + t, y, c);
            k.Flat(x0, z1 - t, x1, z1, y, c);
            k.Flat(x0, z0 + t, x0 + t, z1 - t, y, c);
            k.Flat(x1 - t, z0 + t, x1, z1 - t, y, c);
        }

        string selKey, hoverKey, ghostKey;

        public void ShowSelection(Building b)
        {
            string key = b == null ? "" : b.Id + ":" + b.X + ":" + b.Y;
            if (key == selKey) return;
            selKey = key;
            highlightMesh.Clear();
            if (b == null) return;
            var k = new MeshKit(); var d = b.Def;
            Outline(k, b.X * Tile, b.Y * Tile, (b.X + d.W) * Tile, (b.Y + d.H) * Tile, 0.4f, 1.0f, new Color32(255, 220, 80, 230));
            k.ToOverlay(highlightMesh);
        }

        public void ShowHover(int x, int y, int w, int h, Color32 c)
        {
            string key = w <= 0 ? "" : x + ":" + y + ":" + w + ":" + h + ":" + c.r + ":" + c.g + ":" + c.a;
            if (key == hoverKey) return;
            hoverKey = key;
            hoverMesh.Clear();
            if (w <= 0) return;
            var k = new MeshKit();
            Outline(k, x * Tile, y * Tile, (x + w) * Tile, (y + h) * Tile, 0.35f, 0.6f, c);
            k.ToOverlay(hoverMesh);
        }

        /// <summary>Tinted tiles under the cursor: where a road, zone or building would go.</summary>
        public void ShowGhost(List<Vector2Int> tiles, Color32 c) { ShowGhost(tiles, c, null, c); }

        public void ShowGhost(List<Vector2Int> tiles, Color32 c, List<Vector2Int> bad, Color32 badColor)
        {
            int hsh = 17;
            unchecked
            {
                if (tiles != null) foreach (var t in tiles) hsh = hsh * 31 + t.x * 1009 + t.y;
                hsh = hsh * 7 + c.r * 65536 + c.g * 256 + c.b;
                if (bad != null) foreach (var t in bad) hsh = hsh * 37 + t.x * 1013 + t.y;
            }
            string key = hsh + ":" + (tiles != null ? tiles.Count : 0) + ":" + (bad != null ? bad.Count : 0);
            if (key == ghostKey) return;
            ghostKey = key;
            ghostMesh.Clear();
            if ((tiles == null || tiles.Count == 0) && (bad == null || bad.Count == 0)) return;
            var k = new MeshKit();
            if (tiles != null) foreach (var t in tiles) k.Flat(t.x * Tile + 0.3f, t.y * Tile + 0.3f, t.x * Tile + Tile - 0.3f, t.y * Tile + Tile - 0.3f, 0.3f, c);
            if (bad != null) foreach (var t in bad) k.Flat(t.x * Tile + 0.3f, t.y * Tile + 0.3f, t.x * Tile + Tile - 0.3f, t.y * Tile + Tile - 0.3f, 0.3f, badColor);
            k.ToOverlay(ghostMesh);
        }

        public void HideGhost() { ShowGhost(null, default(Color32)); ShowHover(0, 0, 0, 0, default(Color32)); }
    }
}
