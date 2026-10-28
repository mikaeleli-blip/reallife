using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>The order things happen in: each day, and on the first day of each month.</summary>
    public partial class Town
    {
        void DayTick()
        {
            Day++;
            ResetDay();
            if (ImportsByGood == null) ImportsByGood = new double[Defs.GoodCount];
            if (ExportsByGood == null) ExportsByGood = new double[Defs.GoodCount];
            PeopleDaily();
            if (MonthStart) Monthly();
            LaborDaily();
            CommuteDaily();
            Attendance();
            BusDaily();
            ReadyTrucks();
            ShopsOpen();
            Produce();
            Restock();
            ConstructionDaily();
            HouseholdsDaily();
            Visitors();
            GaragesDaily();
            DealersDaily();
            Exports();
            if (DayEnded != null) DayEnded(Day);
        }

        void Monthly()
        {
            CloseMonth();
            Payroll();
            HousingMonthly();
            BusFares();
            LoansMonthly();
            OfficeWork();
            BusinessMonthly();
            Budgets();
            GovernmentMonthly();
            WageSetting();
            JobHopping();
            PeopleMonthly();
            Developers();
            Drift();
        }

        /// <summary>Last month's books become "last month", and this month starts at zero.</summary>
        void CloseMonth()
        {
            LastBook = Book.Copy(); Book.Clear();
            LastTrade = Trade.Copy(); Trade.Clear();
            foreach (var h in Households)
            {
                Array.Copy(h.In, h.LastIn, h.In.Length); Array.Copy(h.Out, h.LastOut, h.Out.Length);
                Array.Clear(h.In, 0, h.In.Length); Array.Clear(h.Out, 0, h.Out.Length);
            }
            foreach (var c in People) { c.LastOutings = c.Outings; c.Outings = 0; }
            if (LastImports == null) LastImports = new double[Defs.GoodCount];
            Array.Copy(ImportsByGood, LastImports, LastImports.Length); Array.Clear(ImportsByGood, 0, ImportsByGood.Length);
            if (SpendByGood == null) SpendByGood = new double[Defs.GoodCount];
            if (SpendByService == null) SpendByService = new double[Enum.GetValues(typeof(Service)).Length];
            LastSpendByGood = (double[])SpendByGood.Clone(); LastSpendByService = (double[])SpendByService.Clone();
            Array.Clear(SpendByGood, 0, SpendByGood.Length); Array.Clear(SpendByService, 0, SpendByService.Length);
            if (LastExports == null || LastExports.Length != ExportsByGood.Length) LastExports = new double[ExportsByGood.Length];
            Array.Copy(ExportsByGood, LastExports, LastExports.Length);
            Array.Clear(ExportsByGood, 0, ExportsByGood.Length);
            LastFreightOutside = FreightOutsideMonth;
            for (int i = 0; i < Wants.Length; i++) { LastWants[i] = Wants[i]; Wants[i] = 0; }
            for (int i = 0; i < SpendAway.Length; i++) { SpendAway[i] = SpendAway[i] * 0.6f + (float)AwayMonth[i] * 0.4f; AwayMonth[i] = 0; }
            LastBirths = BirthsMonth; LastDeaths = DeathsMonth; LastArrivals = ArrivalsMonth; LastLeft = LeftMonth;
            BirthsMonth = DeathsMonth = ArrivalsMonth = LeftMonth = 0;
            LastUntreated = UntreatedMonth; UntreatedMonth = 0;
            LastOutOfTown = OutOfTown; OutOfTown = 0;
            LastScrapped = ScrappedMonth; LastCarsSold = CarsSoldMonth; LastRepairsAway = RepairsAwayMonth; LastCarsAway = CarsAwayMonth;
            ScrappedMonth = CarsSoldMonth = RepairsAwayMonth = CarsAwayMonth = 0;
            if (Day > 0) RecordHistory();
        }
        public int LastUntreated;
        [NonSerialized] public double LastOutOfTown;

        /// <summary>World prices wander a little month to month.</summary>
        void Drift()
        {
            for (int i = 0; i < PriceDrift.Length; i++)
            {
                float d = PriceDrift[i] * (1 + Gauss(0, 0.012f));
                d += (1 - d) * 0.06f;
                PriceDrift[i] = Math.Max(0.8f, Math.Min(1.25f, d));
            }
        }

        // ================================================================== statistics
        public int LaborForce() { int n = 0; foreach (var c in People) if (c.InLaborForce || c.WorkId >= 0) n++; return n; }
        public int Employed() { int n = 0; foreach (var c in People) if (c.WorkId >= 0) n++; return n; }
        public int Unemployed() { int n = 0; foreach (var c in People) if (c.InLaborForce && c.WorkId < 0) n++; return n; }
        public float UnemploymentRate() { int lf = LaborForce(); return lf > 0 ? Unemployed() / (float)lf : 0; }
        public int JobSlots() { int n = 0; foreach (var b in Buildings) if (b.Open) foreach (var j in b.Def.Jobs) n += DesiredStaff(b, j); return n; }
        public double AverageWage() { double s = 0; int n = 0; foreach (var c in People) if (c.WorkId >= 0) { s += c.Wage; n++; } return n > 0 ? s / n : 0; }
        public double MedianCash()
        {
            if (Households.Count == 0) return 0;
            var l = Households.Select(h => h.Cash).OrderBy(x => x).ToList();
            return l[l.Count / 2];
        }
        public double AverageRent()
        {
            double s = 0; int n = 0;
            foreach (var b in Buildings) if (b.Built && b.Def.Homes > 0 && b.Owner != Owner.Household) { s += b.Rent; n++; }
            return n > 0 ? s / n : 0;
        }
        /// <summary>Prices in town against normal: groceries, a meal, fuel and clothes.</summary>
        public double PriceLevel()
        {
            double s = 0; int n = 0;
            foreach (var b in Buildings)
            {
                if (!b.Open) continue;
                var d = b.Def;
                if (d.Sells.HasValue && d.Sells != Good.Cars) { s += b.Price / Defs.Retail[(int)d.Sells.Value]; n++; }
                else if (d.Serves != Service.None) { s += b.Price / d.ServicePrice; n++; }
            }
            return n > 0 ? s / n : 1;
        }
        public int Students() { int n = 0; foreach (var c in People) if (c.SchoolId >= 0) n++; return n; }

        void RecordHistory()
        {
            double gdp = 0;
            foreach (var b in Buildings) if (IsBusiness(b)) gdp += b.LastRevenue - b.LastSupplies;
            gdp += LastBook.PublicWages;
            History.Add(new HistoryPoint
            {
                Day = Day, Population = Population, Workers = Employed(), Jobs = JobSlots(), Unemployed = Unemployed(),
                Businesses = Buildings.Count(b => IsBusiness(b) && b.Open), Homes = TotalUnits(), VacantHomes = VacantUnits(), Students = Students(),
                Treasury = Treasury, AvgWage = AverageWage(), MedianCash = MedianCash(), Gdp = gdp, Prices = PriceLevel(), AvgRent = AverageRent(),
                Happiness = AverageHappiness()
            });
            if (History.Count > 480) History.RemoveAt(0);
        }

        // ================================================================== advice for the player
        /// <summary>A few plain-language observations about what the town is short of.</summary>
        public List<string> Advice()
        {
            var l = new List<string>();
            bool zoned = false;
            for (int y = 0; y < Rows && !zoned; y++) for (int x = 0; x < Cols && !zoned; x++) if (ZoneAt(x, y) != Zone.None) zoned = true;
            if (!zoned) l.Add("Run streets off the end of the highway and zone land beside them for homes. Families will move in and drive to work out of town.");
            else if (SettlersShort > 0 && !HasLot(BuildingType.House)) l.Add("Families want to buy houses here but there's no land zoned for houses left by a road. Zone more, along streets.");
            int lf = LaborForce(), commuters = Commuters();
            if (commuters >= 8 && commuters > lf * 0.25f) l.Add(commuters + " people drive to jobs out of town. Zone shops and industry so they can work here, and the town keeps their spending.");
            if (Population >= 60 && !Buildings.Any(b => b.Type == BuildingType.TownHall)) l.Add("There's no town hall yet. Build one where you want the middle of town to be: shops and offices gather round it.");
            int kidsNoSchool = People.Count(c => c.Stage != Stage.None && c.Stage <= Stage.HighSchool && c.SchoolId < 0);
            if (kidsNoSchool > 0)
            {
                bool hs = People.Any(c => c.Stage == Stage.HighSchool && c.SchoolId < 0);
                l.Add(kidsNoSchool + " children have no school place" + (hs && !Buildings.Any(b => b.Type == BuildingType.HighSchool) ? " — teenagers need a high school, or they'll turn 18 without a diploma." : "."));
            }
            if (!Buildings.Any(b => b.Type == BuildingType.College) && Population > 200) l.Add("No community college: nobody can train for a trade or get a degree here.");
            if (LastUntreated > 3) l.Add(LastUntreated + " sick people couldn't see a doctor last month.");
            float un = UnemploymentRate();
            if (un > 0.1f) l.Add("Unemployment is " + Pct(un) + ". Zone industrial or commercial land so firms can open.");
            int open = Openings().Where(o => o.Building.Unfilled >= 4).Sum(o => o.Count);
            if (open > 4) l.Add(open + " jobs have gone unfilled for a month or more.");
            if (VacantUnits() == 0 && !HasLot(BuildingType.House) && !HasLot(BuildingType.Apartments) && SettlersShort == 0) l.Add("Homes are full and there's no residential land by a road left to build on.");
            if (LotShort != null && LotShort.Length > (int)Zone.Apartments && LotShort[(int)Zone.Apartments]) l.Add("People want flats to rent but there's no land zoned for apartments by a road. Zone some: Homes, then Apartments.");
            if (LotShort != null && LotShort[(int)Zone.Commercial]) l.Add("A business wanted to open but found no commercial lot by a road.");
            foreach (var t in ChainTypes)
                if (LastWants != null && LastWants[(int)t] > 0)
                {
                    var d = Defs.Get(t);
                    l.Add(Buildings.Any(b => b.Type == t && b.Open)
                        ? d.Chain + " wants to open another store: it needs a free " + d.W + " x " + d.H + " commercial lot by a road in a part of town its store is far from."
                        : d.Chain + " wants to open here: it needs a free " + d.W + " x " + d.H + " commercial lot by a road.");
                }
            foreach (var t in ChainTypes)
            {
                var o = Defs.Get(t).Office;
                if (o.HasValue && LastWants != null && LastWants[(int)o.Value] > 0)
                    l.Add(Defs.Get(t).Chain + " wants to set up a head office to run its stores: it needs a free 1 x 1 commercial lot by a road that isn't part of one big enough for a store.");
            }
            if (LotShort != null && LotShort[(int)Zone.Industrial]) l.Add("A firm wanted to build but found no industrial land by a road.");
            if (LotShort != null && LotShort.Length > (int)Zone.Farmland && LotShort[(int)Zone.Farmland]) l.Add("Someone wanted to start a farm but there's no farmland by a road. Zone some with the Farms tool.");
            if (NoInvestor != BuildingType.FreightTerminal) l.Add("The town could use " + (Defs.Get(NoInvestor).Name.StartsWith("O") || Defs.Get(NoInvestor).Name.StartsWith("E") || Defs.Get(NoInvestor).Name.StartsWith("A") ? "an " : "a ") + Defs.Get(NoInvestor).Name.ToLower() + ", but no family here has the savings to start one.");
            if (!Buildings.Any(b => b.Type == BuildingType.Realty && b.Open) && VacantUnits() < 3) l.Add("There's no property firm in town to build homes. One will start when a family can afford to and there's a commercial lot.");
            if (Treasury < 0) l.Add("The town is " + Money(-Treasury) + " in debt.");
            if (LastOutOfTown > 15000) l.Add("People spent " + Money(LastOutOfTown) + " out of town last month on things they couldn't buy here.");
            return l;
        }
    }
}
