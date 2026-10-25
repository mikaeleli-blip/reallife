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
            Attendance();
            ReadyTrucks();
            ShopsOpen();
            Produce();
            Restock();
            ConstructionDaily();
            HouseholdsDaily();
            Exports();
            if (DayEnded != null) DayEnded(Day);
        }

        void Monthly()
        {
            CloseMonth();
            Payroll();
            HousingMonthly();
            LoansMonthly();
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
            Array.Clear(ExportsByGood, 0, ExportsByGood.Length);
            LastFreightOutside = FreightOutsideMonth;
            for (int i = 0; i < Wants.Length; i++) { LastWants[i] = Wants[i]; Wants[i] = 0; }
            LastBirths = BirthsMonth; LastDeaths = DeathsMonth; LastArrivals = ArrivalsMonth; LastLeft = LeftMonth;
            BirthsMonth = DeathsMonth = ArrivalsMonth = LeftMonth = 0;
            LastUntreated = UntreatedMonth; UntreatedMonth = 0;
            LastOutOfTown = OutOfTown; OutOfTown = 0;
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
            if (VacantUnits() == 0 && ZonedFree(Zone.Residential) < 4) l.Add("Homes are full and there's no residential land left to build on.");
            if (LotShort != null && LotShort[(int)Zone.Commercial]) l.Add("A business wanted to open but found no commercial lot by a road.");
            if (LotShort != null && LotShort[(int)Zone.Industrial]) l.Add("A firm wanted to build but found no industrial land by a road.");
            if (Treasury < 0) l.Add("The town is " + Money(-Treasury) + " in debt.");
            if (LastOutOfTown > 15000) l.Add("People spent " + Money(LastOutOfTown) + " out of town last month on things they couldn't buy here.");
            return l;
        }
    }
}
