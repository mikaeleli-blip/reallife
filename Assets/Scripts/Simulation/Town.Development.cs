using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// Growth. You zone land; developers, entrepreneurs and firms decide what to build on it, and when, from what they see:
    /// empty homes, shoppers turned away, sales nobody in town can make, people looking for work. Construction companies build
    /// everything, buying materials and putting in labour-days; without a local one, outside contractors come in and charge more.
    /// Also the player's tools: roads, zoning, public buildings and the bulldozer.
    /// </summary>
    public partial class Town
    {
        public const double RoadCost = 9000;      // per tile
        public const double DemolishCost = 0.1;   // of the building's value, for public ones

        // ================================================================== the player's tools
        public string CanRoad(int x, int y)
        {
            if (!InBounds(x, y)) return "Off the map.";
            if (IsWater(x, y)) return "Can't build a road on water.";
            if (IsRoad(x, y)) return "Already a road.";
            if (BuildingAt(x, y) != null) return "Something is built here.";
            if (Treasury < RoadCost - 5000000) return "The town is too deep in debt.";
            return null;
        }

        public bool PlaceRoad(int x, int y)
        {
            if (CanRoad(x, y) != null) return false;
            SetRoadBit(x, y, true); SetZoneBits(x, y, Zone.None);
            if (HasTrees(x, y) || Terrain(x, y) == TileBits.Sand) SetTerrain(x, y, TileBits.Grass);
            PayBuilder(RoadCost);
            RebuildRoads(); MapVersion++;
            return true;
        }

        /// <summary>Public works paid by the town go to the local construction company if there is one.</summary>
        void PayBuilder(double amount)
        {
            Treasury -= amount; Book.Construction += amount;
            var c = Buildings.FirstOrDefault(b => b.Type == BuildingType.Construction && b.Open);
            if (c != null) { double cost = amount * 0.55; c.Cash += amount - cost; c.Revenue += amount; c.Supplies += cost; Trade.Imports += cost; }
            else Trade.Imports += amount;
        }

        public bool SetZone(int x, int y, Zone z)
        {
            if (!InBounds(x, y) || IsWater(x, y) || IsRoad(x, y) || BuildingAt(x, y) != null) return false;
            if (ZoneAt(x, y) == z) return false;
            SetZoneBits(x, y, z); MapVersion++;
            return true;
        }

        public string CanPlace(BuildingType t, int x, int y)
        {
            var d = Defs.Get(t);
            if (!d.PlayerBuilds) return "Developers build that on zoned land.";
            if (!FootprintFree(x, y, d.W, d.H)) return "Not enough clear land.";
            if (FindFacing(x, y, d.W, d.H) < 0) return "Needs a road alongside.";
            if (Treasury - d.StartupCost < -5000000) return "The town can't borrow any more.";
            return null;
        }

        public Building PlacePublic(BuildingType t, int x, int y)
        {
            if (CanPlace(t, x, y) != null) return null;
            var d = Defs.Get(t);
            var b = AddBuilding(t, x, y, FindFacing(x, y, d.W, d.H), false);
            b.Owner = Owner.Town;
            for (int ty = y; ty < y + d.H; ty++) for (int tx = x; tx < x + d.W; tx++) SetZoneBits(tx, ty, Zone.None);
            StartConstruction(b, null);
            AddLog("Work started on a new " + d.Name.ToLower() + " (" + Money(d.StartupCost) + ").", LogKind.Info, b.Id);
            return b;
        }

        public string CanBulldoze(int x, int y)
        {
            if (IsRoad(x, y)) return null;
            var b = BuildingAt(x, y);
            if (b == null) return ZoneAt(x, y) != Zone.None ? null : "Nothing here.";
            if (b.Type == BuildingType.FreightTerminal) return "That's the state highway.";
            if (b.Owner == Owner.Town || b.Def.Cat == Category.Public) return null;
            if (!b.Built) return "Private construction site.";
            if (b.Closed && b.Employees.Count == 0) return null;
            if (b.Def.Homes > 0 && b.Households.Count == 0 && b.Owner != Owner.Household) return null;
            return "Privately owned, and in use.";
        }

        public bool Bulldoze(int x, int y)
        {
            if (CanBulldoze(x, y) != null) return false;
            if (IsRoad(x, y))
            {
                SetRoadBit(x, y, false); RebuildRoads(); MapVersion++;
                return true;
            }
            var b = BuildingAt(x, y);
            if (b == null) { SetZoneBits(x, y, Zone.None); MapVersion++; return true; }
            if (b.Owner == Owner.Town) { double c = b.Value * DemolishCost; Treasury -= c; Book.Construction += c; }
            else { double c = b.Value * 0.6; Treasury -= c; Book.Construction += c; SellerGets(b, c); }   // the town buys it out first
            AddLog("The " + b.Def.Name.ToLower() + " was demolished.", LogKind.Info);
            if (b.Def.Cat == Category.Public)
                foreach (var p in People.Where(p => p.SchoolId == b.Id).ToList()) { p.SchoolId = -1; }
            foreach (var hid in b.Households.ToList())
            {
                var h = GetHousehold(hid); if (h == null) continue;
                MoveOut(h);
                var home = FindHome(h.Members.Count, MonthlyIncome(h), 0.5);
                if (home != null) MoveIn(h, home, false);
            }
            RemoveBuilding(b);
            return true;
        }

        // ================================================================== construction
        public Building Contractor(Building site) { return GetBuilding(site.ContractorId); }

        void StartConstruction(Building b, Household client)
        {
            var d = b.Def;
            b.Built = false; b.Progress = 0; b.Started = false;
            b.ContractLeft = d.StartupCost;
            // the local builder with the least on its books
            Building best = null; float least = float.MaxValue;
            foreach (var c in Buildings)
            {
                if (c.Type != BuildingType.Construction || !c.Open || !Connected(c)) continue;
                float backlog = 0; foreach (var o in Buildings) if (!o.Built && o.ContractorId == c.Id) backlog += o.Def.Work - o.Progress;
                backlog /= Math.Max(1, c.Employees.Count);
                if (backlog < least) { least = backlog; best = c; }
            }
            b.ContractorId = best != null ? best.Id : -1;
            if (best == null) b.ContractLeft = d.StartupCost * 1.15;
            MapVersion++;
        }

        /// <summary>Who pays for a building as it goes up: the town, a local family, or outside money.</summary>
        void ClientPays(Building b, double amount)
        {
            if (b.Owner == Owner.Town) { Treasury -= amount; Book.Construction += amount; }
            else if (b.Owner == Owner.Household)
            {
                var h = GetHousehold(b.OwnerId);
                if (h != null) { double fromCash = Math.Min(Math.Max(0, h.Cash - 2000), amount); HouseholdPays(h, fromCash, HouseholdBook.Goods); double rest = amount - fromCash; if (rest > 0) { var l = Loans.FirstOrDefault(x => x.Kind == LoanKind.Business && x.BuildingId == b.Id); if (l != null) { l.Principal += rest; BankCapital -= rest; l.Payment = Annuity(l.Principal, l.Rate, Balance.BusinessMonths); } else NewLoan(LoanKind.Business, h, b, rest, Balance.BusinessRate, Balance.BusinessMonths); } }
            }
            else Trade.Investment += amount;
        }

        void ConstructionDaily()
        {
            var sites = Buildings.Where(b => !b.Built).OrderBy(b => b.BuiltDay).ToList();
            if (sites.Count == 0) return;
            // labour each builder has today
            var labour = new Dictionary<int, float>();
            foreach (var c in Buildings)
            {
                if (c.Type != BuildingType.Construction || !c.Open) continue;
                float l = 0; bool manager = false;
                foreach (var id in c.Employees)
                {
                    var p = GetCitizen(id); if (p == null || !p.AtWork) continue;
                    l += p.Role == Role.ConstructionWorker ? 1f : p.Role == Role.SiteManager ? 0.5f : 1.2f;
                    if (p.Role == Role.SiteManager) manager = true;
                }
                labour[c.Id] = l * (manager ? 1.15f : 1f);
                c.CapTotal += labour[c.Id];
            }
            foreach (var b in sites)
            {
                var d = b.Def;
                var con = GetBuilding(b.ContractorId);
                if (con != null && (!con.Open || !Connected(con))) { b.ContractorId = -1; con = null; b.ContractLeft = Math.Max(b.ContractLeft, (d.Work - b.Progress) * Balance.LaborRate * 1.15); }
                if (!b.Started)
                {
                    // the client pays a deposit that covers the materials, and the builder orders them in
                    double first = d.Materials / d.StartupCost * b.ContractLeft;
                    ClientPays(b, first);
                    if (con != null)
                    {
                        con.Cash += first; con.Revenue += first;
                        double mat = d.Materials;
                        double fee = Freight(mat, -1, con.Id, Good.Materials);
                        con.Cash -= mat + fee; con.Supplies += mat + fee;
                        Trade.Imports += mat; ImportsByGood[(int)Good.Materials] += mat;
                    }
                    else if (b.Owner == Owner.Town || b.Owner == Owner.Household) Trade.Imports += first;
                    b.ContractLeft -= first;
                    b.Started = true;
                }
                float work;
                if (con != null)
                {
                    float avail; labour.TryGetValue(con.Id, out avail);
                    work = Math.Min(avail, Math.Min(d.Work - b.Progress, Math.Max(2f, d.Work / 6f)));
                    if (work <= 0) continue;
                    labour[con.Id] = avail - work;
                    con.CapUsed += work;
                }
                else work = Math.Min(d.Work - b.Progress, 4f);
                float before = b.Progress;
                b.Progress += work;
                double pay = d.Work > 0 ? b.ContractLeft * (work / Math.Max(0.01f, d.Work - before)) : b.ContractLeft;
                pay = Math.Min(pay, b.ContractLeft);
                ClientPays(b, pay);
                if (con != null) { con.Cash += pay; con.Revenue += pay; con.Customers++; }
                else if (b.Owner == Owner.Town || b.Owner == Owner.Household) Trade.Imports += pay;
                b.ContractLeft -= pay;
                if (b.Progress >= d.Work - 0.01f) Finish(b);
            }
        }

        void Finish(Building b)
        {
            var d = b.Def;
            b.Built = true; b.Progress = d.Work; b.BuiltDay = Day; b.ContractorId = -1; b.ContractLeft = 0;
            b.Value = d.Homes > 0 ? d.Value * HousingIndex : d.Value;
            MapVersion++;
            if (d.Cat == Category.Public) { AddLog("The new " + d.Name.ToLower() + " is open.", LogKind.Good, b.Id); return; }
            if (d.Homes > 0)
            {
                if (b.Type == BuildingType.House && b.ForSale) AddLog("A new house went on the market for " + Money(b.Value) + ".", LogKind.Info, b.Id);
                else AddLog("A new " + d.Name.ToLower() + " is finished: " + d.Homes + " homes to let.", LogKind.Info, b.Id);
                return;
            }
            OpenBusiness(b);
        }

        void OpenBusiness(Building b)
        {
            var d = b.Def;
            b.Closed = false; b.ClosedDays = 0; b.Staffing = d.Cat == Category.Industrial ? 0.6f : 0.5f; b.WageMult = 1; b.LossMonths = 0;
            if (d.Sells.HasValue) b.Price = Defs.Retail[(int)d.Sells.Value];
            else if (d.Serves != Service.None) b.Price = d.ServicePrice;
            else if (d.Makes.HasValue) b.Price = Defs.Wholesale[(int)d.Makes.Value];
            double working = MonthlyCosts(b) * 2.5;
            if (b.Owner == Owner.Household)
            {
                var h = GetHousehold(b.OwnerId);
                double put = h != null ? Math.Min(Math.Max(0, h.Cash - 3000), working) : 0;
                if (h != null) HouseholdPays(h, put, HouseholdBook.Goods);
                b.Cash += put;
                if (put < working && h != null) NewLoan(LoanKind.Business, h, b, working - put, Balance.BusinessRate, Balance.BusinessMonths);
                if (put < working) b.Cash += working - put;
                // the founder works there
                var founder = Members(h).Where(m => m.InLaborForce || (m.WorkId >= 0 && !m.Owner)).OrderByDescending(m => m.Ambition).FirstOrDefault(m => QualifiesFor(m, b) != Role.None);
                if (founder != null) { founder.Owner = true; Hire(b, founder, QualifiesFor(founder, b), false); Note(founder, "Opened " + PlaceName(b) + "."); }
                AddLog((founder != null ? founder.Name : "The " + h.Name + " family") + " opened " + PlaceName(b) + ".", LogKind.Good, b.Id);
            }
            else
            {
                b.Cash += working; Trade.Investment += working;
                AddLog(b.OwnerName + " opened a " + d.Name.ToLower() + " in town, hiring.", LogKind.Good, b.Id);
            }
            MapVersion++;
        }

        // ================================================================== developers
        /// <summary>Where a building of this type could go on land zoned for it: the most central free spot.</summary>
        bool FindLot(BuildingType t, out int bx, out int by)
        {
            var d = Defs.Get(t);
            bx = by = -1; float best = float.MaxValue;
            float cx = 48, cy = 48; var th = Buildings.FirstOrDefault(b => b.Type == BuildingType.TownHall);
            if (th != null) { cx = th.CenterX; cy = th.CenterY; }
            for (int y = 0; y + d.H <= Rows; y++)
                for (int x = 0; x + d.W <= Cols; x++)
                {
                    if (ZoneAt(x, y) != d.Zone) continue;
                    bool ok = true;
                    for (int ty = y; ty < y + d.H && ok; ty++) for (int tx = x; tx < x + d.W && ok; tx++) if (ZoneAt(tx, ty) != d.Zone) ok = false;
                    if (!ok || !FootprintFree(x, y, d.W, d.H)) continue;
                    int f = FindFacing(x, y, d.W, d.H);
                    if (f < 0) continue;
                    var tmp = new Building { Type = t, X = x, Y = y, Facing = f };
                    if (!ConnectedFootprint(tmp)) continue;
                    float score = Math.Abs(x + d.W / 2f - cx) + Math.Abs(y + d.H / 2f - cy) + Rand() * 4;
                    if (score < best) { best = score; bx = x; by = y; }
                }
            return bx >= 0;
        }

        bool ConnectedFootprint(Building b)
        {
            int ax, ay;
            if (!AccessTile(b, out ax, out ay)) return false;
            return mainComp >= 0 && roadComp[ax + ay * Cols] == mainComp;
        }

        public int ZonedFree(Zone z)
        {
            int n = 0;
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Cols; x++) if (ZoneAt(x, y) == z && buildingAt[Idx(x, y)] == 0) n++;
            return n;
        }

        /// <summary>Demand for each zone, -1..1, for the meters in the HUD.</summary>
        public float[] ZoneDemand = new float[4];

        Building Develop(BuildingType t, Household founder)
        {
            int x, y;
            if (!FindLot(t, out x, out y)) { Wants[(int)t] += 0.5f; LotShort[(int)Defs.Get(t).Zone] = true; return null; }
            var d = Defs.Get(t);
            var b = AddBuilding(t, x, y, FindFacing(x, y, d.W, d.H), false);
            if (founder != null) { b.Owner = Owner.Household; b.OwnerId = founder.Id; b.OwnerName = founder.Name; }
            else
            {
                b.Owner = Owner.Outside;
                b.OwnerName = d.Homes > 0 ? Pick(Names.Firms) + (t == BuildingType.House ? " Homes" : " Properties") : FirmName(t);
                if (t == BuildingType.House) b.ForSale = true;
            }
            b.Closed = d.Homes == 0;
            StartConstruction(b, founder);
            return b;
        }
        [NonSerialized] public bool[] LotShort = new bool[4];

        /// <summary>The month's building decisions.</summary>
        void Developers()
        {
            if (LotShort == null || LotShort.Length != 4) LotShort = new bool[4];
            for (int i = 0; i < 4; i++) LotShort[i] = false;
            int pop = Population;
            int building = Buildings.Count(b => !b.Built);
            int buildCap = 3 + Buildings.Count(b => b.Type == BuildingType.Construction && b.Open) * 4;
            // ---------------------------------------------------------------- homes
            int free = VacantUnits() + Buildings.Count(b => b.ForSale && b.Households.Count == 0);
            int coming = Buildings.Where(b => !b.Built && b.Def.Homes > 0).Sum(b => b.Def.Homes);
            int openings = Openings().Sum(o => o.Count);
            int wantHomes = (int)(LastWants[(int)BuildingType.House] + openings * 0.6f + pop * 0.03f);
            float resDemand = (wantHomes - free - coming) / (float)Math.Max(6, pop / 12);
            ZoneDemand[(int)Zone.Residential] = Math.Max(-1, Math.Min(1, resDemand));
            int gap = wantHomes - free - coming;
            while (gap > 0 && building < buildCap)
            {
                Building b = null;
                if (gap >= 7 && Chance(0.6f)) b = Develop(BuildingType.Apartments, RichFamily(Defs.Get(BuildingType.Apartments).StartupCost * 0.3));
                if (b == null) b = Develop(BuildingType.House, null);
                if (b == null) break;
                gap -= b.Def.Homes; building++;
            }
            // ---------------------------------------------------------------- shops and services
            float comDemand = 0;
            foreach (var t in new[] { BuildingType.GroceryStore, BuildingType.Restaurant, BuildingType.Cafe, BuildingType.Bar, BuildingType.ClothingStore,
                                      BuildingType.ElectronicsStore, BuildingType.FurnitureStore, BuildingType.GasStation, BuildingType.CarDealer,
                                      BuildingType.AutoRepair, BuildingType.HairSalon, BuildingType.Gym, BuildingType.Bank })
            {
                var d = Defs.Get(t);
                if (pop < d.MinPop) continue;
                var same = Buildings.Where(b => b.Type == t).ToList();
                var open = same.Where(b => b.Open).ToList();
                bool pending = same.Any(b => !b.Built);
                if (pending) continue;
                float want = LastWants[(int)t];
                float lost = open.Sum(b => b.LastLost), served = open.Sum(b => b.LastCustomers);
                bool busy = open.Count > 0 && lost > Math.Max(6, served * 0.12f) && open.All(b => b.Staffing >= 0.95f || b.LastUse > 0.85f);
                bool profitable = open.Count == 0 || open.Average(b => b.LastProfit) > 0;
                int perPop = PopPer(t);
                bool room = open.Count < 1 + pop / perPop;
                bool missing = open.Count == 0 && want > 2;
                if (missing) comDemand += 0.25f;
                if (!(missing || (busy && profitable && room)) || (t == BuildingType.Bank && open.Count > 0 && pop < 1500)) continue;
                if (building >= buildCap) { comDemand += 0.2f; continue; }
                // reopen an empty shop of the same kind first
                var shut = same.FirstOrDefault(b => b.Closed && b.Built && Connected(b));
                var founder = Entrepreneur(t);
                if (shut != null)
                {
                    if (founder != null) { double price = shut.Value * 0.6; HouseholdPays(founder, Math.Min(price, founder.Cash * 0.5), HouseholdBook.Goods); Trade.Profits += Math.Min(price, founder.Cash * 0.5); shut.Owner = Owner.Household; shut.OwnerId = founder.Id; shut.OwnerName = founder.Name; }
                    else { shut.Owner = Owner.Outside; shut.OwnerName = t == BuildingType.Bank ? BankName : FirmName(t); }
                    OpenBusiness(shut);
                    continue;
                }
                if (Develop(t, founder) != null) building++;
            }
            ZoneDemand[(int)Zone.Commercial] = Math.Min(1, comDemand);
            // ---------------------------------------------------------------- work: offices and industry follow the workforce
            var looking = People.Where(c => c.InLaborForce && c.WorkId < 0 && c.JoblessDays >= 2).ToList();
            int grads = looking.Count(c => c.Edu >= Edu.Degree) + People.Count(c => c.Edu >= Edu.Degree && c.WorkId >= 0 && Defs.Get(c.Role).Needs < Edu.HighSchool);
            int school = looking.Count(c => c.Edu == Edu.HighSchool), trade = looking.Count(c => c.Edu == Edu.Trade), basic = looking.Count(c => c.Edu == Edu.None);
            float indDemand = (looking.Count - pop * 0.04f) / Math.Max(6f, pop / 20f);
            ZoneDemand[(int)Zone.Industrial] = Math.Max(-1, Math.Min(1, indDemand));
            bool officePending = Buildings.Any(b => b.Type == BuildingType.Office && !b.Built);
            if (!officePending && building < buildCap && (grads >= 5 || (grads >= 3 && school >= 4)) && Chance(0.6f))
            {
                if (Expand(BuildingType.Office, null) != null) building++;
            }
            bool factoryPending = Buildings.Any(b => !b.Built && b.Def.Cat == Category.Industrial);
            if (!factoryPending && building < buildCap && looking.Count >= 7 + pop / 40 && Chance(0.55f))
            {
                // the kind of plant that would hire the people who are looking
                BuildingType t;
                if (grads >= 2 && trade >= 2 && Chance(0.6f)) t = BuildingType.ElectronicsPlant;
                else if (basic > school && basic >= 4) t = Chance(0.5f) ? BuildingType.FoodPlant : BuildingType.Farm;
                else if (trade >= 2 && Chance(0.5f)) t = BuildingType.FurnitureWorkshop;
                else t = BuildingType.GarmentFactory;
                if (Expand(t, t == BuildingType.Farm ? Entrepreneur(t) : null) != null) building++;
            }
            // food: a plant when stores import groceries, farms when the plant imports crops
            if (building < buildCap && !factoryPending)
            {
                double grocImports = LastImports[(int)Good.Groceries], cropImports = LastImports[(int)Good.Crops];
                if (grocImports > 25000 && !Buildings.Any(b => b.Type == BuildingType.FoodPlant && !b.Built)) { if (Expand(BuildingType.FoodPlant, null) != null) building++; }
                else if (cropImports > 12000 && !Buildings.Any(b => b.Type == BuildingType.Farm && !b.Built)) { if (Expand(BuildingType.Farm, Entrepreneur(BuildingType.Farm)) != null) building++; }
            }
            // builders: when work is piling up
            float backlog = Buildings.Where(b => !b.Built).Sum(b => b.Def.Work - b.Progress);
            int builders = Buildings.Count(b => b.Type == BuildingType.Construction && b.Open);
            if (backlog > 300 + builders * 260 && !Buildings.Any(b => b.Type == BuildingType.Construction && !b.Built))
                Expand(BuildingType.Construction, Entrepreneur(BuildingType.Construction));
            // hauliers: when outside trucking is costing a lot
            if (pop >= Defs.Get(BuildingType.Trucking).MinPop && LastFreightOutside > 9000 && !Buildings.Any(b => b.Type == BuildingType.Trucking && (b.Open || !b.Built)))
                Expand(BuildingType.Trucking, Entrepreneur(BuildingType.Trucking));
        }
        [NonSerialized] public double[] LastImports = new double[Defs.GoodCount];
        [NonSerialized] public double LastFreightOutside;

        static int PopPer(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.GroceryStore: return 350;
                case BuildingType.Restaurant: return 220;
                case BuildingType.Cafe: return 260;
                case BuildingType.Bar: return 280;
                case BuildingType.ClothingStore: return 380;
                case BuildingType.ElectronicsStore: return 600;
                case BuildingType.FurnitureStore: return 700;
                case BuildingType.GasStation: return 450;
                case BuildingType.CarDealer: return 900;
                case BuildingType.AutoRepair: return 400;
                case BuildingType.HairSalon: return 320;
                case BuildingType.Gym: return 500;
                default: return 2500;
            }
        }

        /// <summary>A local family with the savings and the drive to open this kind of business, and someone who can work in it.</summary>
        Household Entrepreneur(BuildingType t)
        {
            var d = Defs.Get(t);
            double need = d.StartupCost * 0.25;
            foreach (var h in Households.OrderByDescending(x => x.Cash))
            {
                if (h.Cash < need + 8000) break;
                if (Buildings.Any(b => b.Owner == Owner.Household && b.OwnerId == h.Id && IsBusiness(b))) continue;
                var tmp = new Building { Type = t, Staffing = 1 };
                foreach (var m in Members(h))
                {
                    if (m.IsChild || m.Retired || m.Age > 60 || m.Owner) continue;
                    if (m.Ambition < 0.55f) continue;
                    bool can = d.Jobs.Any(j => Defs.Qualifies(m.Edu, Defs.Get(j.Role).Needs));
                    if (can && Chance(0.6f)) return h;
                }
            }
            return null;
        }

        /// <summary>Reopens an empty building of this kind under a new owner, or builds a new one.</summary>
        Building Expand(BuildingType t, Household founder)
        {
            var shut = Buildings.FirstOrDefault(b => b.Type == t && b.Closed && b.Built && Connected(b));
            if (shut == null) return Develop(t, founder);
            if (founder != null)
            {
                double price = Math.Min(shut.Value * 0.6, founder.Cash * 0.5);
                HouseholdPays(founder, price, HouseholdBook.Goods); Trade.Profits += price;
                shut.Owner = Owner.Household; shut.OwnerId = founder.Id; shut.OwnerName = founder.Name;
            }
            else { shut.Owner = Owner.Outside; shut.OwnerName = FirmName(t); }
            OpenBusiness(shut);
            return shut;
        }

        Household RichFamily(double need)
        {
            return Households.Where(h => h.Cash > need + 40000).OrderByDescending(h => h.Cash).FirstOrDefault(h => Chance(0.5f));
        }

        // ================================================================== the town's own bills
        void GovernmentMonthly()
        {
            foreach (var b in Buildings)
            {
                if (b.Def.Cat != Category.Public || !b.Built) continue;
                double u = b.Def.Upkeep;
                Treasury -= u; Book.Upkeep += u; Trade.Imports += u;
            }
            int roads = 0; for (int i = 0; i < Tiles.Length; i++) if ((Tiles[i] & TileBits.Road) != 0) roads++;
            double ru = roads * 50;
            Treasury -= ru; Book.Upkeep += ru; Repairs(ru);
            if (Treasury < 0)
            {
                double i = -Treasury * 0.055 / 12;
                Treasury -= i; Book.Interest += i; Trade.Profits += i;
                if (MonthOfYear == 0 && Day % Balance.DaysPerMonth == 0) AddLog("The town owes " + Money(-Treasury) + ". Interest is " + Money(i) + " a month.", LogKind.Bad);
            }
        }
    }
}
