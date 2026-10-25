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

        Material Grass(bool woods, int variant = 0)
        {
            if (season == 0) return S("snow");
            var tint = BuildingArt.GrassTint(season);
            var v = woods ? new Color(0.9f, 0.92f, 0.9f) : MeadowTints[variant % MeadowTints.Length];
            return MaterialLibrary.Tinted("grass", new Color(tint.r * v.r, tint.g * v.g, tint.b * v.b));
        }

        /// <summary>A gentle patchiness for the lawns and verges: smooth noise over a few tiles, in four steps.</summary>
        static int GrassVariant(int x, int y)
        {
            float fx = x / 5f, fy = y / 5f;
            int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
            float tx = fx - ix, ty = fy - iy;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            float a = (Hash(ix, iy) & 255) / 255f, b = (Hash(ix + 1, iy) & 255) / 255f, c = (Hash(ix, iy + 1) & 255) / 255f, d = (Hash(ix + 1, iy + 1) & 255) / 255f;
            float n = Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
            return Mathf.Clamp(Mathf.FloorToInt(n * 4), 0, 3);
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
            var m = t == Town.TileBits.Sand ? Bank : Grass(t == Town.TileBits.Trees, GrassVariant(x, y));
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
        const float NearRing = 640f;

        /// <summary>A patchwork of fields, meadows and woods running off to the horizon on every side; rivers and the
        /// highway carried on out of the map.</summary>
        void RebuildCountry()
        {
            countrySeason = season;
            var k = new MeshKit();
            float W = town.Cols * Tile, Hh = town.Rows * Tile, L = Country, N = NearRing;
            // rivers and the highway first: a strip straight out from each edge tile that has one
            var south = new bool[town.Cols]; var north = new bool[town.Cols]; var west = new bool[town.Rows]; var east = new bool[town.Rows];
            for (int x = 0; x < town.Cols; x++)
            {
                south[x] = EdgeStrip(k, x, 0, new Vector3(x * Tile, 0, 0), new Vector3((x + 1) * Tile, 0, 0), Vector3.back, WaterIn(x - 1, 0), WaterIn(x + 1, 0));
                north[x] = EdgeStrip(k, x, town.Rows - 1, new Vector3((x + 1) * Tile, 0, Hh), new Vector3(x * Tile, 0, Hh), Vector3.forward, WaterIn(x + 1, town.Rows - 1), WaterIn(x - 1, town.Rows - 1));
            }
            for (int y = 0; y < town.Rows; y++)
            {
                west[y] = EdgeStrip(k, 0, y, new Vector3(0, 0, (y + 1) * Tile), new Vector3(0, 0, y * Tile), Vector3.left, WaterIn(0, y + 1), WaterIn(0, y - 1));
                east[y] = EdgeStrip(k, town.Cols - 1, y, new Vector3(W, 0, y * Tile), new Vector3(W, 0, (y + 1) * Tile), Vector3.right, WaterIn(town.Cols - 1, y - 1), WaterIn(town.Cols - 1, y + 1));
            }
            // then the land round them: small parcels near the town, big ones further out
            Parcels(k, 0, W, -N, 0, 40, south, null); Parcels(k, 0, W, Hh, Hh + N, 40, north, null);
            Parcels(k, -N, 0, 0, Hh, 40, null, west); Parcels(k, W, W + N, 0, Hh, 40, null, east);
            Parcels(k, -N, 0, -N, 0, 40, null, null); Parcels(k, W, W + N, -N, 0, 40, null, null);
            Parcels(k, -N, 0, Hh, Hh + N, 40, null, null); Parcels(k, W, W + N, Hh, Hh + N, 40, null, null);
            Parcels(k, -N, W + N, -L, -N, 160, south, null); Parcels(k, -N, W + N, Hh + N, Hh + L, 160, north, null);
            Parcels(k, -L, -N, -L, Hh + L, 160, null, west); Parcels(k, W + N, W + L, -L, Hh + L, 160, null, east);
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

        /// <summary>Fills a rectangle of country with parcels on a size-P grid, leaving out the strips straight out from
        /// edge tiles that carry a river or the highway (byColumn: along x; byRow: along z).</summary>
        void Parcels(MeshKit k, float x0, float x1, float z0, float z1, float P, bool[] byColumn, bool[] byRow)
        {
            for (float gx = Mathf.Floor(x0 / P) * P; gx < x1 - 0.01f; gx += P)
                for (float gz = Mathf.Floor(z0 / P) * P; gz < z1 - 0.01f; gz += P)
                {
                    float cx0 = Mathf.Max(gx, x0), cx1 = Mathf.Min(gx + P, x1), cz0 = Mathf.Max(gz, z0), cz1 = Mathf.Min(gz + P, z1);
                    if (cx1 - cx0 < 0.01f || cz1 - cz0 < 0.01f) continue;
                    uint h = Hash(Mathf.RoundToInt(gx / P) * 3 + (P > 50 ? 7919 : 0), Mathf.RoundToInt(gz / P) * 5 + 1);
                    if (byColumn != null) Runs(cx0, cx1, byColumn, (a, b) => Parcel(k, a, b, cz0, cz1, h, P));
                    else if (byRow != null) Runs(cz0, cz1, byRow, (a, b) => Parcel(k, cx0, cx1, a, b, h, P));
                    else Parcel(k, cx0, cx1, cz0, cz1, h, P);
                }
        }

        /// <summary>Splits [a0, a1] into the runs that miss the special tiles.</summary>
        static void Runs(float a0, float a1, bool[] special, System.Action<float, float> emit)
        {
            float start = a0;
            int t0 = Mathf.FloorToInt(a0 / Tile), t1 = Mathf.CeilToInt(a1 / Tile);
            for (int t = t0; t < t1; t++)
            {
                if (t < 0 || t >= special.Length || !special[t]) continue;
                float s0 = Mathf.Max(a0, t * Tile), s1 = Mathf.Min(a1, (t + 1) * Tile);
                if (s0 > start + 0.01f) emit(start, s0);
                start = Mathf.Max(start, s1);
            }
            if (a1 > start + 0.01f) emit(start, a1);
        }

        static readonly Color[] MeadowTints = { new Color(1f, 1f, 1f), new Color(0.93f, 0.96f, 0.93f), new Color(1.05f, 1.0f, 0.88f), new Color(0.97f, 0.93f, 0.95f) };

        /// <summary>One parcel: a meadow, a crop field or (near the town) a wood, maybe with a line of trees along an edge.</summary>
        void Parcel(MeshKit k, float x0, float x1, float z0, float z1, uint h, float P)
        {
            float W = town.Cols * Tile, Hh = town.Rows * Tile;
            float cx = (x0 + x1) / 2, cz = (z0 + z1) / 2;
            float dist = Mathf.Max(Mathf.Max(-cx, cx - W), Mathf.Max(-cz, cz - Hh));
            float r = (h & 1023) / 1023f;
            bool near = dist < 320f;
            if (near && r < 0.16f)
            {
                // a wood
                k.Flat(x0, z0, x1, z1, 0, Grass(true), 8f);
                float area = (x1 - x0) * (z1 - z0);
                int n = Mathf.Min(14, Mathf.RoundToInt(area / 150f));
                uint t = h;
                for (int i = 0; i < n; i++)
                {
                    t = t * 1664525u + 1013904223u;
                    float px = Mathf.Lerp(x0 + 2, x1 - 2, ((t >> 8) & 255) / 255f), pz = Mathf.Lerp(z0 + 2, z1 - 2, ((t >> 16) & 255) / 255f);
                    float sc = 0.9f + ((t >> 4) & 15) / 25f;
                    if (dist > 160 || (t >> 28) % 3 == 0) PropArt.Pine(k, new Vector3(px, 0, pz), sc * 1.25f, season);
                    else PropArt.Tree(k, new Vector3(px, 0, pz), sc, season, t);
                }
                return;
            }
            Material m;
            if (season == 0) m = S("snow");
            else if (r < 0.5f)
            {
                string crop = season == 1 ? "field_young" : season == 2 ? (r < 0.3f ? "field_green" : "field_young") : (r < 0.35f ? "field_ripe" : "field_green");
                m = S(crop);
            }
            else
            {
                var tint = BuildingArt.GrassTint(season);
                var v = MeadowTints[(h >> 12) % (uint)MeadowTints.Length];
                m = MaterialLibrary.Tinted("grass", new Color(tint.r * v.r, tint.g * v.g, tint.b * v.b));
            }
            k.Flat(x0, z0, x1, z1, 0, m, 8f);
            // a hedgerow of trees down one side now and then
            if (near && ((h >> 20) & 7) < 2 && x1 - x0 > 20)
            {
                uint t = h ^ 0x9e3779b9u;
                for (float z = z0 + 3; z < z1 - 2; z += 7f)
                {
                    t = t * 1664525u + 1013904223u;
                    if (((t >> 8) & 3) == 0) continue;
                    PropArt.Tree(k, new Vector3(x0 + 1.2f, 0, z + ((t >> 12) & 3)), 0.85f + ((t >> 16) & 7) / 20f, season, t);
                }
            }
        }

        /// <summary>A river or the highway carried straight out from an edge tile (from a to b along the edge, out along
        /// `outward`). Returns false, drawing nothing, for an ordinary land tile.</summary>
        bool EdgeStrip(MeshKit k, int tx, int ty, Vector3 a, Vector3 b, Vector3 outward, bool waterLeft, bool waterRight)
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
                return true;
            }
            if (!road) return false;
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
            return true;
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
            byte a = (byte)(zoneStrong ? 95 : 28);
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
                            Color32 edge = ZoneColor(z); edge.a = (byte)(a + 90);
                            k.Flat(x * Tile + 0.6f, y * Tile + 0.6f, x * Tile + Tile - 0.6f, y * Tile + 0.85f, 0.13f, edge);
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
