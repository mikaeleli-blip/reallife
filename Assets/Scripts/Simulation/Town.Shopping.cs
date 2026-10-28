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

        static BuildingType StoreFor(Good g) { return Defs.GoodInfo(g).Store; }

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
                Basket(h, home, ms, thrift);
                CarsDaily(h, home, ms, thrift);
                GoingOut(h, home, ms, thrift);
                NowAndThen(h, home, ms, thrift);
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
            bool drives = HasUsableCar(h);
            // the cheapest for the trip: those on foot care more how far it is; a nicer store is worth a bit more to a family that
            // can afford it; a warehouse club is no use without a car, but worth a longer drive (you go less often, and fill the boot)
            double wealth = Math.Max(0.4, Math.Min(1.6, MonthlyIncome(h) / 7000));
            var shops = Buildings.Where(b => b.Def.Sells == g && b.Open && (!b.Def.NeedsCar || drives) && CanGo(h, home, b))
                                 .OrderBy(b => b.Price * (1 + Distance(home, b) / (!drives ? 35f : b.Def.NeedsCar ? 150f : 90f)) * ParkingFactor(home, b, drives) * (1 - b.Def.Appeal * wealth)).ToList();
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
                if (Defs.Goods[(int)g].Whole) q = (float)Math.Floor(q);
                if (q <= 0.01f) break;
                double value = q * b.Price, tax = value * Policy.SalesTax;
                HouseholdPays(h, value + tax, LineFor(g));
                SpendByGood[(int)g] += value + tax;
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
                    if (canGo && (Defs.Goods[(int)g].MustHave || Chance(0.5f)))
                    {
                        double unit = Defs.Retail[(int)g] * 1.12 * PriceDrift[(int)g];
                        float q = (float)Math.Min(qty - got, Math.Max(0, h.Cash - 100) / unit);
                        if (Defs.Goods[(int)g].Whole) q = (float)Math.Floor(q);
                        if (q > 0.01f) { double v = q * unit; HouseholdPays(h, v, LineFor(g)); SpendByGood[(int)g] += v; Trade.Imports += v; OutOfTown += v; AwayMonth[(int)type] += v; got += q; h.Fuel -= 0.2f; }
                    }
                }
            }
            if (g == Good.Groceries) h.Pantry += got;
            return got;
        }
        [NonSerialized] public double OutOfTown;
        /// <summary>What households spent this month on each good and each service (for the economy panel and tests).</summary>
        [NonSerialized] public double[] SpendByGood = new double[Defs.GoodCount], SpendByService = new double[Enum.GetValues(typeof(Service)).Length];
        [NonSerialized] public double[] LastSpendByGood = new double[Defs.GoodCount], LastSpendByService = new double[Enum.GetValues(typeof(Service)).Length];

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
                double online = fun * 0.18;   // online shopping, trips, streaming
                if (online > 0) { HouseholdPays(h, online, HouseholdBook.Goods); Trade.Imports += online; OutOfTown += online; }
                h.FunMoney = fun - online;
                h.FunDaily = h.FunMoney / Balance.DaysPerMonth;
            }
        }

        // ================================================================== the other things households buy
        /// <summary>The supplies a household keeps of the other things it buys (Catalog.Needs): shoes, toiletries, books, toys,
        /// pet food and the rest. Each wears out or is used up, and when it's low they shop for more: necessities out of their
        /// cash, wants out of their spending money, luxuries only if they're comfortable.</summary>
        void Basket(Household h, Building home, List<Citizen> ms, float thrift)
        {
            if (h.Has == null || h.Has.Length != Defs.GoodCount) { h.Has = Resized(h.Has, Defs.GoodCount, -1f); for (int i = 0; i < h.Has.Length; i++) if (h.Has[i] < 0) h.Has[i] = 0.6f + Rand() * 0.35f; }
            double wealth = -1;
            foreach (var n in Defs.Needs)
            {
                int gi = (int)n.Good;
                float count = Count(h, ms, home, n.Who);
                if (count <= 0) continue;
                float wear = n.Wear;
                if (n.Good == Good.Plants) wear *= PlantSeason[CropSeason];
                if (n.Good == Good.Hardware && home.Type != BuildingType.House) wear *= 0.35f;
                h.Has[gi] = Math.Max(0, h.Has[gi] - wear);
                if (h.Has[gi] > n.Low) continue;
                var gd = Defs.Goods[gi];
                float full = n.Units * count;
                float qty = (1 - h.Has[gi]) * full;
                if (gd.Whole) qty = Math.Max(1, (float)Math.Round(qty));
                double cost = qty * gd.Retail;
                bool ok;
                // necessities out of their cash; wants out of what they have to spend this month (or savings, if they have
                // plenty); luxuries only if they're comfortable
                if (n.Level == 0) ok = h.Cash > cost + 100;
                else if (n.Level == 1) ok = h.FunMoney > cost * 0.6 || h.Cash > Essentials(h) * 8 + cost;
                else
                {
                    if (wealth < 0) wealth = MonthlyIncome(h) / 7000;
                    ok = wealth >= 1.0 && (h.FunMoney > cost || h.Cash > Essentials(h) * 10 + cost);
                }
                if (!ok) continue;
                double before = h.Cash;
                float got = Shop(h, home, n.Good, qty, ms);
                if (n.Level > 0) h.FunMoney -= Math.Max(0, before - h.Cash);
                h.Has[gi] = Math.Min(1, h.Has[gi] + got / full);
            }
        }
        static readonly float[] PlantSeason = { 0.15f, 1.7f, 1.3f, 0.6f };

        /// <summary>How well a household is doing for the things it buys: the share of its supplies it has (1 all full).</summary>
        public float Comfort(Household h)
        {
            if (h.Has == null) return 0.7f;
            var home = HomeOf(h); var ms = Members(h);
            float s = 0; int n = 0;
            foreach (var need in Defs.Needs) if (Count(h, ms, home, need.Who) > 0) { s += h.Has[(int)need.Good]; n++; }
            return n > 0 ? s / n : 0.7f;
        }

        /// <summary>A fixed coin toss for a household: whether it keeps pets, drinks, cycles, plays music.</summary>
        static bool Trait(Household h, int salt, float p)
        {
            uint x = (uint)(h.Id * 2654435761u) ^ (uint)(salt * 40503u);
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
            return (x % 10000) / 10000f < p;
        }

        /// <summary>How many in a household something is for.</summary>
        float Count(Household h, List<Citizen> ms, Building home, Who who)
        {
            int n = 0;
            switch (who)
            {
                case Who.Household: return 1;
                case Who.Everyone: return ms.Count;
                case Who.Adults: foreach (var c in ms) if (!c.IsChild) n++; return n;
                case Who.Drinkers: if (!Trait(h, 2, 0.72f)) return 0; foreach (var c in ms) if (c.Age >= 21) n++; return n;
                case Who.Kids: foreach (var c in ms) if (c.Age >= 2 && c.Age < 13) n++; return n;
                case Who.Readers: foreach (var c in ms) if (c.Age >= 8) n++; return n;
                case Who.Active: foreach (var c in ms) if (c.Age >= 8 && c.Age < 56) n++; return n;
                case Who.Older: { float o = 0; foreach (var c in ms) o += c.Age >= 65 ? 2 : c.Age >= 45 ? 1 : 0; return o; }
                case Who.Students: foreach (var c in ms) if (c.Stage != Stage.None) n++; return n;
                case Who.PetOwners: return Trait(h, 1, 0.42f) ? 1 : 0;
                case Who.Gardeners: return home != null && home.Type == BuildingType.House ? 1 : 0;
                case Who.Cyclists: if (!Trait(h, 3, 0.45f)) return 0; foreach (var c in ms) if (c.Age >= 6 && c.Age < 61) n++; return n;
                case Who.Musicians: return Trait(h, 4, 0.15f) ? 1 : 0;
                case Who.Wealthy: return MonthlyIncome(h) >= 7700 ? 1 : 0;
                case Who.Flat: return home != null && (home.Type == BuildingType.Apartments || Trait(h, 5, 0.12f)) ? 1 : 0;
                case Who.Drivers: return HasUsableCar(h) ? 1 : 0;
                case Who.Carless: if (HasUsableCar(h)) return 0; foreach (var c in ms) if (!c.IsChild && !c.Retired || c.Age >= 70) n++; return n;
                case Who.Toddlers:
                {
                    // small children, when there's nobody at home to look after them, no grandparent nearby to help, and the
                    // family can pay for care
                    if (!Trait(h, 6, 0.35f)) return 0;
                    foreach (var c in ms) if (!c.IsChild && c.WorkId < 0 && !c.IsStudent) return 0;
                    if (MonthlyIncome(h) < 5500) return 0;
                    foreach (var c in ms) if (c.Age >= 0.5f && c.Age < 5) n++;
                    return n;
                }
            }
            return 0;
        }

        // ================================================================== going out
        static ServiceDef[] leisure, routine;
        [NonSerialized] int offerDay = -1;
        [NonSerialized] bool[] offered;
        [NonSerialized] float offerShare;

        /// <summary>Which services the town has a place for today, and the share of going-out money those take.</summary>
        void Offers()
        {
            if (leisure == null)
            {
                leisure = Defs.AllServices.Where(x => x.Kind == ServiceKind.Leisure).ToArray();
                routine = Defs.AllServices.Where(x => x.Kind == ServiceKind.Routine).ToArray();
            }
            if (offerDay == Day && offered != null) return;
            offerDay = Day;
            int n = Enum.GetValues(typeof(Service)).Length;
            if (offered == null || offered.Length != n) offered = new bool[n];
            Array.Clear(offered, 0, n);
            foreach (var b in Buildings) if (b.Open && b.Def.Serves != Service.None) offered[(int)b.Def.Serves] = true;
            offerShare = 0;
            foreach (var sd in leisure) if (offered[(int)sd.Service]) offerShare += sd.Share;
        }

        /// <summary>The usual price of a service: what its usual place charges.</summary>
        static double UsualPrice(ServiceDef sd) { return Defs.Get(sd.Venue).ServicePrice; }

        void GoingOut(Household h, Building home, List<Citizen> ms, float thrift)
        {
            Offers();
            float people = 0;
            foreach (var c in ms) if (c.Age >= 4) people += c.IsChild ? 0.4f : 1f;
            if (people <= 0) return;
            double today = Math.Min(h.FunMoney, h.FunDaily);
            // what the town doesn't have, they'd spend on what it does (and they'd still like to have it)
            float scale = offerShare > 0 ? Math.Min(1.8f, 0.8f / offerShare) : 0;
            foreach (var c in ms)
            {
                if (c.Age < 4 || c.Sick > 0) continue;
                double mine = today * (c.IsChild ? 0.4 : 1) / people;
                bool went = false;
                foreach (var sd in leisure)
                {
                    if (c.Age < sd.MinAge || c.Age > sd.MaxAge) continue;
                    float x = (float)(mine * sd.Share / UsualPrice(sd));
                    if (!offered[(int)sd.Service]) { Wants[(int)sd.Venue] += x; continue; }
                    x *= scale;
                    int n = (int)x; if (Chance(x - n)) n++;
                    n = Math.Min(n, sd.MaxPerDay);
                    for (int k = 0; k < n; k++) { if (h.FunMoney <= 0 || !Visit(h, home, c, sd, 1f, true)) break; went = true; }
                }
                if (!went && Chance(0.15f)) Park(home, c);
            }
        }

        /// <summary>The things people need now and then: a haircut, the dentist, the laundromat, the vet, lessons for the kids,
        /// daycare, a taxi.</summary>
        void NowAndThen(Household h, Building home, List<Citizen> ms, float thrift)
        {
            Offers();
            foreach (var sd in routine)
            {
                double price = UsualPrice(sd);
                if (sd.Who == Who.Everyone)
                {
                    foreach (var c in ms)
                    {
                        if (c.Age < sd.MinAge || c.Age > sd.MaxAge || c.Sick > 0 || !Chance(sd.Chance)) continue;
                        if (!CanPay(h, sd, price, thrift)) break;
                        Visit(h, home, c, sd, sd.Service == Service.Haircut ? 0.4f : 1f, sd.FromFun);
                    }
                    continue;
                }
                float count = Count(h, ms, home, sd.Who);
                if (sd.Who == Who.Students) { count = 0; foreach (var c in ms) if (c.Age >= sd.MinAge && c.Age <= sd.MaxAge && c.Stage != Stage.None) count++; }
                for (int i = 0; i < (int)Math.Ceiling(count); i++)
                {
                    if (!Chance(sd.Chance) || !CanPay(h, sd, price, thrift)) continue;
                    // who goes: an adult for the household's errands, the child for their own lessons
                    Citizen who = sd.Who == Who.Students ? ms.Where(c => c.Age >= sd.MinAge && c.Age <= sd.MaxAge && c.Stage != Stage.None).Skip(i).FirstOrDefault()
                                : ms.FirstOrDefault(c => !c.IsChild && c.ErrandId < 0) ?? ms.FirstOrDefault(c => !c.IsChild);
                    if (who == null) who = ms[0];
                    Visit(h, home, who, sd, 1f, sd.FromFun);
                }
            }
        }

        bool CanPay(Household h, ServiceDef sd, double price, float thrift)
        {
            if (sd.FromFun) return h.FunMoney > price * 0.5 || CanSpare(h, price * 3, thrift);
            return h.Cash > price + 80;
        }

        /// <summary>Taste: someone's liking for a kind of place (some love sushi, some never go), fixed for life.</summary>
        static float Taste(Citizen c, BuildingType t)
        {
            uint x = (uint)(c.Id * 2654435761u) ^ (uint)((int)t * 2246822519u);
            x ^= x >> 13; x *= 3266489917u; x ^= x >> 16;
            return 0.8f + 0.4f * ((x % 10000) / 10000f);
        }

        [NonSerialized] List<Building> venueScratch = new List<Building>();
        bool Visit(Household h, Building home, Citizen c, ServiceDef sd, float wantWeight, bool fromFun)
        {
            if (venueScratch == null) venueScratch = new List<Building>();
            venueScratch.Clear();
            var s = sd.Service;
            foreach (var b in Buildings) if (b.Def.Serves == s && b.Open && CanGo(h, home, b)) venueScratch.Add(b);
            if (venueScratch.Count == 0) { Wants[(int)sd.Venue] += wantWeight; return false; }
            // the one they like best for the trip; if it's full, the next
            Building best = null;
            bool drives = HasUsableCar(h);
            if (venueScratch.Count > 1)
            {
                double wealth = Math.Max(0.4, Math.Min(1.6, MonthlyIncome(h) / 7000));
                venueScratch.Sort((a, b) => Liking(h, home, c, a, drives, wealth).CompareTo(Liking(h, home, c, b, drives, wealth)));
            }
            foreach (var b in venueScratch)
            {
                if (b.ServedToday >= Capacity(b) || !Supplied(b)) { b.Lost++; continue; }
                best = b; break;
            }
            if (best == null) return false;
            var d = best.Def;
            double tax = best.Price * Policy.SalesTax;
            if (h.Cash < best.Price + tax + 50) return false;
            int line = fromFun ? HouseholdBook.Leisure : s == Service.Childcare || s == Service.Lessons ? HouseholdBook.School : HouseholdBook.Goods;
            HouseholdPays(h, best.Price + tax, line);
            SpendByService[(int)s] += best.Price + tax;
            if (fromFun) h.FunMoney -= best.Price + tax;
            best.Cash += best.Price; best.Revenue += best.Price; best.Customers++; best.ServedToday++; best.CapUsed++;
            foreach (var i in d.Inputs) best.Stock[(int)i.Good] = Math.Max(0, best.Stock[(int)i.Good] - i.Per);
            Treasury += tax; Book.SalesTax += tax;
            if (sd.Kind == ServiceKind.Leisure) { c.LeisureId = best.Id; c.Outings++; } else c.ErrandId = best.Id;
            if (sd.Health > 0) c.Health = Math.Min(100, c.Health + sd.Health);
            return true;
        }

        float Liking(Household h, Building home, Citizen c, Building b, bool drives, double wealth)
        {
            return (float)(b.Price * (1 + Distance(home, b) / 60f) * ParkingFactor(home, b, drives) * (1 - b.Def.Appeal * wealth)) * Taste(c, b.Type);
        }

        /// <summary>Has it what a customer uses up.</summary>
        static bool Supplied(Building b)
        {
            foreach (var i in b.Def.Inputs) if (b.Stock[(int)i.Good] < i.Per) return false;
            return true;
        }

        // ================================================================== visitors and funerals
        /// <summary>People from out of town staying the night: on business with the town's plants and offices, visiting family,
        /// passing through. They pay a motel or hotel here, which is money from outside; with nowhere to stay they go on.</summary>
        void Visitors()
        {
            int biz = 0;
            foreach (var b in Buildings) if (b.Open && (b.Def.Cat == Category.Industrial || b.Def.Bills || b.Type == BuildingType.Office)) biz++;
            float v = Population * 0.006f + biz * 0.35f;
            int n = (int)v + (Chance(v - (int)v) ? 1 : 0);
            if (n == 0) return;
            var stays = Buildings.Where(b => b.Open && b.Def.Serves == Service.Lodging && Connected(b)).ToList();
            for (int i = 0; i < n; i++)
            {
                Building best = null; double bv = double.MaxValue;
                foreach (var b in stays)
                {
                    if (b.ServedToday >= Capacity(b) || !Supplied(b)) continue;
                    double val = b.Price * (1 - b.Def.Appeal) * (0.8 + Rand() * 0.4);
                    if (val < bv) { bv = val; best = b; }
                }
                if (best == null) { Wants[(int)BuildingType.Motel] += 1; foreach (var b in stays) b.Lost++; continue; }
                best.Cash += best.Price; best.Revenue += best.Price; best.Customers++; best.ServedToday++; best.CapUsed++;
                foreach (var x in best.Def.Inputs) best.Stock[(int)x.Good] = Math.Max(0, best.Stock[(int)x.Good] - x.Per);
                double tax = best.Price * Policy.SalesTax;
                Treasury += tax; Book.SalesTax += tax;
                Trade.Services += best.Price + tax;
            }
        }

        /// <summary>A funeral: the family pays, at the funeral home here if there is one, otherwise in the next town.</summary>
        void Funeral(Household h, Building home)
        {
            double price = Defs.Get(BuildingType.FuneralHome).ServicePrice;
            var fh = Buildings.Where(b => b.Open && b.Def.Serves == Service.Funeral && b.ServedToday < Math.Max(1, Capacity(b))).OrderBy(b => home != null ? Distance(home, b) : 0).FirstOrDefault();
            double pay = Math.Min(price, Math.Max(0, h.Cash));
            if (pay <= 0) return;
            HouseholdPays(h, pay, HouseholdBook.Goods);
            if (fh != null)
            {
                fh.Cash += pay; fh.Revenue += pay; fh.Customers++; fh.ServedToday++; fh.CapUsed++;
            }
            else { Wants[(int)BuildingType.FuneralHome] += 1; AwayMonth[(int)BuildingType.FuneralHome] += pay; Trade.Imports += pay; OutOfTown += pay; }
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
