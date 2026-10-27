using System;
using System.Linq;
using RealLife.Simulation;

static class Program
{
    static int Main(string[] args)
    {
        int years = args.Length > 0 ? int.Parse(args[0]) : 5;
        int seed = args.Length > 1 ? int.Parse(args[1]) : 12345;
        bool verbose = args.Length > 2 && args[2] == "v";
        string watch = args.Length > 3 ? args[3] : null;
        string mode = args.Length > 4 ? args[4] : "";
        Town t;
        if (mode.StartsWith("load:"))
        {
            // a save from the game: JsonUtility writes public fields
            var opts = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            t = System.Text.Json.JsonSerializer.Deserialize<Town>(System.IO.File.ReadAllText(mode.Substring(5)), opts);
            t.Index(); t.Migrate();
        }
        else t = mode == "blank" ? Town.CreateNew(seed) : Town.CreateDeveloped(seed);
        var bot = mode == "blank" ? new Bot(t) : null;
        if (Environment.GetEnvironmentVariable("DEVNOTES") == "1") t.DevNotes = new System.Collections.Generic.List<string>();
        Console.WriteLine($"{t.Name}: pop {t.Population}, households {t.Households.Count}, buildings {t.Buildings.Count}, jobs {t.JobSlots()}, employed {t.Employed()}, unemployed {t.Unemployed()}");
        if (args.Length > 4 && args[4] == "migrate")
        {
            // pretend this is an old save: outside firms own the rentals and the big businesses, no property firms, farms on industrial land
            foreach (var b in t.Buildings.ToList())
            {
                if (b.Type == BuildingType.Realty) { b.Owner = Owner.Outside; b.Closed = true; foreach (var id in b.Employees.ToList()) { var c = t.GetCitizen(id); c.Owner = false; t.LeaveJob(c, null); } }
                else if (b.Owner == Owner.Firm && b.OwnerId >= 0) { b.Owner = Owner.Outside; b.OwnerId = -1; b.OwnerName = "Horizon Properties"; }
                else if (Town.IsBusiness(b) && b.Brand != null) { b.Owner = Owner.Outside; b.OwnerName = b.Brand; b.Brand = null; foreach (var id in b.Employees) t.GetCitizen(id).Owner = false; }
            }
            t.Loans.RemoveAll(l => l.FirmId >= 0);
            for (int y = 59; y < 71; y++) for (int x = 15; x < 38; x++) if (t.ZoneAt(x, y) == Zone.Farmland) t.SetZone(x, y, Zone.Industrial);
            t.SaveVersion = 1;
            t.Migrate();
            Console.WriteLine("migrated: outside-owned left " + t.Buildings.Count(b => b.Owner == Owner.Outside) + ", property firms " + t.PropertyFirms().Count + " owning " + t.PropertyFirms().Sum(f => t.Portfolio(f).Count()) + ", farm tiles farmland " + t.Buildings.Where(b => b.Type == BuildingType.Farm).All(b => t.ZoneAt(b.X, b.Y) == Zone.Farmland));
            foreach (var f in t.PropertyFirms()) Console.WriteLine("  " + t.Title(f) + " owner " + f.OwnerName + " staff " + f.Employees.Count + " homes " + t.Portfolio(f).Count());
        }
        if (args.Length > 4 && args[4] == "schools")
        {
            foreach (var bt in new[] { BuildingType.HighSchool, BuildingType.College, BuildingType.Park, BuildingType.BusDepot })
            {
                bool done = false;
                for (int y = 30; y < 70 && !done; y++) for (int x = 30; x < 75 && !done; x++) if (t.CanPlace(bt, x, y) == null) { t.PlacePublic(bt, x, y); done = true; }
                Console.WriteLine("placed " + bt + ": " + done);
            }
            for (int y = 64; y < 75; y++) for (int x = 38; x < 56; x++) t.SetZone(x, y, Zone.Residential);
            for (int x = 38; x <= 56; x++) t.PlaceRoad(x, 70);
        }
        Header();
        Row(t);
        int firstDay = t.Day;
        string logGrep = Environment.GetEnvironmentVariable("LOGGREP");
        int months = years * 12;
        for (int m = 0; m < months; m++)
        {
            if (bot != null) bot.Month(m);
            for (int d = 0; d < Balance.DaysPerMonth; d++) t.Advance(1f);
            Row(t);
            if (m % 12 == 11 || Environment.GetEnvironmentVariable("CARS") == "1")
            {
                var owned = t.Cars.Where(c => c.HouseholdId >= 0).ToList();
                var garages = t.Buildings.Where(x => x.Type == BuildingType.AutoRepair && x.Open).ToList();
                Console.WriteLine($"      cars {owned.Count} ({owned.Count / (float)Math.Max(1, t.Households.Count):0.00}/hh, none {t.Households.Count(h => t.CarCount(h) == 0)}) age {(owned.Count > 0 ? owned.Average(c => t.CarAge(c)) : 0):0.0} cond {(owned.Count > 0 ? owned.Average(c => c.Condition) : 0):0.00} broken {owned.Count(c => c.Broken)} inShop {owned.Count(c => c.ShopId >= 0)} away {owned.Count(c => c.ShopId == -2)} | garages {garages.Count} booked {string.Join(",", garages.Select(g => t.Cars.Where(c => c.ShopId == g.Id).Sum(c => c.JobHours).ToString("0")))}h | lots {string.Join(",", t.Buildings.Where(x => x.Type == BuildingType.CarDealer && x.Open).Select(d => t.LotOf(d).Count()))} | sold {t.LastCarsSold} scrapped {t.LastScrapped} repairsAway {t.LastRepairsAway}");
                Console.WriteLine("      " + Homes(t));
                if (t.BusStops != null && t.BusStops.Count > 0) Console.WriteLine("      " + Buses(t));
                if (Environment.GetEnvironmentVariable("BROKEN") == "1")
                    foreach (var c in owned.Where(c => c.Broken && c.ShopId == -1).Take(12))
                    {
                        var h = t.GetHousehold(c.HouseholdId);
                        Console.WriteLine($"        broken car {c.Id} {c.Class} age {t.CarAge(c):0.0} value {Town.Money(t.CarValue(c))} hours {c.JobHours} parts {c.JobParts:0.0} | hh cash {Town.Money(h.Cash)} income {Town.Money(t.MonthlyIncome(h))} cars {t.CarCount(h)} members {h.Members.Count}");
                    }
            }
            if (bot != null && m % 12 == 11) Console.WriteLine("      commuters " + t.Commuters() + "/" + t.CommuteCap() + " pay in " + Town.Money(t.LastTrade.Commuting) + "  advice: " + string.Join(" | ", t.Advice()));
            if (t.DevNotes != null) { if (t.DevNotes.Count > 0) Console.WriteLine("      dev: " + string.Join(" | ", t.DevNotes)); t.DevNotes.Clear(); }
            if (verbose) foreach (var e in t.Log.Where(e => e.Day > t.Day - 4)) Console.WriteLine("      " + e.Text);
            if (watch != null) foreach (var b in t.Buildings.Where(b => b.Type.ToString() == watch)) Console.WriteLine($"      [{b.Type} {b.Id}] {(b.Closed ? "CLOSED" : "")} staff {b.Employees.Count} st {b.Staffing:0.00} cash {Town.Money(b.Cash)} rev {Town.Money(b.LastRevenue)} wages {Town.Money(b.LastWages)} supplies {Town.Money(b.LastSupplies)} other {Town.Money(b.LastOther)} tax {Town.Money(b.LastTax)} profit {Town.Money(b.LastProfit)} use {b.LastUse:0.00} lost {b.LastLost} cust {b.LastCustomers} price {b.Price:0.00} debt {Town.Money(t.Debt(b))} coll {Town.Money(t.Collateral(b))} pay {Town.Money(t.LoanPayments(b))} loss {b.LossMonths}");
        }
        if (!string.IsNullOrEmpty(logGrep))
            foreach (var e in t.Log.Where(e => e.Day > firstDay && System.Text.RegularExpressions.Regex.IsMatch(e.Text, logGrep))) Console.WriteLine("  log " + e.Day + ": " + e.Text);
        Console.WriteLine();
        Console.WriteLine("Businesses:");
        foreach (var b in t.Buildings.Where(Town.IsBusiness).OrderBy(b => b.Type))
            Console.WriteLine($"  {b.Def.Name,-24} {(!b.Built ? "building" : b.Closed ? "CLOSED" : ""),-8} staff {b.Employees.Count,2}/{b.Def.TotalJobs,-2} st {b.Staffing:0.00} wage x{b.WageMult:0.00} price {b.Price,8:0.00} cash {Town.Money(b.Cash),8} rev {Town.Money(b.LastRevenue),8} profit {Town.Money(b.LastProfit),8} use {b.LastUse:0.00} lost {b.LastLost} owner {b.Owner} {b.OwnerName}");
        Console.WriteLine("Trade last month: in " + Town.Money(t.LastTrade.In) + " (exports " + Town.Money(t.LastTrade.Exports) + ", services " + Town.Money(t.LastTrade.Services) + ", pensions " + Town.Money(t.LastTrade.Pensions) + ", invest " + Town.Money(t.LastTrade.Investment) +
            ")  out " + Town.Money(t.LastTrade.Outflow) + " (imports " + Town.Money(t.LastTrade.Imports) + ", utilities " + Town.Money(t.LastTrade.Utilities) + ", profits " + Town.Money(t.LastTrade.Profits) + ", freight " + Town.Money(t.LastTrade.Freight) + ")");
        var b2 = t.LastBook;
        Console.WriteLine($"Town book: in {Town.Money(b2.TotalIn)} (inc {Town.Money(b2.IncomeTax)} sales {Town.Money(b2.SalesTax)} prop {Town.Money(b2.PropertyTax)} biz {Town.Money(b2.BusinessTax)} fees {Town.Money(b2.Fees)}) out {Town.Money(b2.TotalOut)} (wages {Town.Money(b2.PublicWages)} upkeep {Town.Money(b2.Upkeep)} benefits {Town.Money(b2.Benefits)} build {Town.Money(b2.Construction)} interest {Town.Money(b2.Interest)})");
        Console.WriteLine("Edu: " + string.Join(", ", Enum.GetValues(typeof(Edu)).Cast<Edu>().Select(e => e + " " + t.People.Count(c => !c.IsChild && c.Edu == e))));
        Console.WriteLine("Advice: " + string.Join(" | ", t.Advice()));
        Console.WriteLine(Homes(t));
        Console.WriteLine("Wants: " + string.Join(", ", Defs.All.Where(x => t.LastWants[(int)x] > 0).Select(x => x + " " + t.LastWants[(int)x].ToString("0"))));
        int nans = t.Households.Count(h => double.IsNaN(h.Cash) || double.IsInfinity(h.Cash) || double.IsNaN(h.FunMoney)) + t.Buildings.Count(b => double.IsNaN(b.Cash) || double.IsNaN(b.Price) || double.IsNaN(b.Value) || b.Stock.Any(x => float.IsNaN(x))) +
                   t.People.Count(c => float.IsNaN(c.Age) || float.IsNaN(c.Happiness) || float.IsNaN(c.Health) || double.IsNaN(c.Wage)) + t.Loans.Count(l => double.IsNaN(l.Principal)) + (double.IsNaN(t.Treasury) ? 1 : 0);
        Console.WriteLine(nans == 0 ? "No NaNs (save-safe)." : "NaN values: " + nans);
        if (Environment.GetEnvironmentVariable("HAPPY") == "1")
            foreach (var h in t.Households.Take(25))
            {
                var ms = t.People.Where(c => c.HouseholdId == h.Id).ToList();
                double inc = h.LastIn.Sum(), spend = h.LastOut.Sum(), housing = h.LastOut[HouseholdBook.Housing];
                Console.WriteLine($"  hh {h.Name,-10} n {ms.Count} cash {Town.Money(h.Cash),8} in {Town.Money(inc),7} out {Town.Money(spend),7} housing {housing / Math.Max(1, inc):0.00} pantry {h.Pantry:0.0} owns {h.Owns} sad {h.SadMonths} " +
                    string.Join(" ", ms.Select(c => $"[{c.Age:0} {(c.IsChild ? "kid" : t.Commutes(c) ? "comm" : c.WorkId >= 0 ? "work" : c.InLaborForce ? "LOOK" : "home")} h{c.Happiness:0} hp{c.Health:0} out{c.LastOutings}{(c.Stage != Stage.None && c.SchoolId < 0 ? " NOSCHOOL" : "")}]")));
            }
        Console.WriteLine("Loans: " + t.Loans.Count + " owed " + Town.Money(t.Loans.Sum(l => l.Principal)) + " bank capital " + Town.Money(t.BankCapital) + " funding " + Town.Money(t.BankFunding));
        return 0;
    }

    /// <summary>A player for an empty town: lays out the old starting grid off the highway, zones homes first, then shops,
    /// industry and farms, builds the town hall and services, and opens more land as it fills.</summary>
    class Bot
    {
        readonly Town t;
        int nextBlock;
        public Bot(Town town) { t = town; }

        void Road(int x0, int y0, int x1, int y1)
        {
            int dx = Math.Sign(x1 - x0), dy = Math.Sign(y1 - y0), x = x0, y = y0;
            while (true) { if (!t.IsRoad(x, y)) t.PlaceRoad(x, y); if (x == x1 && y == y1) break; if (x != x1) x += dx; else y += dy; }
        }
        void Zone(int x0, int y0, int x1, int y1, Zone z) { for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++) t.SetZone(x, y, z); }
        void Place(BuildingType bt, int x0, int y0)
        {
            for (int r = 0; r < 20; r++)
                for (int y = y0 - r; y <= y0 + r; y++) for (int x = x0 - r; x <= x0 + r; x++)
                    if (t.CanPlace(bt, x, y) == null) { t.PlacePublic(bt, x, y); Console.WriteLine("      bot: placed " + bt); return; }
        }
        bool Has(BuildingType bt) { return t.Buildings.Any(b => b.Type == bt); }

        // residential blocks to open as the town fills, each with the streets around it
        static readonly int[][] Blocks =
        {
            new[] { 39, 41, 43, 45 }, new[] { 45, 41, 49, 45 }, new[] { 39, 47, 43, 51 }, new[] { 51, 41, 55, 45 }, new[] { 39, 53, 43, 57 },
            new[] { 51, 53, 55, 57 }, new[] { 39, 59, 55, 63 }, new[] { 33, 41, 37, 45 }, new[] { 33, 47, 37, 51 }, new[] { 33, 53, 37, 57 },
            new[] { 39, 35, 55, 39 }, new[] { 39, 65, 55, 69 }, new[] { 27, 41, 31, 57 }
        };

        void OpenBlock(RealLife.Simulation.Zone z = RealLife.Simulation.Zone.Residential)
        {
            if (nextBlock >= Blocks.Length) return;
            var b = Blocks[nextBlock++];
            Road(b[0] - 1, b[1] - 1, b[2] + 1, b[1] - 1); Road(b[2] + 1, b[1] - 1, b[2] + 1, b[3] + 1);
            Road(b[2] + 1, b[3] + 1, b[0] - 1, b[3] + 1); Road(b[0] - 1, b[3] + 1, b[0] - 1, b[1] - 1);
            Zone(b[0], b[1], b[2], b[3], z);
        }

        public void Month(int m)
        {
            if (m == 0)
            {
                Road(16, 46, 38, 46);
                OpenBlock(); OpenBlock(); OpenBlock();
            }
            if (m == 2)
            {
                // downtown
                Road(44, 46, 56, 46); Road(50, 40, 50, 58); Road(44, 52, 56, 52); Road(56, 40, 56, 58); Road(44, 58, 56, 58);
                Zone(45, 47, 49, 51, RealLife.Simulation.Zone.Commercial); Zone(51, 47, 55, 51, RealLife.Simulation.Zone.Commercial);
                Zone(45, 53, 49, 57, RealLife.Simulation.Zone.Commercial);
                Place(BuildingType.TownHall, 45, 47);
            }
            if (m == 4)
            {
                // industry to the east
                Road(56, 40, 72, 40); Road(56, 52, 72, 52); Road(72, 40, 72, 64); Road(56, 64, 72, 64); Road(64, 52, 64, 64);
                Zone(57, 41, 71, 51, RealLife.Simulation.Zone.Industrial); Zone(57, 53, 63, 63, RealLife.Simulation.Zone.Industrial);
            }
            int pop = t.Population, kids = t.People.Count(c => c.IsChild && c.Age >= 6);
            if (kids >= 8 && !Has(BuildingType.Elementary)) Place(BuildingType.Elementary, 39, 53);
            if (pop >= 120 && !Has(BuildingType.Clinic)) Place(BuildingType.Clinic, 51, 41);
            if (m == 10)
            {
                // farms down the farm road
                Road(14, 58, 38, 58); Road(26, 58, 26, 70); Road(38, 46, 38, 58);
                Zone(15, 59, 25, 63, RealLife.Simulation.Zone.Farmland); Zone(27, 59, 37, 63, RealLife.Simulation.Zone.Farmland);
            }
            if (pop >= 200 && !Has(BuildingType.Police)) Place(BuildingType.Police, 45, 41);
            if (pop >= 260 && !Has(BuildingType.FireStation)) Place(BuildingType.FireStation, 51, 53);
            if (m == 24) Zone(65, 53, 71, 63, RealLife.Simulation.Zone.Industrial);
            if (pop >= 150 && !Has(BuildingType.Park)) Place(BuildingType.Park, 39, 47);
            if (t.People.Count(c => c.Stage == Stage.HighSchool) >= 12 && !Has(BuildingType.HighSchool)) Place(BuildingType.HighSchool, 39, 59);
            if (pop >= 450 && !Has(BuildingType.College)) Place(BuildingType.College, 39, 65);
            if (pop >= 400 && !Has(BuildingType.BusDepot)) Place(BuildingType.BusDepot, 51, 59);
            if (m > 0 && !t.HasLot(BuildingType.House)) { OpenBlock(); Console.WriteLine("      bot: opened block " + nextBlock); }
            if (m > 0 && Environment.GetEnvironmentVariable("NOFLATS") != "1" && t.ZoneDemand[(int)RealLife.Simulation.Zone.Apartments] > 0.2f && !t.HasLot(BuildingType.Apartments))
            { OpenBlock(RealLife.Simulation.Zone.Apartments); Console.WriteLine("      bot: opened a block for flats " + nextBlock); }
            if (m > 24 && m % 12 == 0 && t.ZonedFree(RealLife.Simulation.Zone.Commercial) < 6) { Road(56, 34, 56, 40); Zone(51, 35, 55, 39, RealLife.Simulation.Zone.Commercial); }
        }
    }

    static int RoadSteps(Town t, int x0, int y0, int x1, int y1)
    {
        var prev = new System.Collections.Generic.Dictionary<int, int>();
        var q = new System.Collections.Generic.Queue<int>();
        int s = y0 * t.Cols + x0, g = y1 * t.Cols + x1;
        prev[s] = 0; q.Enqueue(s);
        while (q.Count > 0)
        {
            int i = q.Dequeue();
            if (i == g) return prev[i];
            int x = i % t.Cols, y = i / t.Cols;
            for (int d = 0; d < 4; d++)
            {
                int nx = x + Town.StepX(d), ny = y + Town.StepY(d);
                if (!t.IsRoad(nx, ny)) continue;
                int j = ny * t.Cols + nx;
                if (prev.ContainsKey(j)) continue;
                prev[j] = prev[i] + 1; q.Enqueue(j);
            }
        }
        return -1;
    }

    static string Buses(Town t)
    {
        var loops = t.BusRoutes.Select(r =>
        {
            int len = 0;
            for (int i = 0; i < r.Stops.Count; i++) { var a = t.BusStops[r.Stops[i]]; var b = t.BusStops[r.Stops[(i + 1) % r.Stops.Count]]; len += Math.Max(0, RoadSteps(t, a.X, a.Y, b.X, b.Y)); }
            return $"{r.Stops.Count} stops {len} tiles x{r.Buses}";
        });
        int kids = t.People.Count(c => c.RidesBus && c.IsChild), students = t.People.Count(c => c.RidesBus && !c.IsChild && c.WorkId < 0), comm = t.People.Count(c => c.RidesBus && t.Commutes(c));
        return $"bus: {t.BusStops.Count} stops, loops [{string.Join("; ", loops)}] riders {t.BusRiders} (kids {kids}, students {students}, commuters {comm}) cap {t.BusCapacity()} fares {Town.Money(t.LastFares)} carless hh {t.Households.Count(h => t.CarCount(h) == 0)}";
    }

    static string Homes(Town t)
    {
        var houses = t.Buildings.Where(b => b.Type == BuildingType.House).ToList();
        var flats = t.Buildings.Where(b => b.Type == BuildingType.Apartments).ToList();
        int owned = houses.Count(b => b.Built && b.Households.Count > 0 && t.GetHousehold(b.Households[0]) != null && t.GetHousehold(b.Households[0]).Owns);
        string firms = string.Join(" ", t.PropertyFirms().Select(f => $"[{t.Portfolio(f).Count(o => o.Type == BuildingType.House)}h {t.Portfolio(f).Count(o => o.Type == BuildingType.Apartments)}a rev {Town.Money(f.LastRevenue)} profit {Town.Money(f.LastProfit)} cash {Town.Money(f.Cash)}]"));
        return $"homes: houses {houses.Count(b => b.Built)} (+{houses.Count(b => !b.Built)} going up; owned {owned}, for sale {houses.Count(b => b.Built && b.ForSale)}, let {houses.Count(b => b.Built && !b.ForSale && b.Households.Count > 0) - owned}) flats {flats.Count(b => b.Built)} blocks (+{flats.Count(b => !b.Built)}) {flats.Sum(b => b.Households.Count)}/{flats.Count(b => b.Built) * 12} let | vacant {t.VacantUnits()} wants house {t.LastWants[(int)BuildingType.House]:0} flat {t.LastWants[(int)BuildingType.Apartments]:0} | demand R {t.ZoneDemand[1]:0.00}{(t.ZoneDemand.Length > 5 ? " A " + t.ZoneDemand[5].ToString("0.00") : "")} | firms {firms}";
    }

    static void Header() => Console.WriteLine("date          pop  hh  emp unemp  un%  jobs open vac  hap  treasury   avgwage  medcash  prices  rent   houseIdx biz  trade-in trade-out  b/d/a/l");
    static void Row(Town t)
    {
        int open = t.Openings().Sum(o => o.Count);
        Console.WriteLine($"{t.MonthName} {t.Year} w{t.Week} {t.Population,5} {t.Households.Count,3} {t.Employed(),4} {t.Unemployed(),4} {t.UnemploymentRate()*100,5:0.0} {t.JobSlots(),4} {open,4} {t.VacantUnits(),3} {t.AverageHappiness(),4:0} {Town.Money(t.Treasury),9} {Town.Money(t.AverageWage()),8} {Town.Money(t.MedianCash()),8} {t.PriceLevel(),6:0.00} {Town.Money(t.AverageRent()),6} {t.HousingIndex,6:0.00} {t.Buildings.Count(b => Town.IsBusiness(b) && b.Open),4} {Town.Money(t.LastTrade.In),9} {Town.Money(t.LastTrade.Outflow),9}  {t.LastBirths}/{t.LastDeaths}/{t.LastArrivals}/{t.LastLeft}");
    }
}
