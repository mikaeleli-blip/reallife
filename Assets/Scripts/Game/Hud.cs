using System.Collections.Generic;
using RealLife.Game.UI;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    public enum Panel { None, Town, Economy, People, News }

    /// <summary>
    /// The HUD: the top bar (date, speed, headline numbers), the toolbar, the building menu, the inspector on the right,
    /// the big panels on the left (laws and budget, economy, people, news), toasts and tooltips.
    /// </summary>
    public sealed partial class Hud : MonoBehaviour
    {
        GameController gc;
        Town T { get { return gc.Town; } }
        public Panel Open { get; private set; }
        bool buildMenu;
        readonly List<Rect> rects = new List<Rect>();
        readonly List<Rect> lastRects = new List<Rect>();
        public bool Typing { get { return false; } }
        string hoverTip;

        struct ToastMsg { public string Text; public float Until; public Color Col; }
        readonly List<ToastMsg> toasts = new List<ToastMsg>();

        void Awake() { gc = GetComponent<GameController>(); }

        /// <summary>Is the mouse over any part of the HUD?</summary>
        public bool PointerOver
        {
            get
            {
                if (Ui.Dragging) return true;
                var m = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                foreach (var r in lastRects) if (r.Contains(m)) return true;
                return false;
            }
        }

        public void CloseAll() { Open = Panel.None; buildMenu = false; }
        public void Toggle(Panel p) { Open = Open == p ? Panel.None : p; buildMenu = false; }

        public void Toast(string s) { Toast(s, Ui.Text); }
        public void Toast(string s, Color c)
        {
            toasts.Add(new ToastMsg { Text = s, Until = Time.unscaledTime + 4.5f, Col = c });
            if (toasts.Count > 4) toasts.RemoveAt(0);
        }

        /// <summary>Important news pops up briefly at the top of the screen.</summary>
        public void OnLog(LogEntry e)
        {
            if (e.Kind == LogKind.Bad && (e.Text.Contains("closed") || e.Text.Contains("left town") || e.Text.Contains("foreclosed"))) Toast(e.Text, Ui.Bad);
            else if (e.Kind == LogKind.Good && (e.Text.Contains("opened") || e.Text.Contains("is open"))) Toast(e.Text, Ui.Good);
        }

        Rect Claim(Rect r) { rects.Add(r); return r; }

        void OnGUI()
        {
            if (gc == null || gc.Town == null) return;
            Ui.Begin();
            if (Event.current.type == EventType.Layout) { lastRects.Clear(); lastRects.AddRange(rects); rects.Clear(); }
            hoverTip = null;
            TopBar();
            Demand();
            Toolbar();
            if (buildMenu) BuildMenu();
            switch (Open)
            {
                case Panel.Town: TownPanel(); break;
                case Panel.Economy: EconomyPanel(); break;
                case Panel.People: PeoplePanel(); break;
                case Panel.News: NewsPanel(); break;
            }
            Inspector();
            Toasts();
            var mouse = Event.current.mousePosition;
            if (hoverTip != null) Ui.Tooltip(mouse, hoverTip);
            else if (gc.ToolTip != null && !PointerOver) Ui.Tooltip(mouse, gc.ToolTip);
        }

        void Tip(Rect r, string s) { if (r.Contains(Event.current.mousePosition)) hoverTip = s; }

        // ================================================================== top bar
        static readonly string[] SpeedNames = { "II", "1x", "3x", "10x", "30x", "90x" };

        void TopBar()
        {
            float W = Screen.width / Ui.S;
            var bar = Claim(Ui.R(0, 0, W, 50));
            Ui.Fill(bar, Ui.Panel);
            Ui.Fill(new Rect(0, bar.yMax - Ui.S, bar.width, Ui.S), Ui.Border);
            Ui.Title(Ui.R(16, 4, 260, 26), T.Name);
            int hh = (int)T.Hour, mm = (int)((T.Hour - hh) * 60) / 15 * 15;
            Ui.Small(Ui.R(16, 28, 260, 18), T.Date + "  ·  " + hh.ToString("00") + ":" + mm.ToString("00") + "  ·  " + SeasonName(T.Season));
            // speed
            float x = 290;
            for (int i = 0; i < SpeedNames.Length; i++)
            {
                if (Ui.Button(Ui.R(x, 9, 44, 32), SpeedNames[i], gc.Speed == i)) gc.SetSpeed(i);
                Tip(Ui.R(x, 9, 44, 32), i == 0 ? "Pause (Space)" : "Speed " + SpeedNames[i] + " (key " + i + ")");
                x += 47;
            }
            // headline numbers, right-aligned
            var pop = T.Population;
            double delta = T.LastBook.TotalIn - T.LastBook.TotalOut;
            float un = T.UnemploymentRate();
            float hap = T.AverageHappiness();
            var chips = new[]
            {
                new Chip("People", pop.ToString("#,0"), T.LastBirths + T.LastArrivals - T.LastDeaths - T.LastLeft >= 0 ? "+" + (T.LastBirths + T.LastArrivals - T.LastDeaths - T.LastLeft) : (T.LastBirths + T.LastArrivals - T.LastDeaths - T.LastLeft).ToString(), Ui.Text, Panel.People,
                         "Population. Last month: " + T.LastBirths + " born, " + T.LastArrivals + " moved in, " + T.LastDeaths + " died, " + T.LastLeft + " left."),
                new Chip("Treasury", Town.Money(T.Treasury), (delta >= 0 ? "+" : "") + Town.Money(delta) + "/mo", T.Treasury < 0 ? Ui.Bad : Ui.Text, Panel.Town,
                         "The town's money. Last month it took in " + Town.Money(T.LastBook.TotalIn) + " and spent " + Town.Money(T.LastBook.TotalOut) + ". Click for the budget and laws."),
                new Chip("Jobless", Town.Pct(un), T.Unemployed() + " looking", un > 0.1f ? Ui.Bad : un > 0.06f ? Ui.Warn : Ui.Good, Panel.Economy,
                         "Share of the workforce without a job. Click for the economy."),
                new Chip("Happiness", Mathf.RoundToInt(hap) + "%", hap >= 60 ? "content" : hap >= 40 ? "uneasy" : "unhappy", hap >= 60 ? Ui.Good : hap >= 40 ? Ui.Warn : Ui.Bad, Panel.People,
                         "How happy grown-ups are on average: work, money, housing, food, health care, safety, parks, schools for their kids."),
            };
            float cx = W - 12;
            for (int i = chips.Length - 1; i >= 0; i--)
            {
                var c = chips[i];
                cx -= 150;
                var r = Ui.R(cx, 5, 146, 40);
                bool over = r.Contains(Event.current.mousePosition);
                if (over) Ui.Fill(r, Ui.Hover);
                Ui.Small(Ui.R(cx + 8, 5, 140, 16), c.Name);
                Ui.Bold(Ui.R(cx + 8, 21, 140, 22), c.Value, c.Color);
                Ui.Right(Ui.R(cx, 5, 138, 16), "<size=" + Mathf.RoundToInt(12 * Ui.S) + ">" + c.Sub + "</size>", Ui.Dim);
                Tip(r, c.Tip);
                if (over && Event.current.type == EventType.MouseDown && Event.current.button == 0) { Toggle(c.Panel); Event.current.Use(); }
            }
        }

        struct Chip
        {
            public string Name, Value, Sub, Tip; public Color Color; public Panel Panel;
            public Chip(string n, string v, string s, Color c, Panel p, string tip) { Name = n; Value = v; Sub = s; Color = c; Panel = p; Tip = tip; }
        }

        static string SeasonName(int s) { return s == 0 ? "Winter" : s == 1 ? "Spring" : s == 2 ? "Summer" : "Autumn"; }

        /// <summary>The R/C/I meters: how much each kind of zone is wanted.</summary>
        void Demand()
        {
            var r = Claim(Ui.R(10, 58, 172, 70));
            Ui.PanelBox(r);
            Ui.Small(Ui.R(18, 60, 160, 18), "Demand");
            string[] n = { "Homes", "Shops", "Work", "Farms" };
            Zone[] z = { Zone.Residential, Zone.Commercial, Zone.Industrial, Zone.Farmland };
            for (int i = 0; i < 4; i++)
            {
                float v = T.ZoneDemand[(int)z[i]];
                float bx = 18 + i * 40;
                var track = Ui.R(bx, 80, 30, 30);
                Ui.Fill(track, Ui.Panel2);
                float h = Mathf.Abs(v) * 15;
                var mid = 95f;
                var col = WorldView.ZoneColor(z[i]);
                if (v >= 0) Ui.Fill(Ui.R(bx, mid - h, 30, h), col); else Ui.Fill(Ui.R(bx, mid, 30, h), Ui.Bad);
                Ui.Fill(Ui.R(bx, mid, 30, 1), Ui.Dim);
                Ui.Center(Ui.R(bx - 5, 109, 40, 16), "<size=" + Mathf.RoundToInt(11 * Ui.S) + ">" + n[i] + "</size>", Ui.Dim);
                Tip(track, z[i] == Zone.Residential ? "Demand for homes: people who'd move here, or move out of their parents', if there were somewhere to live."
                         : z[i] == Zone.Commercial ? "Demand for shops and services: sales nobody in town could make."
                         : z[i] == Zone.Farmland ? "Demand for farmland: crops the food plant has to bring in from outside."
                         : "Demand for workplaces: people looking for work that firms could hire.");
            }
        }

        // ================================================================== toolbar
        void Toolbar()
        {
            float W = Screen.width / Ui.S, H = Screen.height / Ui.S;
            float bw = 84, bh = 54, gap = 4;
            int n = 13;
            float total = n * (bw + gap) + 14;
            float x0 = (W - total) / 2, y = H - bh - 12;
            var back = Claim(Ui.R(x0 - 8, y - 8, total + 16, bh + 16));
            Ui.PanelBox(back);
            float x = x0;
            ToolButton(ref x, y, bw, bh, "Select", gc.Tool == Tool.Inspect, () => gc.SetTool(Tool.Inspect), "Click buildings and people to see how they're doing. (Esc)");
            ToolButton(ref x, y, bw, bh, "Road", gc.Tool == Tool.Road, () => gc.SetTool(Tool.Road), "Lay streets: drag from one point to another. " + Town.Money(Town.RoadCost) + " a tile. (T)");
            ToolButton(ref x, y, bw, bh, "Homes", gc.Tool == Tool.Zone && gc.ZoneKind == Zone.Residential, () => gc.SetTool(Tool.Zone, Zone.Residential), GameController.ZoneHint(Zone.Residential));
            ToolButton(ref x, y, bw, bh, "Shops", gc.Tool == Tool.Zone && gc.ZoneKind == Zone.Commercial, () => gc.SetTool(Tool.Zone, Zone.Commercial), GameController.ZoneHint(Zone.Commercial));
            ToolButton(ref x, y, bw, bh, "Industry", gc.Tool == Tool.Zone && gc.ZoneKind == Zone.Industrial, () => gc.SetTool(Tool.Zone, Zone.Industrial), GameController.ZoneHint(Zone.Industrial));
            ToolButton(ref x, y, bw, bh, "Farms", gc.Tool == Tool.Zone && gc.ZoneKind == Zone.Farmland, () => gc.SetTool(Tool.Zone, Zone.Farmland), GameController.ZoneHint(Zone.Farmland));
            ToolButton(ref x, y, bw, bh, "Unzone", gc.Tool == Tool.Dezone, () => gc.SetTool(Tool.Dezone), "Clear zoning from empty land.");
            ToolButton(ref x, y, bw, bh, "Build", buildMenu || gc.Tool == Tool.Place, () => { buildMenu = !buildMenu; if (buildMenu) Open = Panel.None; }, "Public buildings the town pays for: schools, a clinic, police, parks, buses.");
            ToolButton(ref x, y, bw, bh, "Bulldoze", gc.Tool == Tool.Bulldoze, () => gc.SetTool(Tool.Bulldoze), "Remove roads, zoning, public buildings and empty private ones. (X)");
            x += 14;
            ToolButton(ref x, y, bw, bh, "Laws", Open == Panel.Town, () => Toggle(Panel.Town), "Taxes, wages, benefits, staffing, and the town budget.");
            ToolButton(ref x, y, bw, bh, "Economy", Open == Panel.Economy, () => Toggle(Panel.Economy), "Jobs, wages, prices, trade, businesses and history.");
            ToolButton(ref x, y, bw, bh, "People", Open == Panel.People, () => Toggle(Panel.People), "Who lives here: ages, education, housing, the unemployed.");
            ToolButton(ref x, y, bw, bh, "News", Open == Panel.News, () => Toggle(Panel.News), "What's been happening in town.");
        }

        void ToolButton(ref float x, float y, float w, float h, string label, bool on, System.Action act, string tip)
        {
            var r = Ui.R(x, y, w, h);
            if (Ui.Button(r, label, on)) act();
            Tip(r, tip);
            x += w + 4;
        }

        static readonly BuildingType[] Public =
        {
            BuildingType.Elementary, BuildingType.HighSchool, BuildingType.College, BuildingType.Clinic, BuildingType.Hospital,
            BuildingType.Police, BuildingType.FireStation, BuildingType.Park, BuildingType.BusDepot
        };

        void BuildMenu()
        {
            float W = Screen.width / Ui.S, H = Screen.height / Ui.S;
            float w = 560, rowH = 46, h = Public.Length * (rowH + 4) + 50;
            float x = W / 2 - w / 2, y = H - 92 - h;
            var r = Claim(Ui.R(x, y, w, h));
            Ui.PanelBox(r);
            Ui.Title(Ui.R(x + 14, y + 8, w - 28, 28), "Public buildings");
            float ry = y + 44;
            foreach (var t in Public)
            {
                var d = Defs.Get(t);
                var rr = Ui.R(x + 10, ry, w - 20, rowH);
                bool on = gc.Tool == Tool.Place && gc.PlaceType == t;
                if (Ui.Button(rr, "", on)) { gc.SetTool(Tool.Place, Zone.None, t); buildMenu = false; }
                Ui.Bold(Ui.R(x + 20, ry + 4, 300, 20), d.Name);
                Ui.Small(Ui.R(x + 20, ry + 24, w - 160, 18), d.Blurb);
                Ui.Right(Ui.R(x + w - 160, ry + 4, 140, 20), Town.Money(d.StartupCost), T.Treasury >= d.StartupCost ? Ui.Text : Ui.Warn);
                double staff = 0; foreach (var j in d.Jobs) staff += Defs.Get(j.Role).Wage * j.Count;
                Ui.Right(Ui.R(x + w - 220, ry + 24, 200, 18), "<size=" + Mathf.RoundToInt(12 * Ui.S) + ">~" + Town.Money(staff * T.Policy.PublicPay + d.Upkeep) + "/mo · " + d.W + "×" + d.H + "</size>", Ui.Dim);
                Tip(rr, d.Blurb + "\nBuilding cost " + Town.Money(d.StartupCost) + ". Running cost about " + Town.Money(staff * T.Policy.PublicPay + d.Upkeep) + " a month at full staff.");
                ry += rowH + 4;
            }
        }

        // ================================================================== toasts
        void Toasts()
        {
            float W = Screen.width / Ui.S;
            float y = 60;
            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (toasts[i].Until < Time.unscaledTime) { toasts.RemoveAt(i); continue; }
            }
            foreach (var t in toasts)
            {
                float w = Mathf.Min(640, 40 + t.Text.Length * 7.5f);
                var r = Ui.R(W / 2 - w / 2, y, w, 32);
                Ui.Frame(r, new Color32(18, 22, 28, 235), t.Col);
                Ui.Center(r, t.Text);
                y += 36;
            }
        }

        // ================================================================== short descriptions
        public string Describe(Building b)
        {
            var d = b.Def;
            string s = T.Title(b);
            if (s.ToLower() != d.Name.ToLower()) s += "  (" + d.Name + ")";
            if (!b.Built) return s + "\nUnder construction: " + Mathf.RoundToInt(b.BuildFraction * 100) + "%";
            if (b.Closed) return s + "\nClosed";
            if (d.Homes > 0) return s + "\n" + b.Households.Count + " of " + d.Homes + " homes lived in" + (b.ForSale ? " · for sale " + Town.Money(b.Value) : "");
            if (d.Jobs.Length > 0) return s + "\n" + b.Employees.Count + " working here";
            return s;
        }
    }
}
