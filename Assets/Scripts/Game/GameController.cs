using System;
using System.Collections.Generic;
using System.IO;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    public enum Tool { Inspect, Road, Zone, Dezone, Place, Bulldoze }

    /// <summary>
    /// Owns the Town, runs the clock, turns mouse and keys into tools, and saves and loads.
    /// Boots itself when you press Play, so the scene only needs to exist.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }
        public Town Town { get; private set; }
        public CameraRig Rig { get; private set; }
        public WorldView View { get; private set; }
        public Hud Hud { get; private set; }
        public Agents Agents { get; private set; }

        public static readonly int[] Speeds = { 0, 1, 3, 10, 30, 90 };
        public const float SecondsPerDay = 360f;     // six minutes a day (about a week) at normal speed
        public int Speed { get; private set; }
        int lastSpeed = 1;

        public Tool Tool { get; private set; }
        public Zone ZoneKind { get; private set; }
        public BuildingType PlaceType { get; private set; }
        public int SelectedBuilding = -1, SelectedCitizen = -1;
        public bool Following;
        public Vector2Int Hover { get; private set; }
        public bool HoverValid { get; private set; }
        public string ToolTip { get; private set; }
        bool dragging; Vector2Int dragStart;

        string SavePath { get { return Path.Combine(Application.persistentDataPath, "reallife-save-v1.json"); } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            new GameObject("Real Life").AddComponent<GameController>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 60;
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            Rig = cam.GetComponent<CameraRig>();
            if (Rig == null) Rig = cam.gameObject.AddComponent<CameraRig>();
            View = new GameObject("World").AddComponent<WorldView>();
            Hud = gameObject.AddComponent<Hud>();
            var t = Load() ?? Town.CreateNew(Environment.TickCount & 0x7fffffff);
            Bind(t);
            SetSpeed(1);
        }

        void Bind(Town t)
        {
            Town = t;
            Town.DayEnded = day => { if (day % Balance.DaysPerMonth == 0) Save(); };
            Town.Logged = e => { if (Hud != null) Hud.OnLog(e); };
            View.Bind(t, Rig);
            Agents = View.GetComponent<Agents>();
            Rig.Bounds = new Vector2(t.Cols * WorldView.Tile, t.Rows * WorldView.Tile);
            SelectedBuilding = SelectedCitizen = -1; Following = false;
            float cx, cy; t.TownCentre(out cx, out cy);
            Rig.Snap(new Vector3((cx + 0.5f) * WorldView.Tile, 0, (cy + 0.5f) * WorldView.Tile));
            SetTool(Tool.Inspect);
        }

        public void NewTown()
        {
            // the old town is kept beside the save, named after it, in case you want it back
            try
            {
                if (File.Exists(SavePath) && Town != null)
                {
                    string safe = string.Concat(Town.Name.Split(Path.GetInvalidFileNameChars()));
                    File.Copy(SavePath, Path.Combine(Application.persistentDataPath, "reallife-old-" + safe + ".json"), true);
                }
            }
            catch (Exception e) { Debug.LogWarning("Couldn't keep a copy of the old town: " + e.Message); }
            Bind(Town.CreateNew(Environment.TickCount & 0x7fffffff));
            Save();
            SetSpeed(1);
        }

#if UNITY_EDITOR
        // Save before Unity reloads changed scripts, so pressing Play again carries on.
        void OnEnable() { UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += SaveNow; }
        void OnDisable() { UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= SaveNow; }
        void SaveNow() { if (Town != null) Save(); }
#endif
        void OnApplicationQuit() { if (Town != null) Save(); }

        // ================================================================== saving
        public void Save()
        {
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(Town)); }
            catch (Exception e) { Debug.LogWarning("Couldn't save: " + e.Message); }
        }

        Town Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return null;
                var t = JsonUtility.FromJson<Town>(File.ReadAllText(SavePath));
                if (t == null || t.SaveVersion < 1 || t.SaveVersion > Town.CurrentVersion || t.Tiles == null || t.Tiles.Length != t.Cols * t.Rows) return null;
                t.Index();
                t.Migrate();
                return t;
            }
            catch (Exception e) { Debug.LogWarning("Couldn't load the save, starting a new town: " + e.Message); return null; }
        }

        // ================================================================== controls
        public void SetSpeed(int i) { i = Mathf.Clamp(i, 0, Speeds.Length - 1); if (i > 0) lastSpeed = i; Speed = i; }
        public void TogglePause() { SetSpeed(Speed > 0 ? 0 : lastSpeed); }

        public void SetTool(Tool t, Zone z = Zone.None, BuildingType p = BuildingType.Park)
        {
            Tool = t; ZoneKind = z; PlaceType = p; dragging = false;
            View.HideGhost(); View.ShowGhost(null, default(Color32));
            View.SetZoneEmphasis(t == Tool.Zone || t == Tool.Dezone);
        }

        public void SelectBuilding(int id) { SelectedBuilding = id; SelectedCitizen = -1; Following = false; Rig.StopFollowing(); }
        public void SelectCitizen(int id) { SelectedCitizen = id; SelectedBuilding = -1; Following = false; }
        public void ClearSelection() { SelectedBuilding = SelectedCitizen = -1; Following = false; Rig.StopFollowing(); }

        public void FocusBuilding(Building b) { if (b == null) return; SelectBuilding(b.Id); Rig.CenterOn(WorldView.FootprintCenter(b)); }
        public void FocusCitizen(Citizen c)
        {
            if (c == null) return;
            SelectCitizen(c.Id);
            Vector3 p;
            if (Agents != null && Agents.Where(c.Id, out p)) { Rig.CenterOn(p); return; }
            var place = Town.GetBuilding(c.AtWork ? c.WorkId : -1) ?? Town.HomeOf(c);
            if (place != null) Rig.CenterOn(WorldView.FootprintCenter(place));
        }

        void Update()
        {
            if (Town == null) return;
            bool overUi = Hud.PointerOver;
            Rig.Control(!overUi, !Hud.Typing);
            Keys();
            if (Speed > 0)
            {
                float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                Town.Advance(dt * Speeds[Speed] / SecondsPerDay);
            }
            // the tile under the mouse
            Vector3 hit = Vector3.zero;
            HoverValid = !overUi && Rig.GroundPoint(Input.mousePosition, out hit);
            if (HoverValid)
            {
                Hover = new Vector2Int(Mathf.FloorToInt(hit.x / WorldView.Tile), Mathf.FloorToInt(hit.z / WorldView.Tile));
                HoverValid = Town.InBounds(Hover.x, Hover.y);
            }
            ToolTip = null;
            UseTool(overUi);
            // keep the outline on what's selected, and the camera on whoever's followed
            var sel = Town.GetBuilding(SelectedBuilding);
            View.ShowSelection(sel);
            if (SelectedCitizen >= 0 && Town.GetCitizen(SelectedCitizen) == null) SelectedCitizen = -1;
            if (Following && SelectedCitizen >= 0)
            {
                Vector3 p;
                if (Agents.Where(SelectedCitizen, out p)) Rig.Follow(p);
            }
        }

        void Keys()
        {
            if (Hud.Typing) return;
            if (Input.GetKeyDown(KeyCode.Space)) TogglePause();
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetSpeed(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetSpeed(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetSpeed(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetSpeed(4);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetSpeed(5);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Tool != Tool.Inspect) SetTool(Tool.Inspect);
                else if (SelectedBuilding >= 0 || SelectedCitizen >= 0) ClearSelection();
                else Hud.CloseAll();
            }
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.X)) SetTool(Tool.Bulldoze);
            if (Input.GetKeyDown(KeyCode.T)) SetTool(Tool.Road);
            if (Input.GetKeyDown(KeyCode.F5)) { Save(); Hud.Toast("Saved."); }
        }

        // ================================================================== tools
        void UseTool(bool overUi)
        {
            bool down = Input.GetMouseButtonDown(0) && !overUi, held = Input.GetMouseButton(0), up = Input.GetMouseButtonUp(0);
            if (!HoverValid && !dragging) { View.HideGhost(); View.ShowGhost(null, default(Color32)); return; }
            switch (Tool)
            {
                case Tool.Inspect: Inspect(down); break;
                case Tool.Road: RoadTool(down, held, up); break;
                case Tool.Zone:
                case Tool.Dezone: ZoneTool(down, held, up); break;
                case Tool.Place: PlaceTool(down); break;
                case Tool.Bulldoze: BulldozeTool(down, held, up); break;
            }
        }

        void Inspect(bool click)
        {
            var b = Town.BuildingAt(Hover.x, Hover.y);
            if (b != null) { var d = b.Def; View.ShowHover(b.X, b.Y, d.W, d.H, new Color32(255, 255, 255, 140)); ToolTip = Hud.Describe(b); }
            else View.ShowHover(0, 0, 0, 0, default(Color32));
            if (!click) return;
            int who = Agents != null ? Agents.Pick(Rig.Cam, Input.mousePosition) : -1;
            if (who >= 0) { SelectCitizen(who); return; }
            if (b != null) SelectBuilding(b.Id);
            else ClearSelection();
        }

        List<Vector2Int> DragLine(Vector2Int a, Vector2Int b)
        {
            var l = new List<Vector2Int>();
            int dx = Math.Sign(b.x - a.x), dy = Math.Sign(b.y - a.y);
            // the longer leg first
            bool xFirst = Mathf.Abs(b.x - a.x) >= Mathf.Abs(b.y - a.y);
            int x = a.x, y = a.y;
            l.Add(new Vector2Int(x, y));
            if (xFirst) { while (x != b.x) { x += dx; l.Add(new Vector2Int(x, y)); } while (y != b.y) { y += dy; l.Add(new Vector2Int(x, y)); } }
            else { while (y != b.y) { y += dy; l.Add(new Vector2Int(x, y)); } while (x != b.x) { x += dx; l.Add(new Vector2Int(x, y)); } }
            return l;
        }

        List<Vector2Int> DragRect(Vector2Int a, Vector2Int b)
        {
            var l = new List<Vector2Int>();
            for (int y = Mathf.Min(a.y, b.y); y <= Mathf.Max(a.y, b.y); y++)
                for (int x = Mathf.Min(a.x, b.x); x <= Mathf.Max(a.x, b.x); x++)
                    if (Town.InBounds(x, y)) l.Add(new Vector2Int(x, y));
            return l;
        }

        void RoadTool(bool down, bool held, bool up)
        {
            if (down) { dragging = true; dragStart = Hover; }
            var line = dragging ? DragLine(dragStart, Hover) : new List<Vector2Int> { Hover };
            int ok = 0; string why = null;
            var good = new List<Vector2Int>(); var bad = new List<Vector2Int>();
            foreach (var t in line)
            {
                if (Town.IsRoad(t.x, t.y)) continue;
                var r = Town.CanRoad(t.x, t.y);
                if (r == null) { ok++; good.Add(t); } else { bad.Add(t); why = r; }
            }
            View.ShowGhost(good, new Color32(90, 200, 255, 150), bad, new Color32(255, 80, 80, 150));
            ToolTip = "Road: " + ok + " tiles, " + Town.Money(ok * Town.RoadCost) + (why != null ? "\n" + why : "") + "\nDrag to lay a street. Esc to stop.";
            if (up && dragging)
            {
                dragging = false;
                int built = 0;
                foreach (var t in line) if (Town.PlaceRoad(t.x, t.y)) built++;
                if (built > 0) Hud.Toast("Built " + built + " road tiles for " + Town.Money(built * Town.RoadCost) + ".");
            }
        }

        void ZoneTool(bool down, bool held, bool up)
        {
            if (down) { dragging = true; dragStart = Hover; }
            var area = dragging ? DragRect(dragStart, Hover) : new List<Vector2Int> { Hover };
            var ok = new List<Vector2Int>();
            foreach (var t in area)
                if (!Town.IsWater(t.x, t.y) && !Town.IsRoad(t.x, t.y) && Town.BuildingAt(t.x, t.y) == null) ok.Add(t);
            Color32 c = Tool == Tool.Dezone ? new Color32(200, 200, 200, 140) : (Color32)WorldView.ZoneColor(ZoneKind);
            c.a = 150;
            View.ShowGhost(ok, c);
            string what = Tool == Tool.Dezone ? "Clear zoning" : ZoneName(ZoneKind) + " zone";
            ToolTip = what + ": " + ok.Count + " tiles. Free.\n" + (Tool == Tool.Dezone ? "" : ZoneHint(ZoneKind));
            if (up && dragging)
            {
                dragging = false;
                int n = 0;
                foreach (var t in ok) if (Town.SetZone(t.x, t.y, Tool == Tool.Dezone ? Zone.None : ZoneKind)) n++;
            }
        }

        public static string ZoneName(Zone z)
        {
            switch (z)
            {
                case Zone.Residential: return "Houses";
                case Zone.Apartments: return "Apartments";
                case Zone.Commercial: return "Shops";
                case Zone.Industrial: return "Industry";
                case Zone.Farmland: return "Farms";
                default: return "";
            }
        }

        public static string ZoneHint(Zone z)
        {
            switch (z)
            {
                case Zone.Residential: return "Property firms build houses here for families who can get a mortgage. Lots must touch a road.";
                case Zone.Apartments: return "Property firms build blocks of twelve flats here, to let to singles, young people leaving home and workers who come for a job. Each block needs 2 x 2 tiles by a road.";
                case Zone.Commercial: return "Shops, restaurants, offices and banks open here when there are customers for them.";
                case Zone.Industrial: return "Factories, builders and hauliers set up here when there are workers to hire.";
                case Zone.Farmland: return "Farms start here when the food plant is short of crops or people need work. Keep it away from the factories; each farm needs 3 x 3 tiles by a road.";
                default: return "";
            }
        }

        void PlaceTool(bool click)
        {
            var d = Defs.Get(PlaceType);
            int x = Hover.x - (d.W - 1) / 2, y = Hover.y - (d.H - 1) / 2;
            string why = Town.CanPlace(PlaceType, x, y);
            var tiles = new List<Vector2Int>();
            for (int ty = y; ty < y + d.H; ty++) for (int tx = x; tx < x + d.W; tx++) if (Town.InBounds(tx, ty)) tiles.Add(new Vector2Int(tx, ty));
            View.ShowGhost(tiles, why == null ? new Color32(80, 220, 120, 150) : new Color32(255, 80, 80, 150));
            ToolTip = d.Name + ": " + (PlaceType == BuildingType.Bank ? "paid for by " + Town.BankName : Town.Money(d.StartupCost)) + "\n" + d.Blurb + (why != null ? "\n" + Ui_Red(why) : "\nClick to build.");
            if (click && why == null)
            {
                var b = Town.PlacePublic(PlaceType, x, y);
                if (b != null) { Hud.Toast("Work started on the " + d.Name.ToLower() + "."); if (!Input.GetKey(KeyCode.LeftShift)) SetTool(Tool.Inspect); }
            }
        }

        static string Ui_Red(string s) { return "<color=#ff6b6b>" + s + "</color>"; }

        void BulldozeTool(bool down, bool held, bool up)
        {
            var b = Town.BuildingAt(Hover.x, Hover.y);
            if (b != null)
            {
                var d = b.Def;
                string why = Town.CanBulldoze(Hover.x, Hover.y);
                View.ShowGhost(null, default(Color32));
                View.ShowHover(b.X, b.Y, d.W, d.H, why == null ? new Color32(255, 80, 80, 220) : new Color32(150, 150, 150, 200));
                ToolTip = "Demolish the " + d.Name.ToLower() + (why != null ? "\n" + Ui_Red(why) : b.Owner == Owner.Town ? "" : "\nThe town buys it out first.");
                if (down && why == null) { Town.Bulldoze(Hover.x, Hover.y); if (SelectedBuilding == b.Id) ClearSelection(); }
                return;
            }
            View.ShowHover(0, 0, 0, 0, default(Color32));
            if (down) { dragging = true; dragStart = Hover; }
            var area = dragging ? DragRect(dragStart, Hover) : new List<Vector2Int> { Hover };
            var hitTiles = area.FindAll(t => Town.IsRoad(t.x, t.y) || (Town.ZoneAt(t.x, t.y) != Zone.None && Town.BuildingAt(t.x, t.y) == null));
            View.ShowGhost(hitTiles, new Color32(255, 80, 80, 150));
            ToolTip = "Bulldoze roads and zoning (drag), or a building (click).";
            if (up && dragging)
            {
                dragging = false;
                foreach (var t in hitTiles) Town.Bulldoze(t.x, t.y);
            }
        }
    }
}
