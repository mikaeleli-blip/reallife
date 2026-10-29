using System;
using System.Collections.Generic;
using System.Globalization;

namespace RealLife.Simulation
{
    /// <summary>
    /// The whole town: map, people, households, buildings, loans, laws and books. Engine-free so it can run headless and be
    /// saved as plain JSON. The Unity layer reads it, calls Advance to move the clock, and calls the Place/Zone/Bulldoze tools.
    /// One day is about a real week: a month is four days, a year forty-eight.
    /// </summary>
    [Serializable]
    public partial class Town
    {
        public const int CurrentVersion = 6;
        public const int StartYear = 2025;

        public int SaveVersion = CurrentVersion;
        public string Name;
        public int Seed;
        public int RngState = 1013904223;   // the random number generator, saved so a loaded game carries on the same way
        public int Day;
        public float Hour = 6f;              // the clock within today, for the view
        public int Cols, Rows;
        public int[] Tiles;                  // terrain, road and zone bits (see TileBits)
        public int TerminalId = -1;

        public List<Citizen> People = new List<Citizen>();
        public List<Household> Households = new List<Household>();
        public List<Building> Buildings = new List<Building>();
        public List<Loan> Loans = new List<Loan>();
        public int NextCitizen = 1, NextHousehold = 1, NextBuilding = 1, NextLoan = 1;

        public Policy Policy = new Policy();
        public double Treasury;
        public TownBook Book = new TownBook(), LastBook = new TownBook();
        public TradeBook Trade = new TradeBook(), LastTrade = new TradeBook();
        public string BankName;
        public double BankCapital;           // the bank's own money, lent out to borrowers
        public double BankFunding;           // what the bank has borrowed from outside to lend on
        public double BankProfitLast;
        public float[] PriceDrift;           // world prices against normal
        public float[] Wants;                // by building type: demand nobody in town could meet this month
        public float[] LastWants;
        public float[] WantAvg;              // by building type: unmet demand, averaged over the last few months
        public float[] SpendAway;            // by store type: what people here spend out of town for want of one, averaged
        [NonSerialized] public double[] AwayMonth;
        public int[] EntryMonths;            // by building type: months in a row a new one would have paid its way
        public int[] LastFailDay;            // by building type: when one last went under
        public int[] PrevFailDay;            // ...and the time before that
        public int BirthsMonth, DeathsMonth, ArrivalsMonth, LeftMonth, LastBirths, LastDeaths, LastArrivals, LastLeft;
        public List<LogEntry> Log = new List<LogEntry>();
        public List<HistoryPoint> History = new List<HistoryPoint>();

        // ------------------------------------------------------------------ caches (rebuilt after load)
        [NonSerialized] Dictionary<int, Citizen> peopleById;
        [NonSerialized] Dictionary<int, Household> householdsById;
        [NonSerialized] Dictionary<int, Building> buildingsById;
        [NonSerialized] int[] buildingAt;
        [NonSerialized] int[] roadComp;
        [NonSerialized] int mainComp = -1;
        /// <summary>Bumped whenever the map changes, so the view knows to rebuild.</summary>
        [NonSerialized] public int MapVersion;
        [NonSerialized] public Action<int> DayEnded;
        [NonSerialized] public Action<LogEntry> Logged;

        // ================================================================== tiles
        public static class TileBits
        {
            public const int TerrainMask = 3, Grass = 0, Water = 1, Trees = 2, Sand = 3;
            public const int Road = 4;
            public const int ZoneShift = 3, ZoneMask = 7 << ZoneShift;
        }

        public bool InBounds(int x, int y) { return x >= 0 && y >= 0 && x < Cols && y < Rows; }
        int Idx(int x, int y) { return y * Cols + x; }
        public int Terrain(int x, int y) { return InBounds(x, y) ? Tiles[Idx(x, y)] & TileBits.TerrainMask : TileBits.Water; }
        public bool IsWater(int x, int y) { return Terrain(x, y) == TileBits.Water; }
        public bool HasTrees(int x, int y) { return Terrain(x, y) == TileBits.Trees; }
        public bool IsRoad(int x, int y) { return InBounds(x, y) && (Tiles[Idx(x, y)] & TileBits.Road) != 0; }
        public Zone ZoneAt(int x, int y) { return InBounds(x, y) ? (Zone)((Tiles[Idx(x, y)] & TileBits.ZoneMask) >> TileBits.ZoneShift) : Zone.None; }
        void SetTerrain(int x, int y, int t) { int i = Idx(x, y); Tiles[i] = (Tiles[i] & ~TileBits.TerrainMask) | t; }
        void SetRoadBit(int x, int y, bool on) { int i = Idx(x, y); Tiles[i] = on ? Tiles[i] | TileBits.Road : Tiles[i] & ~TileBits.Road; }
        void SetZoneBits(int x, int y, Zone z) { int i = Idx(x, y); Tiles[i] = (Tiles[i] & ~TileBits.ZoneMask) | ((int)z << TileBits.ZoneShift); }

        public Building BuildingAt(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            int id = buildingAt[Idx(x, y)];
            return id > 0 ? GetBuilding(id) : null;
        }

        // ================================================================== lookups
        public Citizen GetCitizen(int id) { Citizen c; return id >= 0 && peopleById.TryGetValue(id, out c) ? c : null; }
        public Household GetHousehold(int id) { Household h; return id >= 0 && householdsById.TryGetValue(id, out h) ? h : null; }
        public Building GetBuilding(int id) { Building b; return id >= 0 && buildingsById.TryGetValue(id, out b) ? b : null; }
        public Household HouseholdOf(Citizen c) { return c == null ? null : GetHousehold(c.HouseholdId); }
        public Building HomeOf(Household h) { return h == null ? null : GetBuilding(h.HomeId); }
        public Building HomeOf(Citizen c) { return HomeOf(HouseholdOf(c)); }
        public Building WorkOf(Citizen c) { return c == null ? null : GetBuilding(c.WorkId); }
        public Building Terminal { get { return GetBuilding(TerminalId); } }
        public int Population { get { return People.Count; } }

        /// <summary>Rebuilds every lookup after a load or a big change.</summary>
        public void Index()
        {
            peopleById = new Dictionary<int, Citizen>();
            foreach (var c in People) peopleById[c.Id] = c;
            householdsById = new Dictionary<int, Household>();
            foreach (var h in Households) householdsById[h.Id] = h;
            buildingsById = new Dictionary<int, Building>();
            foreach (var b in Buildings) buildingsById[b.Id] = b;
            IndexCars();
            buildingAt = new int[Cols * Rows];
            foreach (var b in Buildings) Stamp(b, b.Id);
            if (PriceDrift == null || PriceDrift.Length != Defs.GoodCount) PriceDrift = Resized(PriceDrift, Defs.GoodCount, 1f);
            int nt = Defs.All.Length;
            if (Wants == null || Wants.Length != nt) Wants = new float[nt];
            if (LastWants == null || LastWants.Length != nt) LastWants = new float[nt];
            if (WantAvg == null || WantAvg.Length != nt) WantAvg = new float[nt];
            if (SpendAway == null || SpendAway.Length != nt) SpendAway = new float[nt];
            if (AwayMonth == null || AwayMonth.Length != nt) AwayMonth = new double[nt];
            if (EntryMonths == null || EntryMonths.Length != nt) EntryMonths = new int[nt];
            if (PracticeOwed == null || PracticeOwed.Length != PracticeCount) PracticeOwed = new double[PracticeCount];
            if (PracticeAway == null || PracticeAway.Length != PracticeCount) PracticeAway = new float[PracticeCount];
            if (LastFailDay == null || LastFailDay.Length != nt) { LastFailDay = new int[nt]; for (int i = 0; i < nt; i++) LastFailDay[i] = -9999; }
            if (PrevFailDay == null || PrevFailDay.Length != nt) { PrevFailDay = new int[nt]; for (int i = 0; i < nt; i++) PrevFailDay[i] = -9999; }
            int nz = Enum.GetValues(typeof(Zone)).Length;
            if (ZoneDemand == null || ZoneDemand.Length != nz) ZoneDemand = new float[nz];
            if (LotShort == null || LotShort.Length != nz) LotShort = new bool[nz];
            // saves from before there were more goods: their stock carries over
            foreach (var b in Buildings) if (b.Stock == null || b.Stock.Length != Defs.GoodCount) b.Stock = Resized(b.Stock, Defs.GoodCount, 0f);
            foreach (var h in Households) if (h.Has == null || h.Has.Length != Defs.GoodCount) h.Has = Resized(h.Has, Defs.GoodCount, -1f);
            foreach (var h in Households) for (int i = 0; i < h.Has.Length; i++) if (h.Has[i] < 0) h.Has[i] = 0.55f + Rand() * 0.4f;
            RebuildRoads();
            MapVersion++;
        }

        static float[] Resized(float[] a, int n, float fill)
        {
            var r = new float[n];
            for (int i = 0; i < n; i++) r[i] = a != null && i < a.Length ? a[i] : fill;
            return r;
        }

        void Stamp(Building b, int id)
        {
            var d = b.Def;
            for (int y = b.Y; y < b.Y + d.H; y++)
                for (int x = b.X; x < b.X + d.W; x++)
                    if (InBounds(x, y)) buildingAt[Idx(x, y)] = id;
        }

        // ================================================================== randomness (saved with the town)
        public float Rand()
        {
            uint x = unchecked((uint)RngState); x ^= x << 13; x ^= x >> 17; x ^= x << 5; if (x == 0) x = 2463534242;
            RngState = unchecked((int)x);
            return (x & 0xFFFFFF) / 16777216f;
        }
        public int RandInt(int lo, int hiExclusive) { return lo + (int)(Rand() * (hiExclusive - lo)); }
        public bool Chance(float p) { return Rand() < p; }
        public T Pick<T>(IList<T> l) { return l[RandInt(0, l.Count)]; }
        public float Gauss(float mean, float sd)
        {
            float u = Math.Max(1e-6f, Rand()), v = Rand();
            return mean + sd * (float)(Math.Sqrt(-2.0 * Math.Log(u)) * Math.Cos(2 * Math.PI * v));
        }

        // ================================================================== calendar
        static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        public int Year { get { return StartYear + Day / Balance.DaysPerYear; } }
        public int MonthOfYear { get { return (Day / Balance.DaysPerMonth) % Balance.MonthsPerYear; } }
        public int Week { get { return Day % Balance.DaysPerMonth + 1; } }
        public bool MonthStart { get { return Day % Balance.DaysPerMonth == 0; } }
        public string MonthName { get { return Months[MonthOfYear]; } }
        public string Date { get { return MonthName + " " + Year + ", week " + Week; } }
        public static string DateOf(int day) { return Months[(day / Balance.DaysPerMonth) % 12] + " " + (StartYear + day / Balance.DaysPerYear); }
        /// <summary>0 winter .. 3 autumn, for the view.</summary>
        /// <summary>What the town looks like: winter is December and January, spring February to April, summer May to August,
        /// autumn September to November.</summary>
        public int Season { get { int m = MonthOfYear; return m == 11 || m == 0 ? 0 : m < 4 ? 1 : m < 8 ? 2 : 3; } }
        /// <summary>Snow on the ground: through December and the first week of January. The rest of January is still winter,
        /// just without the snow.</summary>
        public bool Snowy { get { int m = MonthOfYear; return m == 11 || (m == 0 && Week == 1); } }
        /// <summary>The season the land is drawn in: winter only while there's snow, early spring after.</summary>
        public int LookSeason { get { return Season == 0 && !Snowy ? 1 : Season; } }

        /// <summary>The growing year, for the farms: nothing much grows from December to February.</summary>
        public int CropSeason { get { int m = MonthOfYear; return m == 11 || m < 2 ? 0 : m < 5 ? 1 : m < 8 ? 2 : 3; } }

        // ================================================================== formatting
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        /// <summary>A family by name: the Nelsons, the Joneses.</summary>
        public static string Family(string surname)
        {
            if (string.IsNullOrEmpty(surname)) return surname;
            bool es = surname.EndsWith("s") || surname.EndsWith("x") || surname.EndsWith("z") || surname.EndsWith("ch") || surname.EndsWith("sh");
            return surname + (es ? "es" : "s");
        }

        public static string Money(double v)
        {
            string s = v < 0 ? "-$" : "$"; double a = Math.Abs(v);
            if (a >= 1e6) return s + (a / 1e6).ToString(a >= 1e7 ? "0.0" : "0.00", Inv) + "M";
            if (a >= 1e5) return s + (a / 1e3).ToString("0", Inv) + "k";
            return s + a.ToString("#,0", Inv);
        }
        public static string Pct(double v) { return (v * 100).ToString(Math.Abs(v) < 0.1 ? "0.0" : "0", Inv) + "%"; }

        public void AddLog(string text, LogKind kind, int subject = -1, bool person = false)
        {
            var e = new LogEntry { Day = Day, Text = text, Kind = kind, Subject = subject, AboutPerson = person };
            Log.Add(e);
            if (Log.Count > 400) Log.RemoveRange(0, Log.Count - 400);
            if (Logged != null) Logged(e);
        }
        void Note(Citizen c, string text) { if (c != null) c.Note = DateOf(Day) + ": " + text; }

        // ================================================================== the clock
        /// <summary>Moves time on by a fraction of a day. Each midnight runs the day's economy.</summary>
        public void Advance(float days)
        {
            Hour += days * 24f;
            int guard = 0;
            while (Hour >= 24f && guard++ < 20)
            {
                Hour -= 24f;
                DayTick();
            }
        }

        // ================================================================== roads and access
        static readonly int[] DX = { 0, 1, 0, -1 }, DY = { 1, 0, -1, 0 };
        public static int StepX(int dir) { return DX[dir]; }
        public static int StepY(int dir) { return DY[dir]; }

        public void RebuildRoads()
        {
            roadComp = new int[Cols * Rows];
            for (int i = 0; i < roadComp.Length; i++) roadComp[i] = -1;
            int comp = 0; var q = new Queue<int>();
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    if (!IsRoad(x, y) || roadComp[Idx(x, y)] >= 0) continue;
                    roadComp[Idx(x, y)] = comp; q.Enqueue(Idx(x, y));
                    while (q.Count > 0)
                    {
                        int i = q.Dequeue(), cx = i % Cols, cy = i / Cols;
                        for (int d = 0; d < 4; d++)
                        {
                            int nx = cx + DX[d], ny = cy + DY[d];
                            if (!IsRoad(nx, ny) || roadComp[Idx(nx, ny)] >= 0) continue;
                            roadComp[Idx(nx, ny)] = comp; q.Enqueue(Idx(nx, ny));
                        }
                    }
                    comp++;
                }
            mainComp = -1;
            var t = Terminal;
            if (t != null) { int ax, ay; if (AccessTile(t, out ax, out ay)) mainComp = roadComp[Idx(ax, ay)]; }
        }

        /// <summary>The road tile in front of a building.</summary>
        public bool AccessTile(Building b, out int ax, out int ay)
        {
            var d = b.Def;
            int f = b.Facing;
            // walk along the facing side, middle first
            int len = f == 0 || f == 2 ? d.W : d.H;
            for (int k = 0; k < len; k++)
            {
                int o = (len - 1) / 2 + ((k % 2 == 0) ? k / 2 : -(k + 1) / 2);
                if (o < 0 || o >= len) continue;
                int x, y;
                switch (f)
                {
                    case 0: x = b.X + o; y = b.Y + d.H; break;
                    case 1: x = b.X + d.W; y = b.Y + o; break;
                    case 2: x = b.X + o; y = b.Y - 1; break;
                    default: x = b.X - 1; y = b.Y + o; break;
                }
                if (IsRoad(x, y)) { ax = x; ay = y; return true; }
            }
            ax = ay = -1; return false;
        }

        /// <summary>Is it on a road that reaches the highway?</summary>
        public bool Connected(Building b)
        {
            int ax, ay;
            if (!AccessTile(b, out ax, out ay)) return false;
            return mainComp >= 0 && roadComp[Idx(ax, ay)] == mainComp;
        }

        /// <summary>The side of a footprint that has a road next to it (-1: none). Prefers south, then the other sides.</summary>
        public int FindFacing(int x, int y, int w, int h)
        {
            int[] order = { 2, 0, 1, 3 };
            foreach (int f in order)
            {
                int len = f == 0 || f == 2 ? w : h;
                for (int o = 0; o < len; o++)
                {
                    int tx, ty;
                    switch (f)
                    {
                        case 0: tx = x + o; ty = y + h; break;
                        case 1: tx = x + w; ty = y + o; break;
                        case 2: tx = x + o; ty = y - 1; break;
                        default: tx = x - 1; ty = y + o; break;
                    }
                    if (IsRoad(tx, ty)) return f;
                }
            }
            return -1;
        }

        public bool FootprintFree(int x, int y, int w, int h)
        {
            for (int ty = y; ty < y + h; ty++)
                for (int tx = x; tx < x + w; tx++)
                {
                    if (!InBounds(tx, ty) || IsWater(tx, ty) || IsRoad(tx, ty) || buildingAt[Idx(tx, ty)] > 0) return false;
                }
            return true;
        }

        public float Distance(Building a, Building b) { return a == null || b == null ? 999f : Math.Abs(a.CenterX - b.CenterX) + Math.Abs(a.CenterY - b.CenterY); }

        // ================================================================== creating and removing buildings
        Building AddBuilding(BuildingType t, int x, int y, int facing, bool built)
        {
            var d = Defs.Get(t);
            var b = new Building { Id = NextBuilding++, Type = t, X = x, Y = y, Facing = facing, BuiltDay = Day, Built = built, Progress = built ? d.Work : 0, Value = d.Value };
            if (d.Sells.HasValue) b.Price = Defs.Retail[(int)d.Sells.Value];
            else if (d.Serves != Service.None) b.Price = d.ServicePrice;
            else if (d.Makes.HasValue) b.Price = Defs.Wholesale[(int)d.Makes.Value];
            b.Rent = d.Rent;
            if (b.Value <= 0) b.Value = d.StartupCost;
            Buildings.Add(b); buildingsById[b.Id] = b; Stamp(b, b.Id);
            for (int ty = y; ty < y + d.H; ty++) for (int tx = x; tx < x + d.W; tx++) { if (HasTrees(tx, ty)) SetTerrain(tx, ty, TileBits.Grass); }
            MapVersion++;
            return b;
        }

        void RemoveBuilding(Building b)
        {
            foreach (var id in b.Employees.ToArray()) { var c = GetCitizen(id); if (c != null) LeaveJob(c, null); }
            foreach (var hid in b.Households.ToArray()) { var h = GetHousehold(hid); if (h != null) { h.HomeId = -1; h.Owns = false; } }
            foreach (var c in People) if (c.SchoolId == b.Id) c.SchoolId = -1;
            foreach (var l in Loans.ToArray()) if (l.BuildingId == b.Id && l.Kind == LoanKind.Business) Loans.Remove(l);
            foreach (var o in Buildings) if (o.ContractorId == b.Id) o.ContractorId = -1;
            Stamp(b, 0);
            Buildings.Remove(b); buildingsById.Remove(b.Id);
            MapVersion++;
        }

        // ================================================================== money helpers
        public void HouseholdPays(Household h, double amount, int line) { h.Cash -= amount; h.Out[line] += amount; }
        public void HouseholdEarns(Household h, double amount, int line) { h.Cash += amount; h.In[line] += amount; }

        /// <summary>Total money held by everyone in town.</summary>
        public double HouseholdCash() { double s = 0; foreach (var h in Households) s += h.Cash; return s; }
    }
}
