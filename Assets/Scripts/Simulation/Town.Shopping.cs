using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// Households spending money. Food every week; clothes as they wear out; a new TV or sofa now and then when there's money
    /// to spare; fuel, repairs and eventually a new car; and going out — a meal, a coffee, a drink, the gym, a haircut.
    /// People shop where it's cheapest for the trip; a shop out of stock or too busy loses the sale, and a sale nobody in town
    /// could make is remembered as demand for a new business. With a car you can drive to the next town, at a price.
    /// </summary>
    public partial class Town
    {
        /// <summary>What a household spends in a usual month on things it can't skip.</summary>
        public double Essentials(Household h)
        {
            double e = h.LastOut[HouseholdBook.Housing] + h.LastOut[HouseholdBook.Debt] + h.LastOut[HouseholdBook.Utilities] + h.LastOut[HouseholdBook.School];
            e += FoodNeed(h) * Defs.Retail[(int)Good.Groceries] * Balance.DaysPerMonth;
            return Math.Max(800, e);
        }

        /// <summary>Would spending this leave them a cushion of a month or two of essentials?</summary>
        bool CanSpare(Household h, double amount, float thrift)
        {
            return h.Cash - amount > Essentials(h) * (0.8 + thrift * 1.6);
        }

        float Thrift(Household h)
        {
            float t = 0; int n = 0;
            foreach (var c in Members(h)) if (!c.IsChild) { t += c.Thrift; n++; }
            return n > 0 ? t / n : 0.5f;
        }

        bool CanGo(Household h, Building home, Building shop)
        {
            if (!Connected(shop)) return false;
            if (Distance(home, shop) <= Balance.WalkTiles) return true;
            if (HasUsableCar(h) && h.Fuel > 0) return true;
            return BusConnects(home, shop);
        }

        bool CanDriveOutOfTown(Household h, Building home) { return HasUsableCar(h) && h.Fuel > 0.3f && Connected(home); }

        static BuildingType StoreFor(Good g)
        {
            switch (g)
            {
                case Good.Groceries: return BuildingType.GroceryStore;
                case Good.Clothes: return BuildingType.ClothingStore;
                case Good.Electronics: return BuildingType.ElectronicsStore;
                case Good.Furniture: return BuildingType.FurnitureStore;
                case Good.Fuel: return BuildingType.GasStation;
                default: return BuildingType.CarDealer;
            }
        }

        // ================================================================== every day
        void ShopsOpen()
        {
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (!b.Open || !IsBusiness(b)) continue;
                if (d.Sells.HasValue || d.Serves != Service.None) b.CapTotal += Capacity(b);
            }
        }

        void HouseholdsDaily()
        {
            var order = Households.ToList(); Shuffle(order);
            foreach (var h in order)
            {
                var home = HomeOf(h);
                var ms = Members(h);
                if (ms.Count == 0) continue;
                foreach (var c in ms) { c.ErrandId = -1; c.LeisureId = -1; }
                if (home == null) continue;
                float thrift = Thrift(h);
                // food
                float need = FoodNeed(h);
                h.Pantry -= need;
                if (h.Pantry < 0)
                {
                    h.Pantry = 0;
                    foreach (var c in ms) { c.Health -= 2.5f; c.Happiness -= 3; }
                }
                if (h.Pantry < need * 1.5f) Shop(h, home, Good.Groceries, need * 3.2f - h.Pantry, ms);
                // clothes wear out
                h.Clothes = Math.Max(0, h.Clothes - 0.034f);
                if (h.Clothes < 0.55f)
                {
                    float want = (0.92f - h.Clothes) * 6f * ms.Count;
                    double cost = want * Defs.Retail[(int)Good.Clothes];
                    if (h.Clothes < 0.25f || h.FunMoney > cost * 0.5 || CanSpare(h, cost, thrift))
                    {
                        double before = h.Cash;
                        float got = Shop(h, home, Good.Clothes, want, ms);
                        h.FunMoney -= Math.Max(0, before - h.Cash);
                        h.Clothes = Math.Min(1f, h.Clothes + got / (6f * ms.Count));
                    }
                }
                // durables
                h.Electronics = Math.Max(0, h.Electronics - 0.008f);
                h.Furniture = Math.Max(0, h.Furniture - 0.005f);
                Durable(h, home, ms, Good.Electronics, ref h.Electronics, 0.45f, thrift);
                Durable(h, home, ms, Good.Furniture, ref h.Furniture, 0.4f, thrift);
                CarsDaily(h, home, ms, thrift);
                GoingOut(h, home, ms, thrift);
                Healthcare(h, home, ms);
            }
        }

        /// <summary>A new TV, laptop or sofa when the old one's worn and there's money for it.</summary>
        void Durable(Household h, Building home, List<Citizen> ms, Good g, ref float condition, float gain, float thrift)
        {
            if (condition > 0.4f || !Chance(0.3f)) return;
            double price = Defs.Retail[(int)g];
            if (!(h.FunMoney > price * 0.6 || CanSpare(h, price * 2, thrift))) return;
            double before = h.Cash;
            if (Shop(h, home, g, 1, ms) > 0.5f) condition = Math.Min(1, condition + gain);
            h.FunMoney -= Math.Max(0, before - h.Cash);
        }

        /// <summary>Buys up to qty of a good at the shops a household can get to; returns how much it got.</summary>
        float Shop(Household h, Building home, Good g, float qty, List<Citizen> ms)
        {
            var type = StoreFor(g);
            float got = 0;
            bool anyStore = false;
            Building nearestShort = null;
            var shops = Buildings.Where(b => b.Type == type && b.Open && CanGo(h, home, b)).OrderBy(b => b.Price * (1 + Distance(home, b) / 90f)).ToList();
            foreach (var b in shops)
            {
                anyStore = true;
                if (got >= qty - 0.01f) break;
                float cap = Capacity(b) - b.Sold;
                float avail = Math.Min(b.Stock[(int)g], cap);
                if (avail <= 0.01f) { if (nearestShort == null) nearestShort = b; continue; }
                double unit = b.Price * (1 + Policy.SalesTax);
                float q = Math.Min(qty - got, avail);
                if (g == Good.Groceries) q = (float)Math.Min(q, Math.Max(0, h.Cash) / unit);
                else q = (float)Math.Min(q, Math.Max(0, h.Cash - 200) / unit);
                if (g == Good.Electronics || g == Good.Furniture || g == Good.Cars) q = (float)Math.Floor(q);
                if (q <= 0.01f) break;
                double value = q * b.Price, tax = value * Policy.SalesTax;
                HouseholdPays(h, value + tax, LineFor(g));
                b.Cash += value; b.Revenue += value; b.Customers++; b.Sold += q; b.CapUsed += q;
                Treasury += tax; Book.SalesTax += tax;
                b.Stock[(int)g] -= q; got += q;
                Errand(ms, b);
            }
            if (got < qty - 0.01f)
            {
                if (nearestShort != null) nearestShort.Lost++;
                if (!anyStore) Wants[(int)type] += g == Good.Groceries ? 1 : 0.6f;
                // drive to the next town for the rest
                if (!anyStore || nearestShort != null)
                {
                    // on the reserve you can always make it to a gas station on the highway
                    bool canGo = CanDriveOutOfTown(h, home) || (g == Good.Fuel && HasUsableCar(h) && Connected(home));
                    if (canGo && (g == Good.Groceries || g == Good.Fuel || Chance(0.5f)))
                    {
                        double unit = Defs.Retail[(int)g] * 1.12 * PriceDrift[(int)g];
                        float q = (float)Math.Min(qty - got, Math.Max(0, h.Cash - 100) / unit);
                        if (g == Good.Electronics || g == Good.Furniture || g == Good.Cars) q = (float)Math.Floor(q);
                        if (q > 0.01f) { double v = q * unit; HouseholdPays(h, v, LineFor(g)); Trade.Imports += v; OutOfTown += v; AwayMonth[(int)type] += v; got += q; h.Fuel -= 0.2f; }
                    }
                }
            }
            if (g == Good.Groceries) h.Pantry += got;
            return got;
        }
        [NonSerialized] public double OutOfTown;

        static int LineFor(Good g)
        {
            switch (g)
            {
                case Good.Groceries: return HouseholdBook.Food;
                case Good.Fuel: case Good.Cars: return HouseholdBook.Transport;
                default: return HouseholdBook.Goods;
            }
        }

        void Errand(List<Citizen> ms, Building b)
        {
            // whoever's free goes: someone not at work, or a worker on the way home
            Citizen who = ms.FirstOrDefault(c => !c.IsChild && c.ErrandId < 0 && c.WorkId < 0) ?? ms.FirstOrDefault(c => !c.IsChild && c.ErrandId < 0);
            if (who != null) who.ErrandId = b.Id;
        }

        // ================================================================== the month's spending money
        /// <summary>After the bills: what's left of the month's income, less what they put by, is money to enjoy. Families with
        /// a lot saved spend a little of it too. Some of it goes online or on trips out of town.</summary>
        void Budgets()
        {
            foreach (var h in Households)
            {
                var ms = Members(h);
                double wages = h.In[HouseholdBook.Wages];
                // retirement saving, invested out of town
                double pension = wages * 0.04;
                if (pension > 0 && h.Cash > pension) { HouseholdPays(h, pension, HouseholdBook.Savings); Trade.Savings += pension; }
                double income = 0; for (int i = 0; i < HouseholdBook.Count; i++) income += h.In[i];
                income -= h.In[HouseholdBook.Loans] + h.Out[HouseholdBook.Taxes] + h.Out[HouseholdBook.Savings];
                double fuel = CarCount(h) * Balance.DaysPerMonth * Defs.Retail[(int)Good.Fuel] * 0.8;
                double bills = h.Out[HouseholdBook.Housing] + h.Out[HouseholdBook.Debt] + h.Out[HouseholdBook.Utilities] + h.Out[HouseholdBook.School];
                double essentials = bills + FoodNeed(h) * Defs.Retail[(int)Good.Groceries] * Balance.DaysPerMonth + fuel;
                float thrift = Thrift(h);
                double save = income * (0.03 + thrift * 0.14);
                double spare = income - essentials - save;
                double fun = 0;
                if (h.Cash > essentials * 0.5)
                {
                    fun = Math.Max(0, spare) * 0.6 + Math.Max(0, h.Cash - essentials * 6) * 0.008;
                    fun = Math.Min(fun, 20000);
                }
                double online = fun * 0.25;   // online shopping, trips, streaming
                if (online > 0) { HouseholdPays(h, online, HouseholdBook.Goods); Trade.Imports += online; OutOfTown += online; }
                h.FunMoney = fun - online;
                h.FunDaily = h.FunMoney / Balance.DaysPerMonth;
            }
        }

        // ================================================================== going out
        static readonly BuildingType[] Venues = { BuildingType.Restaurant, BuildingType.Cafe, BuildingType.Bar, BuildingType.Gym };
        static readonly float[] VenueShare = { 0.42f, 0.14f, 0.14f, 0.1f };
        static readonly float[] VenuePrice = { 25, 6, 16, 15 };
        static readonly int[] VenueMax = { 2, 3, 2, 1 };

        void GoingOut(Household h, Building home, List<Citizen> ms, float thrift)
        {
            float people = 0;
            foreach (var c in ms) if (c.Age >= 14) people += c.IsChild ? 0.4f : 1f;
            if (people <= 0) return;
            double today = Math.Min(h.FunMoney, h.FunDaily);
            foreach (var c in ms)
            {
                if (c.Age < 14 || c.Sick > 0) continue;
                // a haircut every six weeks or so
                if (Chance(0.16f) && h.FunMoney > 45) Visit(h, home, c, BuildingType.HairSalon, 0.4f);
                double mine = today * (c.IsChild ? 0.4 : 1) / people;
                bool went = false;
                for (int i = 0; i < Venues.Length; i++)
                {
                    var type = Venues[i];
                    if (type == BuildingType.Bar && c.Age < 21) continue;
                    float x = (float)(mine * VenueShare[i] / VenuePrice[i]);
                    int n = (int)x; if (Chance(x - n)) n++;
                    n = Math.Min(n, VenueMax[i]);
                    for (int k = 0; k < n; k++) { if (h.FunMoney <= 0 || !Visit(h, home, c, type, 1f)) break; went = true; }
                }
                if (!went && Chance(0.15f)) Park(home, c);
            }
        }

        [NonSerialized] List<Building> venueScratch = new List<Building>();
        bool Visit(Household h, Building home, Citizen c, BuildingType type, float wantWeight)
        {
            if (venueScratch == null) venueScratch = new List<Building>();
            venueScratch.Clear();
            foreach (var b in Buildings) if (b.Type == type && b.Open && CanGo(h, home, b)) venueScratch.Add(b);
            if (venueScratch.Count == 0) { Wants[(int)type] += wantWeight; return false; }
            // the favourite first; if it's full, the next
            Building best = null;
            if (venueScratch.Count > 1) venueScratch.Sort((a, b) => ((float)a.Price * (1 + Distance(home, a) / 60f)).CompareTo((float)b.Price * (1 + Distance(home, b) / 60f)));
            foreach (var b in venueScratch)
            {
                var bd = b.Def;
                if (b.ServedToday >= Capacity(b) || (bd.Input.HasValue && b.Stock[(int)bd.Input.Value] < bd.InputPer)) { b.Lost++; continue; }
                best = b; break;
            }
            if (best == null) return false;
            var d = best.Def;
            double tax = best.Price * Policy.SalesTax;
            if (h.Cash < best.Price + tax + 50) return false;
            HouseholdPays(h, best.Price + tax, HouseholdBook.Leisure);
            h.FunMoney -= best.Price + tax;
            best.Cash += best.Price; best.Revenue += best.Price; best.Customers++; best.ServedToday++; best.CapUsed++;
            if (d.Input.HasValue) best.Stock[(int)d.Input.Value] -= d.InputPer;
            Treasury += tax; Book.SalesTax += tax;
            c.LeisureId = best.Id; c.Outings++;
            if (type == BuildingType.Gym) c.Health = Math.Min(100, c.Health + 1.5f);
            return true;
        }

        void Park(Building home, Citizen c)
        {
            foreach (var b in Buildings)
                if (b.Type == BuildingType.Park && b.Open && Distance(home, b) <= b.Def.Radius + 6) { c.LeisureId = b.Id; c.Outings++; c.Happiness = Math.Min(100, c.Happiness + 0.5f); return; }
        }

        // ================================================================== the doctor
        public int PatientCapacity(Building b)
        {
            int n = 0;
            foreach (var id in b.Employees)
            {
                var c = GetCitizen(id);
                if (c == null || !c.AtWork) continue;
                if (c.Role == Role.Doctor) n += Balance.PatientsPerDoctor;
                else if (c.Role == Role.Nurse) n += Balance.PatientsPerNurse;
            }
            return n;
        }

        [NonSerialized] public int UntreatedMonth;
        void Healthcare(Household h, Building home, List<Citizen> ms)
        {
            foreach (var c in ms)
            {
                if (c.Sick <= 0 || c.Sick > 4 && Chance(0.5f)) continue;
                Building best = null; float bd = float.MaxValue;
                foreach (var b in Buildings)
                {
                    if ((b.Type != BuildingType.Clinic && b.Type != BuildingType.Hospital) || !b.Open) continue;
                    float dist = Distance(home, b);
                    if (dist > b.Def.Radius || b.ServedToday >= PatientCapacity(b)) continue;
                    if (dist < bd) { bd = dist; best = b; }
                }
                if (best == null) { UntreatedMonth++; continue; }
                best.ServedToday++; best.Customers++;
                c.Sick = 0; c.Health = Math.Min(100, c.Health + 9);
                c.ErrandId = best.Id;
            }
        }
    }
}
