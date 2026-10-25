using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// Draws the town: ground, water, woods, roads, zoned lots and every building, rebuilt when the map changes. Also the sun
    /// and the time of day, the selection outline and the placement preview.
    /// </summary>
    public sealed class WorldView : MonoBehaviour
    {
        public const float Tile = Nav.Tile;
        const int Chunk = 16;

        Town town;
        CameraRig rig;
        public Material Lit { get; private set; }
        public Material Windows { get; private set; }
        public Material Overlay { get; private set; }
        Light sun;

        readonly Dictionary<int, GameObject> chunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, GameObject> treeChunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, int> treeHash = new Dictionary<int, int>();
        readonly Dictionary<int, GameObject> zoneChunks = new Dictionary<int, GameObject>();
        readonly Dictionary<int, BuildingObj> buildings = new Dictionary<int, BuildingObj>();
        int mapVersion = -1, season = -1;
        bool zoneStrong, zoneStrongShown;
        GameObject ghost, highlight, hover;
        Mesh ghostMesh, highlightMesh, hoverMesh;

        sealed class BuildingObj { public GameObject Go; public string Key; public Mesh Mesh; }

        public Nav Nav { get; private set; }

        // ================================================================== setup
        void Awake()
        {
            Lit = MakeMat("RealLife/Lit", "Standard");
            Windows = MakeMat("RealLife/Window", "Standard");
            Overlay = MakeMat("RealLife/Overlay", "Sprites/Default");
            Windows.SetColor("_Color", new Color(0.26f, 0.33f, 0.42f));
            var sg = new GameObject("Sun");
            sun = sg.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.shadowBias = 0.05f;
            sun.shadowNormalBias = 0.4f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 700f;
            QualitySettings.shadowCascades = 2;
            QualitySettings.antiAliasing = 4;
            ghost = Obj("Ghost", Overlay, out ghostMesh);
            highlight = Obj("Selection", Overlay, out highlightMesh);
            hover = Obj("Hover", Overlay, out hoverMesh);
        }

        static Material MakeMat(string shader, string fallback)
        {
            var sh = Resources.Load<Shader>(shader);
            if (sh == null) sh = Shader.Find(shader);
            if (sh == null) sh = Shader.Find(fallback);
            return new Material(sh);
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
            Nav = new Nav(t);
            foreach (var c in chunks.Values) Destroy(c);
            foreach (var c in treeChunks.Values) Destroy(c);
            foreach (var c in zoneChunks.Values) Destroy(c);
            foreach (var b in buildings.Values) { Destroy(b.Go); if (b.Mesh != null) Destroy(b.Mesh); }
            chunks.Clear(); treeChunks.Clear(); treeHash.Clear(); zoneChunks.Clear(); buildings.Clear();
            mapVersion = -1; season = -1;
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
            }
            else if (Time.frameCount % 15 == 0) SyncBuildings(false);
            Lighting();
        }

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
                    SetChunk(chunks, key, "Ground " + key, k, Lit, true);
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
                    SetChunk(treeChunks, key, "Trees " + key, k, Lit, true);
                }
        }

        void SetChunk(Dictionary<int, GameObject> set, int key, string name, MeshKit k, Material mat, bool shadows)
        {
            GameObject go;
            if (!set.TryGetValue(key, out go))
            {
                go = new GameObject(name);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterials = mat == Lit ? new[] { Lit, Windows } : new[] { mat };
                r.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                set[key] = go;
            }
            var mf = go.GetComponent<MeshFilter>();
            if (mf.sharedMesh != null) Destroy(mf.sharedMesh);
            mf.sharedMesh = k.VertexCount > 0 ? k.ToMesh(name) : null;
        }

        static uint Hash(int x, int y) { uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return h; }

        Color32 GrassColor(int x, int y)
        {
            float n = (Hash(x, y) & 255) / 255f;
            switch (season)
            {
                case 0: return MeshKit.Mix(MeshKit.Hex("#e7ecef"), MeshKit.Hex("#f6f8fa"), n);
                case 1: return MeshKit.Mix(MeshKit.Hex("#86bd5a"), MeshKit.Hex("#93c463"), n);
                case 2: return MeshKit.Mix(MeshKit.Hex("#7aae4f"), MeshKit.Hex("#88b65a"), n);
                default: return MeshKit.Mix(MeshKit.Hex("#a2a956"), MeshKit.Hex("#b5ac5e"), n);
            }
        }

        void GroundTile(MeshKit k, int x, int y)
        {
            float x0 = x * Tile, z0 = y * Tile, x1 = x0 + Tile, z1 = z0 + Tile;
            int t = town.Terrain(x, y);
            if (t == Town.TileBits.Water)
            {
                k.Flat(x0, z0, x1, z1, -0.9f, season == 0 ? MeshKit.Hex("#b9d3e0") : MeshKit.Hex("#4f93c0"));
                // banks down to the water
                var bank = season == 0 ? MeshKit.Hex("#d9dfe3") : MeshKit.Hex("#c8b48a");
                if (!town.IsWater(x, y - 1) && y > 0) k.Quad(new Vector3(x1, -0.9f, z0), new Vector3(x1, 0, z0), new Vector3(x0, 0, z0), new Vector3(x0, -0.9f, z0), bank);
                if (!town.IsWater(x, y + 1) && y < town.Rows - 1) k.Quad(new Vector3(x0, -0.9f, z1), new Vector3(x0, 0, z1), new Vector3(x1, 0, z1), new Vector3(x1, -0.9f, z1), bank);
                if (!town.IsWater(x - 1, y) && x > 0) k.Quad(new Vector3(x0, -0.9f, z0), new Vector3(x0, 0, z0), new Vector3(x0, 0, z1), new Vector3(x0, -0.9f, z1), bank);
                if (!town.IsWater(x + 1, y) && x < town.Cols - 1) k.Quad(new Vector3(x1, -0.9f, z1), new Vector3(x1, 0, z1), new Vector3(x1, 0, z0), new Vector3(x1, -0.9f, z0), bank);
                return;
            }
            Color32 c = t == Town.TileBits.Sand ? (season == 0 ? MeshKit.Hex("#eef1f3") : MeshKit.Hex("#dccb9a")) : GrassColor(x, y);
            if (t == Town.TileBits.Trees) c = MeshKit.Shade(c, 0.9f);
            k.Flat(x0, z0, x1, z1, 0, c);
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

        static readonly Color32 Asphalt = MeshKit.Hex("#50545a"), Curb = MeshKit.Hex("#c9c6be"), Paint = MeshKit.Hex("#f4f1e4"), Yellow = MeshKit.Hex("#e8c547");

        void RoadTile(MeshKit k, int x, int y)
        {
            float x0 = x * Tile, z0 = y * Tile, x1 = x0 + Tile, z1 = z0 + Tile, cx = x0 + Tile / 2, cz = z0 + Tile / 2;
            const float sw = 1.8f, sh = 0.2f;
            bool n = RoadAt(x, y + 1), e = RoadAt(x + 1, y), s = RoadAt(x, y - 1), w = RoadAt(x - 1, y);
            int links = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);
            var asphalt = season == 0 ? MeshKit.Hex("#6a6e74") : Asphalt;
            k.Flat(x0, z0, x1, z1, 0.05f, asphalt);
            var curb = season == 0 ? MeshKit.Hex("#eef1f3") : Curb;
            // pavements on the sides with no road, and the four corners
            if (!n) k.Box(new Vector3(x0, 0, z1 - sw), new Vector3(x1, sh, z1), curb, curb);
            if (!s) k.Box(new Vector3(x0, 0, z0), new Vector3(x1, sh, z0 + sw), curb, curb);
            if (!e) k.Box(new Vector3(x1 - sw, 0, z0), new Vector3(x1, sh, z1), curb, curb);
            if (!w) k.Box(new Vector3(x0, 0, z0), new Vector3(x0 + sw, sh, z1), curb, curb);
            if (n && e) k.Box(new Vector3(x1 - sw, 0, z1 - sw), new Vector3(x1, sh, z1), curb, curb);
            if (n && w) k.Box(new Vector3(x0, 0, z1 - sw), new Vector3(x0 + sw, sh, z1), curb, curb);
            if (s && e) k.Box(new Vector3(x1 - sw, 0, z0), new Vector3(x1, sh, z0 + sw), curb, curb);
            if (s && w) k.Box(new Vector3(x0, 0, z0), new Vector3(x0 + sw, sh, z0 + sw), curb, curb);
            if (season == 0) return;
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
                    SetChunk(zoneChunks, cy * cw + cx, "Zones", k, Overlay, false);
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
                    var r = o.Go.AddComponent<MeshRenderer>();
                    r.sharedMaterials = new[] { Lit, Windows };
                    buildings[b.Id] = o;
                }
                if (o.Key == key && !rebuildAll) continue;
                o.Key = key;
                bool sideways = b.Facing == 1 || b.Facing == 3;
                float w = (sideways ? d.H : d.W) * Tile, dep = (sideways ? d.W : d.H) * Tile;
                if (o.Mesh != null) Destroy(o.Mesh);
                o.Mesh = b.Built ? BuildingArt.Build(b.Type, w, dep, (uint)(b.Id * 2654435761u), d.Floors, b.Closed && Town.IsBusiness(b), season)
                                 : BuildingArt.Site(w, dep, b.BuildFraction, BuildingArt.Height(b.Type, d.Floors), b.Started);
                o.Go.GetComponent<MeshFilter>().sharedMesh = o.Mesh;
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
            Fill(highlightMesh, k);
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
            Fill(hoverMesh, k);
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
            Fill(ghostMesh, k);
        }

        public void HideGhost() { ShowGhost(null, default(Color32)); ShowHover(0, 0, 0, 0, default(Color32)); }

        static void Fill(Mesh target, MeshKit k)
        {
            var m = k.ToMesh("tmp");
            target.Clear();
            target.vertices = m.vertices; target.normals = m.normals; target.colors32 = m.colors32;
            target.subMeshCount = 1;
            target.SetTriangles(m.GetTriangles(0), 0);
            target.RecalculateBounds();
            Destroy(m);
        }

        // ================================================================== sun, sky and night
        public float Night { get; private set; }

        void Lighting()
        {
            float hour = town.Hour;
            // the sun rises at 6 and sets at 20
            float day = Mathf.InverseLerp(6f, 20f, hour);
            float elev = Mathf.Sin(Mathf.Clamp01(day) * Mathf.PI);
            bool up = hour > 6f && hour < 20f;
            float dusk = Mathf.Clamp01(Mathf.Min(Mathf.Abs(hour - 6.5f), Mathf.Abs(hour - 19.5f)) / 1.6f);
            Night = up ? Mathf.Clamp01(1 - elev * 4f) : 1f;
            float azimuth = Mathf.Lerp(-80, 260, day);
            sun.transform.rotation = up ? Quaternion.Euler(Mathf.Lerp(8, 62, elev), azimuth, 0) : Quaternion.Euler(50, 150, 0);
            var warm = new Color(1f, 0.72f, 0.48f); var noon = new Color(1f, 0.97f, 0.9f); var moon = new Color(0.55f, 0.62f, 0.85f);
            sun.color = up ? Color.Lerp(warm, noon, Mathf.Clamp01(elev * 2.2f)) : moon;
            sun.intensity = up ? Mathf.Lerp(0.35f, 1.15f, Mathf.Clamp01(elev * 1.8f)) : 0.22f;
            Color skyDay = new Color(0.55f, 0.75f, 0.95f), skyDusk = new Color(0.95f, 0.6f, 0.42f), skyNight = new Color(0.05f, 0.07f, 0.14f);
            if (season == 0) skyDay = new Color(0.72f, 0.8f, 0.88f);
            Color sky = up ? Color.Lerp(skyDusk, skyDay, dusk) : skyNight;
            sky = Color.Lerp(sky, skyNight, Night * 0.85f);
            RenderSettings.ambientSkyColor = Color.Lerp(sky, Color.white, 0.25f) * (up ? 1f : 0.5f);
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, new Color(0.6f, 0.6f, 0.6f), 0.5f) * (up ? 0.85f : 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.33f, 0.3f) * (up ? 0.8f : 0.3f);
            RenderSettings.fogColor = sky;
            if (rig != null)
            {
                rig.Cam.backgroundColor = sky;
                RenderSettings.fogStartDistance = rig.Distance * 1.6f;
                RenderSettings.fogEndDistance = rig.Distance * 5f + 400;
            }
            Windows.SetFloat("_Glow", Mathf.SmoothStep(0, 1, Night));
        }
    }
}
