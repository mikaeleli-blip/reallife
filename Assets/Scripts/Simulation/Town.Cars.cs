using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    public enum CarClass : byte { Budget, Family, Premium }
    public enum CarJob : byte { None, Service, Repair, Prep }

    /// <summary>One car: who owns it (or which dealer has it for sale), how old and worn it is, and whether it's in a garage.</summary>
    [Serializable]
    public class Car
    {
        public int Id;
        public CarClass Class;
        public int HouseholdId = -1;      // its owners (-1: nobody)
        public int LotId = -1;            // the dealership it's for sale at
        public int BuiltDay;              // the day it was new (before the town's first day for older cars)
        public float Km;                  // thousands of kilometres on the clock
        public float Condition = 1f;      // 0..1
        public bool Broken;
        public int ShopId = -1;           // the garage it's in or waiting at (-2: one out of town)
        public CarJob Job;
        public float JobHours;            // work left on it
        public float JobParts;            // parts it still needs taking from the garage's stock
        public float JobFix;              // condition it gets back when it's done
        public double JobBill;            // what the owner pays when it's done
        public int JobQueued;             // the day it was booked in: the queue order
        public int LastService;
        public float KmAtService;
        public int Color;                 // paint, for the view
        public double Cost;               // on a dealer's lot: what the dealer paid for it, counted against the sale
    }

    /// <summary>
    /// Cars. Every car is owned by a family or sits on a dealer's lot. Cars wear with age and the distance they're driven (a
    /// commuter's most), and the more worn, the more often they break down. A broken car goes to a garage, where it waits its
    /// turn and the mechanics' hours and parts; or, if the repair would cost more than the car is worth, the family replaces it
    /// if they can afford to, and does without if they can't. Cars are serviced twice a year by families who can spare it.
    /// Dealers sell new cars they order in and used ones they took in trade, made ready by their mechanic.
    /// </summary>
    public partial class Town
    {
        public List<Car> Cars = new List<Car>();
        public int NextCar = 1;
        [NonSerialized] Dictionary<int, Car> carById;
        [NonSerialized] Dictionary<int, List<Car>> carsByHousehold;
        [NonSerialized] static readonly List<Car> NoCars = new List<Car>();
        [NonSerialized] public int ScrappedMonth, LastScrapped, CarsSoldMonth, LastCarsSold, RepairsAwayMonth, LastRepairsAway, CarsAwayMonth, LastCarsAway;

        // ================================================================== reference
        public static double NewPrice(CarClass c) { return c == CarClass.Budget ? 19000 : c == CarClass.Premium ? 48000 : Balance.CarPrice; }
        public static float FuelUse(CarClass c) { return c == CarClass.Budget ? 0.8f : c == CarClass.Premium ? 1.3f : 1f; }
        static float PartsFactor(CarClass c) { return c == CarClass.Budget ? 0.8f : c == CarClass.Premium ? 1.6f : 1f; }
        public static string ClassName(CarClass c) { return c == CarClass.Budget ? "small car" : c == CarClass.Premium ? "premium SUV" : "family car"; }

        // a breakdown: minor, moderate, major
        static readonly float[] FaultHours = { 3, 8, 20 };
        static readonly float[] FaultParts = { 2.5f, 10, 32 };   // units of parts: $100, $400, $1,280 wholesale
        const float ServiceHours = 2, ServiceParts = 1.5f, PrepHours = 4, PrepParts = 5;
        const double PartsMarkup = 1.35, TowFee = 120;
        public const int ServiceDays = 24;                        // twice a year
        const float ServiceKm = 7.5f;

        public float CarAge(Car c) { return Math.Max(0, (Day - c.BuiltDay) / (float)Balance.DaysPerYear); }
        public int CarYear(Car c) { return StartYear + (int)Math.Floor(c.BuiltDay / (float)Balance.DaysPerYear); }

        /// <summary>What it would fetch: the new price, less a seventh a year, for its condition and mileage.</summary>
        public double CarValue(Car c)
        {
            double v = NewPrice(c.Class) * PriceDrift[(int)Good.Cars] * Math.Pow(0.87, CarAge(c)) * (0.55 + 0.45 * c.Condition) * Math.Max(0.4, 1 - c.Km / 400.0);
            if (c.Broken) v *= 0.8;
            return Math.Max(400, Math.Round(v / 50) * 50);
        }

        // ================================================================== who has what
        void IndexCars()
        {
            if (Cars == null) Cars = new List<Car>();
            carById = new Dictionary<int, Car>();
            carsByHousehold = new Dictionary<int, List<Car>>();
            foreach (var c in Cars)
            {
                carById[c.Id] = c;
                if (c.HouseholdId >= 0) Owned(c.HouseholdId).Add(c);
                if (c.LotId >= 0 && c.Cost <= 0) c.Cost = CarValue(c) * 0.85;
            }
        }

        List<Car> Owned(int household)
        {
            List<Car> l;
            if (!carsByHousehold.TryGetValue(household, out l)) { l = new List<Car>(); carsByHousehold[household] = l; }
            return l;
        }

        public Car GetCar(int id) { Car c; return carById != null && carById.TryGetValue(id, out c) ? c : null; }
        public List<Car> CarsOf(Household h) { List<Car> l; return h != null && carsByHousehold != null && carsByHousehold.TryGetValue(h.Id, out l) ? l : NoCars; }
        public int CarCount(Household h) { return CarsOf(h).Count; }
        public static bool Drivable(Car c) { return !c.Broken && c.ShopId == -1; }
        public int UsableCars(Household h) { int n = 0; foreach (var c in CarsOf(h)) if (Drivable(c)) n++; return n; }
        public bool HasUsableCar(Household h) { foreach (var c in CarsOf(h)) if (Drivable(c)) return true; return false; }
        public IEnumerable<Car> LotOf(Building dealer) { foreach (var c in Cars) if (c.LotId == dealer.Id) yield return c; }
        public IEnumerable<Car> QueueAt(Building garage) { return Cars.Where(c => c.ShopId == garage.Id).OrderBy(c => c.JobQueued).ThenBy(c => c.Id); }

        Car MakeCar(CarClass cls, float ageYears, float condition, float km)
        {
            var c = new Car
            {
                Id = NextCar++, Class = cls, BuiltDay = Day - (int)(ageYears * Balance.DaysPerYear), Km = km,
                Condition = Clamp01(condition), Color = RandInt(0, 11), LastService = Day - RandInt(0, ServiceDays), KmAtService = km
            };
            Cars.Add(c);
            carById[c.Id] = c;
            return c;
        }

        void Own(Car c, Household h)
        {
            Disown(c);
            c.LotId = -1;
            c.HouseholdId = h.Id;
            Owned(h.Id).Add(c);
        }

        void Disown(Car c)
        {
            if (c.HouseholdId >= 0) { List<Car> l; if (carsByHousehold.TryGetValue(c.HouseholdId, out l)) { l.Remove(c); if (l.Count == 0) carsByHousehold.Remove(c.HouseholdId); } }
            c.HouseholdId = -1;
        }

        void Scrap(Car c)
        {
            Disown(c);
            Cars.Remove(c);
            carById.Remove(c.Id);
            ScrappedMonth++;
        }

        /// <summary>A household's cars pass to the one taking over from it (a death, two families moving in together), or leave town with them.</summary>
        void HandOverCars(Household from, Household to)
        {
            foreach (var c in CarsOf(from).ToList())
            {
                if (to != null) Own(c, to);
                else { Disown(c); Cars.Remove(c); carById.Remove(c.Id); }
            }
        }

        /// <summary>What sort of car a family goes for: a premium one if they're well off, a small one if money's tight.</summary>
        CarClass ClassFor(double income, double cash)
        {
            if (income > 9500 || cash > 150000) return Chance(0.6f) ? CarClass.Premium : CarClass.Family;
            if (income < 4500 && cash < 40000) return Chance(0.75f) ? CarClass.Budget : CarClass.Family;
            return Chance(0.7f) ? CarClass.Family : CarClass.Budget;
        }

        /// <summary>A car a family already had when they came: some years old.</summary>
        Car GiveCar(Household h, Edu edu)
        {
            double income = edu >= Edu.Degree ? 9000 : edu == Edu.Trade ? 6000 : edu == Edu.HighSchool ? 5000 : 3800;
            var cls = ClassFor(income, h.Cash);
            float age = Rand() * Rand() * 14;
            var c = MakeCar(cls, age, 1 - age * 0.05f - Rand() * 0.15f, age * (9 + Rand() * 8));
            Own(c, h);
            return c;
        }

        // ================================================================== every day, for each household
        void CarsDaily(Household h, Building home, List<Citizen> ms, float thrift)
        {
            var cars = CarsOf(h);
            if (cars.Count > 0)
            {
                // who drives which car: the longest drives get the cars first
                var drives = new List<float>();
                foreach (var m in ms)
                {
                    if (m.IsChild) continue;
                    if (Commutes(m)) drives.Add(0.35f);
                    else if (m.WorkId >= 0 && Distance(home, GetBuilding(m.WorkId)) > 6) drives.Add(0.12f);
                }
                drives.Sort((a, b) => b.CompareTo(a));
                int i = 0;
                float fuel = 0;
                foreach (var c in cars.ToList())
                {
                    if (!Drivable(c)) continue;
                    float km = i < drives.Count ? drives[i] : 0.05f; i++;
                    c.Km += km;
                    fuel += FuelUse(c.Class) * (km >= 0.3f ? 1.5f : km >= 0.1f ? 1f : 0.45f);
                    // wear: time and distance, more if it's overdue a service
                    float wear = 0.0012f + km * 0.0035f;
                    if (Day - c.LastService > ServiceDays * 1.5f || c.Km - c.KmAtService > ServiceKm * 1.6f) wear *= 1.6f;
                    c.Condition = Math.Max(0, c.Condition - wear);
                    float p = 0.0015f + (1 - c.Condition) * (1 - c.Condition) * 0.02f + (CarAge(c) > 12 ? 0.002f : 0);
                    if (Chance(p)) BreakDown(c, h, ms);
                    else if ((Day - c.LastService >= ServiceDays || c.Km - c.KmAtService >= ServiceKm) && Chance(0.35f)) BookService(c, h, home, thrift);
                }
                h.Fuel = Math.Max(0, h.Fuel - fuel);
                int usable = UsableCars(h);
                if (usable > 0 && h.Fuel < usable * 1.2f) { float got = Shop(h, home, Good.Fuel, usable * 3f - h.Fuel, ms); h.Fuel += got; }
                // broken and not yet seen to: decide what to do (again)
                foreach (var c in cars.ToList()) if (c.Broken && c.ShopId == -1 && Chance(0.5f)) RepairOrReplace(c, h, home, ms, thrift);
                // a worn-out car is replaced when there's money; the well-off trade up now and then
                var worst = cars.Where(c => c.ShopId == -1).OrderBy(c => c.Condition).FirstOrDefault();
                if (worst != null && (worst.Condition < 0.12f || CarAge(worst) > 16) && Chance(0.15f)) BuyCar(h, home, ms, thrift, worst);
                else if (worst != null && CarAge(worst) > 7 && (h.Cash > 90000 || MonthlyIncome(h) > 10000) && Chance(0.004f)) BuyCar(h, home, ms, thrift, worst);
                // a second car when two people drive a long way to work
                // (unless the bus gets the other one there and money's not so loose)
                if (cars.Count == 1 && drives.Count >= 2 && MonthlyIncome(h) > 6500 && Chance(0.01f) && !(MonthlyIncome(h) < 8000 && BusCovers(h, home))) BuyCar(h, home, ms, thrift, null);
            }
            else if (Chance(0.06f))
            {
                // a family that gets everywhere by bus and hasn't much put by does without
                if (MonthlyIncome(h) < 7500 && h.Cash < 50000 && BusCovers(h, home)) return;
                // a car when there's a long way to work, or kids, and it's affordable
                bool farJob = ms.Any(c => c.WorkId >= 0 && Distance(home, GetBuilding(c.WorkId)) > 14);
                bool kids = ms.Any(c => c.IsChild);
                bool looking = ms.Any(c => c.InLaborForce && c.WorkId < 0 && c.JoblessDays > 4);
                if ((farJob || kids || looking || Chance(0.3f)) && ms.Any(c => c.WorkId >= 0)) BuyCar(h, home, ms, thrift, null);
            }
        }

        void BreakDown(Car c, Household h, List<Citizen> ms)
        {
            c.Broken = true;
            float r = Rand(), major = 0.1f + (1 - c.Condition) * 0.3f;
            int sev = r < major ? 2 : r < major + 0.35f ? 1 : 0;
            c.Job = CarJob.Repair;
            c.JobHours = FaultHours[sev];
            c.JobParts = FaultParts[sev] * PartsFactor(c.Class);
            c.JobFix = FaultHours[sev] * 0.011f;
            var who = ms.FirstOrDefault(m => !m.IsChild);
            if (who != null) Note(who, sev == 2 ? "The car broke down badly." : "The car broke down.");
        }

        // ================================================================== garages
        /// <summary>Hours of work a garage's staff can do in a day.</summary>
        float GarageHours(Building g) { return Math.Max(1, g.Employees.Count) * g.Def.Output; }
        float Booked(Building g) { float s = 0; foreach (var c in Cars) if (c.ShopId == g.Id) s += c.JobHours; return s; }

        double Quote(Building g, Car c, float hours, float parts)
        {
            double rate = g != null ? g.Price : 110;
            double partsPrice = parts * Defs.Wholesale[(int)Good.Parts] * (g != null ? PartsMarkup : 1.5);
            return Math.Round(hours * rate + partsPrice + (c.Broken ? (g != null ? TowFee : TowFee * 2.5) : 0));
        }

        /// <summary>The garage in town a family would take it to: the cheapest that can fit it in within a few days (null: none can).
        /// A broken car is towed, so how far doesn't matter; for a service they have to get there.</summary>
        Building PickGarage(Car c, Household h, Building home, out Building full)
        {
            Building best = null; double bs = double.MaxValue;
            full = null;
            foreach (var g in Buildings)
            {
                if (g.Type != BuildingType.AutoRepair || !g.Open || !Connected(g)) continue;
                if (!c.Broken && !CanGo(h, home, g)) continue;
                float wait = Booked(g) / GarageHours(g);
                if (wait > 3) { full = g; continue; }   // booked up: they'll try elsewhere
                double sc = g.Price * (1 + wait * 0.15) * (1 + Distance(home, g) / 120f);
                if (sc < bs) { bs = sc; best = g; }
            }
            return best;
        }

        void BookIn(Car c, Building g, CarJob job, double bill)
        {
            c.ShopId = g != null ? g.Id : -2;
            c.Job = job; c.JobBill = bill; c.JobQueued = Day;
            if (g == null) { c.JobHours = job == CarJob.Service ? 1 : 2; c.JobParts = 0; }   // out of town: days away
        }

        void BookService(Car c, Household h, Building home, float thrift)
        {
            float parts = ServiceParts * PartsFactor(c.Class);
            Building full;
            var g = PickGarage(c, h, home, out full);
            if (g == null && !CanDriveOutOfTown(h, home)) return;
            double bill = Quote(g, c, ServiceHours, parts);
            if (!CanSpare(h, bill, thrift)) return;   // they put it off
            if (g == null)
            {
                if (full != null) full.Lost++;
                if (!Chance(0.5f)) return;
                PayAway(h, bill);
            }
            c.JobHours = ServiceHours; c.JobParts = parts; c.JobFix = 0.03f;
            BookIn(c, g, CarJob.Service, bill);
        }

        /// <summary>Paid at a garage out of town, up front.</summary>
        void PayAway(Household h, double bill)
        {
            HouseholdPays(h, bill, HouseholdBook.Transport);
            Trade.Imports += bill; OutOfTown += bill; AwayMonth[(int)BuildingType.AutoRepair] += bill;
            RepairsAwayMonth++;
        }

        /// <summary>A car's broken down: fix it, or replace it if the repair costs more than it's worth. If they can't afford either
        /// it sits there until they can.</summary>
        void RepairOrReplace(Car c, Household h, Building home, List<Citizen> ms, float thrift)
        {
            Building full;
            var g = PickGarage(c, h, home, out full);
            double bill = Quote(g, c, c.JobHours, c.JobParts);
            double value = CarValue(c);
            bool notWorthIt = bill > value * 0.55 || CarAge(c) > 15;
            // not worth fixing, and the bus will do: a family short of money gives the car up
            if (notWorthIt && CarCount(h) == 1 && MonthlyIncome(h) < 6000 && BusCovers(h, home) && Chance(0.5f))
            {
                SellOrScrap(c, h, null, false);
                var gave = ms.FirstOrDefault(m => !m.IsChild);
                if (gave != null) Note(gave, "Gave up the car: the bus does.");
                return;
            }
            if (notWorthIt && BuyCar(h, home, ms, thrift, c)) return;
            // a family with another car may do without this one rather than pay
            if (notWorthIt && CarCount(h) > 1 && h.Cash < bill * 3) { SellOrScrap(c, h, null, false); return; }
            double spare = h.Cash - Essentials(h) * 0.5;
            if (spare < bill)
            {
                // short of cash: the bank lends for the repair to a family with the income to pay it back; if not, it waits
                double loanable;
                CarBudget(h, thrift, 0, out loanable);
                double borrow = Math.Round(bill - Math.Max(0, spare));
                if (borrow > Math.Min(loanable * 0.5, 8000)) return;
                NewLoan(LoanKind.Car, h, null, borrow, Balance.CarRate, 18);
                HouseholdEarns(h, borrow, HouseholdBook.Loans);
                var who = ms.FirstOrDefault(m => !m.IsChild);
                if (who != null) Note(who, "Took a loan to fix the car.");
            }
            if (g == null) { if (full != null) full.Lost++; PayAway(h, bill); }
            BookIn(c, g, CarJob.Repair, bill);
        }

        /// <summary>Each garage works through its queue in the order cars came in, as far as its mechanics' hours and its parts go.</summary>
        void GaragesDaily()
        {
            // a garage that's shut sends its cars home to be booked in elsewhere
            foreach (var c in Cars)
            {
                if (c.ShopId < 0) continue;
                var at = GetBuilding(c.ShopId);
                if (at != null && at.Open) continue;
                c.ShopId = -1;
                if (c.Job == CarJob.Service) c.Job = CarJob.None;
            }
            foreach (var g in Buildings)
            {
                if (g.Type != BuildingType.AutoRepair || !g.Open) continue;
                float hours = Capacity(g);
                foreach (var c in QueueAt(g).ToList())
                {
                    if (hours <= 0.01f) break;
                    if (c.JobParts > 0)
                    {
                        if (g.Stock[(int)Good.Parts] < c.JobParts) continue;   // waiting for parts
                        g.Stock[(int)Good.Parts] -= c.JobParts; c.JobParts = 0;
                    }
                    float w = Math.Min(hours, c.JobHours);
                    c.JobHours -= w; hours -= w; g.CapUsed += w;
                    if (c.JobHours <= 0.01f) FinishJob(c, g);
                }
            }
            // and the cars at garages out of town come back after a day or two
            foreach (var c in Cars)
            {
                if (c.ShopId != -2) continue;
                c.JobHours -= 1;
                if (c.JobHours <= 0) FinishJob(c, null);
            }
        }

        void FinishJob(Car c, Building g)
        {
            // paid on collection (a garage out of town was paid up front)
            var h = GetHousehold(c.HouseholdId);
            if (g != null && h != null)
            {
                double bill = c.JobBill, tax = bill * Policy.SalesTax;
                HouseholdPays(h, bill + tax, HouseholdBook.Transport);
                Treasury += tax; Book.SalesTax += tax;
                g.Cash += bill; g.Revenue += bill; g.Customers++; g.ServedToday++;
            }
            c.Broken = false;
            c.Condition = Math.Min(0.97f, c.Condition + c.JobFix);
            if (c.Job == CarJob.Service) { c.LastService = Day; c.KmAtService = c.Km; }
            c.ShopId = -1; c.Job = CarJob.None; c.JobHours = 0; c.JobParts = 0; c.JobFix = 0; c.JobBill = 0;
        }

        // ================================================================== buying and selling
        /// <summary>The most a family could pay for a car: what they can spare, plus a car loan the bank would approve.</summary>
        double CarBudget(Household h, float thrift, double tradeIn, out double loanable)
        {
            double cash = Math.Max(0, h.Cash - Essentials(h) * (0.8 + thrift * 1.6));
            double income = MonthlyIncome(h);
            double pay = Math.Min(income * 0.14, income * 0.45 - DebtPayments(h));
            loanable = 0;
            if (pay > 0 && income > 0 && !Loans.Any(l => l.HouseholdId == h.Id && l.Missed > 0))
            {
                double r = Balance.CarRate / 12;
                loanable = pay * (1 - Math.Pow(1 + r, -Balance.CarMonths)) / r;
            }
            // a loan needs 15% down
            double withLoan = Math.Min(cash / 0.15, cash + loanable);
            return Math.Max(cash, withLoan) + tradeIn;
        }

        /// <summary>A family buys a car, trading in (or getting rid of) the one it replaces: new or used, at a dealer in town if
        /// there's one with something that suits, otherwise out of town. False if they can't afford one.</summary>
        bool BuyCar(Household h, Building home, List<Citizen> ms, float thrift, Car replacing)
        {
            double income = MonthlyIncome(h);
            double tradeValue = replacing != null ? CarValue(replacing) : 0;
            double loanable;
            double budget = CarBudget(h, thrift, tradeValue * 0.7, out loanable);
            var want = ClassFor(income, h.Cash);
            bool wantNew = income > 8000 || h.Cash > 80000;
            if (budget < 3000) return false;
            // at a dealer in town
            Building dealer = null; Car pick = null; double price = 0;
            foreach (var d in Buildings)
            {
                if (d.Type != BuildingType.CarDealer || !d.Open || !CanGo(h, home, d)) continue;
                if (Capacity(d) <= d.Sold) { d.Lost++; continue; }   // nobody free to sell them one today
                double factor = DealerMarkup(d);
                foreach (var c in LotOf(d))
                {
                    if (c.Job == CarJob.Prep) continue;
                    double p = AskingPrice(d, c, factor);
                    if (p * (1 + Policy.SalesTax) > budget) continue;
                    bool isNew = CarAge(c) < 0.5f && c.Km < 1;
                    // the closest match to what they're after: the right sort, new or used as they prefer, the most car for the money
                    double score = (c.Class == want ? 0 : 1) * 3 + (isNew == wantNew ? 0 : 1) + p / 60000.0 - c.Condition;
                    if (pick == null || score < ScoreOf(pick, dealer, want, wantNew)) { pick = c; dealer = d; price = p; }
                }
            }
            if (pick == null)
            {
                // nobody here has one that fits: the next town over, new or a few years old
                if (!CanDriveOutOfTown(h, home) && !BusServes(home) && replacing == null) return false;
                bool used = !wantNew || NewPrice(want) * 1.05 * (1 + Policy.SalesTax) > budget;
                var cls = want;
                if (used)
                {
                    // a few years old of the sort they want; failing that a small one; failing that an old banger
                    for (int tryN = 0; tryN < 3 && pick == null; tryN++)
                    {
                        if (tryN == 1 && cls == CarClass.Budget) continue;
                        var k = tryN == 0 ? cls : CarClass.Budget;
                        float age = tryN < 2 ? 2 + Rand() * 6 : 9 + Rand() * 5;
                        var c = MakeCar(k, age, Math.Max(0.45f, 0.78f - age * 0.03f), age * (10 + Rand() * 5));
                        price = CarValue(c) * 1.2;
                        if (price * (1 + Policy.SalesTax) <= budget) pick = c;
                        else { Cars.Remove(c); carById.Remove(c.Id); }
                    }
                    if (pick == null) return false;
                }
                else { pick = MakeCar(cls, 0, 1, 0.02f); price = NewPrice(cls) * 1.05 * PriceDrift[(int)Good.Cars]; }
            }
            // pay: the trade-in first, then cash, then a loan for the rest
            double tax = price * Policy.SalesTax, total = price + tax;
            double tradeIn = replacing != null ? TradeInOffer(dealer, replacing) : 0;
            double owe = total - tradeIn;
            double cash = Math.Max(0, h.Cash - Essentials(h) * (0.8 + thrift * 1.6));
            double loan = 0;
            if (owe > cash)
            {
                loan = owe - Math.Max(cash, owe * 0.15);
                if (h.Cash < owe - loan || loan > loanable + 1) { if (dealer == null) { Cars.Remove(pick); carById.Remove(pick.Id); } return false; }
            }
            if (replacing != null) SellOrScrap(replacing, h, dealer, true);
            HouseholdPays(h, owe - loan, HouseholdBook.Transport);
            if (loan > 0) { NewLoan(LoanKind.Car, h, null, loan, Balance.CarRate, Balance.CarMonths); HouseholdEarns(h, loan, HouseholdBook.Loans); HouseholdPays(h, loan, HouseholdBook.Transport); }
            Treasury += tax; Book.SalesTax += tax;
            if (dealer == null) { Trade.Imports += price; OutOfTown += price; AwayMonth[(int)BuildingType.CarDealer] += price; CarsAwayMonth++; }
            else
            {
                dealer.Cash += price; dealer.Revenue += price; dealer.Supplies += pick.Cost; dealer.Customers++; dealer.Sold += 1; dealer.CapUsed += 1;
                // and the bank pays the dealer a little for a loan it arranged
                if (loan > 0) { double fee = Math.Round(loan * 0.02); BankCapital -= fee; dealer.Cash += fee; dealer.Revenue += fee; }
                Errand(ms, dealer);
            }
            bool isNewCar = CarAge(pick) < 0.5f && pick.Km < 1;
            Own(pick, h);
            pick.LastService = Day; pick.KmAtService = pick.Km;
            h.Fuel = Math.Max(h.Fuel, 1);
            CarsSoldMonth++;
            var buyer = ms.FirstOrDefault(m => !m.IsChild);
            string what = (isNewCar ? "a new " : "a used ") + ClassName(pick.Class);
            if (buyer != null) Note(buyer, "Bought " + what + (loan > 0 ? " on a car loan" : "") + ".");
            if (Chance(0.2f) && buyer != null) AddLog("The " + h.Name + " family bought " + what + (dealer == null ? " in the next town over." : " at " + Ref(dealer) + "."), LogKind.Info, buyer.Id, true);
            return true;
        }

        double ScoreOf(Car c, Building d, CarClass want, bool wantNew)
        {
            double p = AskingPrice(d, c, DealerMarkup(d));
            bool isNew = CarAge(c) < 0.5f && c.Km < 1;
            return (c.Class == want ? 0 : 1) * 3 + (isNew == wantNew ? 0 : 1) + p / 60000.0 - c.Condition;
        }

        /// <summary>How far a dealer's prices sit above list (it moves them a little with demand, like any shop).</summary>
        public static double DealerMarkup(Building d) { return Math.Max(0.98, Math.Min(1.12, d.Price / Defs.Retail[(int)Good.Cars])); }

        /// <summary>A new car at list price; a used one at what it's worth plus the dealer's margin.</summary>
        public double AskingPrice(Building dealer, Car c, double factor)
        {
            bool isNew = CarAge(c) < 0.5f && c.Km < 1;
            double p = isNew ? NewPrice(c.Class) * PriceDrift[(int)Good.Cars] : CarValue(c) * 1.15;
            return Math.Round(p * factor / 50) * 50;
        }

        /// <summary>What a dealer here gives for an old car: three-quarters of its worth, if it's one it can sell on.</summary>
        double TradeInOffer(Building dealer, Car c)
        {
            if (dealer != null && c.Condition > 0.2f && CarAge(c) < 15 && LotOf(dealer).Count() < dealer.Def.StockCap) return Math.Round(CarValue(c) * 0.75);
            return c.Condition > 0.2f && CarAge(c) < 15 ? Math.Round(CarValue(c) * 0.6) : 300;
        }

        /// <summary>The old car goes: onto the dealer's lot as a trade-in, sold out of town, or to the scrapyard. Credited: the
        /// money's already come off the price of the car they're buying.</summary>
        void SellOrScrap(Car c, Household h, Building dealer, bool credited)
        {
            double offer = TradeInOffer(dealer, c);
            bool keep = dealer != null && c.Condition > 0.2f && CarAge(c) < 15 && LotOf(dealer).Count() < dealer.Def.StockCap;
            c.ShopId = -1;
            if (!credited) HouseholdEarns(h, offer, HouseholdBook.Business);
            if (keep)
            {
                // the dealer pays for it and gets it ready to sell
                dealer.Cash -= offer; c.Cost = offer;
                Disown(c);
                c.LotId = dealer.Id; c.Broken = false;
                c.Job = CarJob.Prep; c.JobHours = PrepHours; c.JobParts = PrepParts * PartsFactor(c.Class); c.JobFix = 0.1f;
                return;
            }
            // sold out of town, or scrapped: the money comes in from outside
            Trade.Exports += offer;
            Scrap(c);
        }

        // ================================================================== dealers
        /// <summary>Dealers keep a few new cars on the lot, ordering in what sells, and their mechanic readies the trade-ins.</summary>
        void DealersDaily()
        {
            foreach (var d in Buildings)
            {
                if (d.Type != BuildingType.CarDealer) continue;
                var lot = LotOf(d).ToList();
                if (!d.Open || !Connected(d))
                {
                    // a shut dealer's cars go back to the makers or to auction
                    if (d.Closed) foreach (var c in lot) { BankCapital += CarValue(c) * 0.6; Trade.Exports += CarValue(c) * 0.6; Cars.Remove(c); carById.Remove(c.Id); }
                    d.Stock[(int)Good.Cars] = d.Closed ? 0 : lot.Count;
                    continue;
                }
                // the mechanic gets the trade-ins ready: one a day each
                int mech = 0; foreach (var id in d.Employees) { var e = GetCitizen(id); if (e != null && e.AtWork && e.Role == Role.Mechanic) mech++; }
                foreach (var c in lot.Where(c => c.Job == CarJob.Prep).Take(Math.Max(1, mech)))
                {
                    double parts = c.JobParts * ImportPrice(Good.Parts);
                    d.Cash -= parts; c.Cost += parts; Trade.Imports += parts;
                    c.Condition = Math.Min(0.95f, c.Condition + c.JobFix);
                    c.Job = CarJob.None; c.JobHours = 0; c.JobParts = 0; c.JobFix = 0;
                }
                // new stock: a few of what's been selling, if there's money for it
                int fresh = lot.Count(c => CarAge(c) < 0.5f && c.Km < 1);
                int target = 3 + Math.Min(4, d.LastCustomers / 3);
                while (fresh < target && lot.Count < d.Def.StockCap)
                {
                    float r = Rand();
                    var cls = r < 0.3f ? CarClass.Budget : r < 0.8f ? CarClass.Family : CarClass.Premium;
                    double cost = NewPrice(cls) * PriceDrift[(int)Good.Cars] * 0.86;
                    double fee = Freight(cost, -1, d.Id, Good.Cars);
                    if (Spendable(d) < cost + fee) break;
                    d.Cash -= cost + fee;
                    Trade.Imports += cost; ImportsByGood[(int)Good.Cars] += cost;
                    var c = MakeCar(cls, 0, 1, 0.01f);
                    c.LotId = d.Id; c.Cost = cost + fee;
                    lot.Add(c); fresh++;
                }
                d.Stock[(int)Good.Cars] = lot.Count;
            }
        }

        /// <summary>The bank takes back a car whose loan went unpaid (the newest) and sells it: to a dealer here, or at auction.
        /// Returns what it got.</summary>
        double Repossess(Household h)
        {
            var c = CarsOf(h).OrderBy(x => CarAge(x)).FirstOrDefault();
            if (c == null) return 0;
            AddLog("The bank repossessed the " + h.Name + " family's " + ClassName(c.Class) + ".", LogKind.Bad);
            double v = CarValue(c);
            c.ShopId = -1;
            var dealer = Buildings.FirstOrDefault(b => b.Type == BuildingType.CarDealer && b.Open && LotOf(b).Count() < b.Def.StockCap);
            if (dealer != null)
            {
                v *= 0.7;
                dealer.Cash -= v; c.Cost = v;
                Disown(c);
                c.LotId = dealer.Id; c.Broken = false;
                c.Job = CarJob.Prep; c.JobHours = PrepHours; c.JobParts = PrepParts * PartsFactor(c.Class); c.JobFix = 0.1f;
                return v;
            }
            v *= 0.6;
            Trade.Exports += v;
            Scrap(c);
            return v;
        }

        /// <summary>What one customer is worth in a business's price units: a garage charges by the hour, and a job is a few.</summary>
        static double TicketSize(BuildingType t) { return t == BuildingType.AutoRepair ? 5 : 1; }

        // ================================================================== old saves
        /// <summary>Saves from before cars were kept one by one: a family's car count and its wear become cars, and a dealer's
        /// stock becomes new cars on its lot.</summary>
        void MigrateCars()
        {
            IndexCars();
            foreach (var h in Households)
            {
                for (int i = 0; i < h.Cars; i++)
                {
                    var cls = ClassFor(MonthlyIncome(h), h.Cash);
                    float age = Math.Max(0, (1 - h.CarWear) * 12 + Gauss(0, 1.5f));
                    var c = MakeCar(cls, age, h.CarWear + Gauss(0, 0.05f), age * (10 + Rand() * 6));
                    if (i == 0 && h.CarBroken) { c.Broken = true; c.Job = CarJob.Repair; c.JobHours = FaultHours[1]; c.JobParts = FaultParts[1] * PartsFactor(cls); c.JobFix = FaultHours[1] * 0.011f; }
                    Own(c, h);
                }
                h.Cars = 0; h.CarBroken = false;
            }
            foreach (var d in Buildings)
            {
                if (d.Type != BuildingType.CarDealer || d.Closed) continue;
                int n = (int)d.Stock[(int)Good.Cars];
                for (int i = 0; i < n; i++)
                {
                    float r = Rand();
                    var c = MakeCar(r < 0.3f ? CarClass.Budget : r < 0.8f ? CarClass.Family : CarClass.Premium, 0, 1, 0.01f);
                    c.LotId = d.Id; c.Cost = NewPrice(c.Class) * 0.9;
                }
            }
        }
    }
}
