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
            if (t == BuildingType.TownHall && Buildings.Any(b => b.Type == BuildingType.TownHall)) return "The town already has a town hall.";
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
            else { double c = b.Value * 0.6; Treasury -= c; Book.Construction += c; SellerGets(b, c, false); }   // the town buys it out first
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
            // the builder prices the labour at what it's paying for it; outside contractors charge more
            if (best != null) b.ContractLeft = d.Materials + (d.StartupCost - d.Materials) * Math.Max(1, best.WageMult);
            else b.ContractLeft = d.StartupCost * 1.15;
            MapVersion++;
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
                    else Trade.Imports += first;
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
                else Trade.Imports += pay;
                b.ContractLeft -= pay;
                if (b.Progress >= d.Work - 0.01f) Finish(b);
            }
        }

        void Finish(Building b)
        {
            var d = b.Def;
            b.Built = true; b.Progress = d.Work; b.BuiltDay = Day; b.ContractorId = -1; b.ContractLeft = 0; b.EquityLeft = 0;
            b.Value = d.Homes > 0 ? d.Value * HousingIndex : d.Value;
            StartRepaying(b);
            MapVersion++;
            if (d.Cat == Category.Public) { AddLog("The new " + d.Name.ToLower() + " is open.", LogKind.Good, b.Id); return; }
            if (d.Homes > 0)
            {
                var firm = OwnerFirm(b);
                if (b.Type == BuildingType.House && b.ForSale) AddLog((firm != null ? Title(firm) + " put a new house" : "A new house went") + " on the market for " + Money(b.Value) + ".", LogKind.Info, b.Id);
                else AddLog((firm != null ? Title(firm) + "'s" : "A") + " new " + d.Name.ToLower() + " is finished: " + d.Homes + " homes to let.", LogKind.Info, b.Id);
                return;
            }
            OpenBusiness(b);
        }

        void OpenBusiness(Building b)
        {
            var d = b.Def;
            b.Closed = false; b.ClosedDays = 0; b.Staffing = d.Cat == Category.Industrial ? 0.75f : 0.5f; b.WageMult = 1; b.LossMonths = 0; b.BuiltDay = Day;
            if (d.Sells.HasValue) b.Price = Defs.Retail[(int)d.Sells.Value];
            else if (d.Serves != Service.None) b.Price = d.ServicePrice;
            else if (d.Makes.HasValue) b.Price = Defs.Wholesale[(int)d.Makes.Value];
            // enough to see it through to the first sales: longer for a property firm waiting on its first houses
            double working = MonthlyCosts(b) * (b.Type == BuildingType.Realty ? 6 : b.Type == BuildingType.Construction ? 3.5 : 2.5);
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
                AddLog((founder != null ? founder.Name : "The " + (h != null ? h.Name : "") + " family") + " opened " + PlaceName(b) + ".", LogKind.Good, b.Id);
            }
            else
            {
                // nobody's savings behind it: the bank lends the working capital
                NewLoan(LoanKind.Business, null, b, working, Balance.BusinessRate, Balance.BusinessMonths);
                b.Cash += working;
                AddLog(Title(b) + " opened and is hiring.", LogKind.Good, b.Id);
            }
            if (b.Type == BuildingType.Realty) PlanFirmStaff(b);
            MapVersion++;
        }

        // ================================================================== developers
        /// <summary>Where a building of this type could go on land zoned for it: the most central free spot.</summary>
        bool FindLot(BuildingType t, out int bx, out int by)
        {
            var d = Defs.Get(t);
            bx = by = -1; float best = float.MaxValue;
            float cx, cy; TownCentre(out cx, out cy);
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

        /// <summary>Where the town grows out from: the town hall; before there is one, the middle of what's been built and
        /// zoned; on empty land, the end of the highway.</summary>
        public void TownCentre(out float cx, out float cy)
        {
            var th = Buildings.FirstOrDefault(b => b.Type == BuildingType.TownHall);
            if (th != null) { cx = th.CenterX; cy = th.CenterY; return; }
            double sx = 0, sy = 0; int n = 0;
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                    if (ZoneAt(x, y) != Zone.None || (buildingAt[Idx(x, y)] != 0 && x > 3)) { sx += x; sy += y; n++; }
            if (n > 0) { cx = (float)(sx / n); cy = (float)(sy / n); return; }
            cx = StartX >= 0 ? StartX : Cols / 2f; cy = StartY >= 0 ? StartY : Rows / 2f;
        }

        bool ConnectedFootprint(Building b)
        {
            int ax, ay;
            if (!AccessTile(b, out ax, out ay)) return false;
            return mainComp >= 0 && roadComp[ax + ay * Cols] == mainComp;
        }

        /// <summary>Is there a lot by a road on the highway's network where one of these could go.</summary>
        public bool HasLot(BuildingType t) { int x, y; return FindLot(t, out x, out y); }

        public int ZonedFree(Zone z)
        {
            int n = 0;
            for (int y = 0; y < Rows; y++) for (int x = 0; x < Cols; x++) if (ZoneAt(x, y) == z && buildingAt[Idx(x, y)] == 0) n++;
            return n;
        }

        /// <summary>Demand for each zone, -1..1, for the meters in the HUD.</summary>
        public float[] ZoneDemand = new float[5];

        /// <summary>Puts up a new building on the best free lot of its zone, for a founding family or a property firm.</summary>
        Building Develop(BuildingType t, Household founder, Building firm)
        {
            int x, y;
            var d = Defs.Get(t);
            if (!FindLot(t, out x, out y)) { Wants[(int)t] += 0.5f; LotShort[(int)d.Zone] = true; DevNote("no lot for " + t); return null; }
            if (founder == null && firm == null) return null;
            var b = AddBuilding(t, x, y, FindFacing(x, y, d.W, d.H), false);
            if (firm != null)
            {
                FirmOwns(b, firm);
                b.EquityLeft = d.StartupCost * (t == BuildingType.House ? 0.05 : 0.15);
                if (t == BuildingType.House) b.ForSale = true;
            }
            else
            {
                FamilyOwns(b, founder);
                b.Brand = BrandFor(b);
                b.EquityLeft = Math.Max(d.StartupCost * FoundersShare(t), Math.Min(Math.Max(0, founder.Cash - 8000), d.StartupCost * 0.3));
            }
            b.Closed = d.Homes == 0;
            StartConstruction(b, founder);
            WorkStarted += d.Work;
            return b;
        }
        [NonSerialized] public bool[] LotShort = new bool[5];
        [NonSerialized] public BuildingType NoInvestor = BuildingType.FreightTerminal;

        // ================================================================== is it worth opening?
        /// <summary>What one sale or visit is worth to a shop of this type, for turning unmet demand into money.</summary>
        static double SpendPerWant(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.GroceryStore: return 300;
                case BuildingType.Restaurant: return 25;
                case BuildingType.Cafe: return 6;
                case BuildingType.Bar: return 16;
                case BuildingType.Gym: return 15;
                case BuildingType.HairSalon: return Defs.Get(t).ServicePrice / 0.4;
                case BuildingType.AutoRepair: return 250;   // many put a service off, or can't get there, once there's a garage
                case BuildingType.ClothingStore: return 68 / 0.6;
                case BuildingType.ElectronicsStore: return 880 / 1.2;   // a want counts each trip until they find one out of town
                case BuildingType.FurnitureStore: return 690 / 1.2;   // as for electronics
                case BuildingType.GasStation: return 150 / 0.6;
                case BuildingType.CarDealer: return Balance.CarPrice / 2.5;   // counted on every try, bought or not
                default: return 0;
            }
        }

        /// <summary>The share of each sale left after stock and supplies.</summary>
        static double Margin(BuildingType t)
        {
            var d = Defs.Get(t);
            if (d.Sells.HasValue) { int g = (int)d.Sells.Value; return (Defs.Retail[g] - Defs.Wholesale[g]) / Defs.Retail[g]; }
            if (d.Serves != Service.None) return d.Input.HasValue ? 1 - d.InputPer * Defs.Wholesale[(int)d.Input.Value] / d.ServicePrice : 1;
            return 1;
        }

        /// <summary>What it would cost a month to run one, with just the staff that much business needs, paying off the building.</summary>
        double RunningCost(BuildingType t, double revenue)
        {
            var d = Defs.Get(t);
            double unit = d.Sells.HasValue ? Defs.Retail[(int)d.Sells.Value] : d.Serves != Service.None ? d.ServicePrice : 1;
            double perWorker = Math.Max(0.01, d.Output * Balance.DaysPerMonth);
            int crew = d.Output > 0 ? (int)Math.Round(revenue / unit / perWorker + 0.3) : 3;
            crew = Math.Max(1, Math.Min(d.TotalJobs, crew));
            double wage = 0; int n = 0;
            foreach (var j in d.Jobs) for (int k = 0; k < j.Count && n < crew; k++, n++) wage += Math.Max(Policy.MinWage, Defs.Get(j.Role).Wage * 1.12);   // with a few years' experience
            return wage + d.TotalJobs * Balance.UtilitiesJob + d.Value * (Balance.Maintenance + Policy.PropertyTax) / 12
                   + Annuity(d.StartupCost * (1 - FoundersShare(t)), Balance.PropertyRate, Balance.PropertyMonths);
        }

        /// <summary>Would a new plant pay its way fully staffed: what it makes at the price it would get (shops here for the food
        /// plant when they're importing groceries, otherwise the world price less freight), against wages, upkeep and its loan.</summary>
        bool PlantPays(BuildingType t)
        {
            var d = Defs.Get(t);
            if (!d.Makes.HasValue) return true;
            int g = (int)d.Makes.Value;
            bool local = t == BuildingType.FoodPlant && LastImports[(int)Good.Groceries] > 15000 || t == BuildingType.Farm && LastImports[(int)Good.Crops] > 8000;
            double price = local ? Defs.Wholesale[g] * (1 - Balance.FreightLocal) : World(d.Makes.Value) * (1 - Balance.FreightOutside);
            double units = d.TotalJobs * d.Output * Balance.DaysPerMonth * (t == BuildingType.Farm ? 0.95 : 1);
            double input = d.Input.HasValue ? d.InputPer * ImportPrice(d.Input.Value) * (1 + Balance.FreightOutside) : 0;
            double margin = units * (price - input);
            // at what the plants here are paying now, not the textbook rate
            var plants = Buildings.Where(b => b.Open && b.Def.Cat == Category.Industrial && b.Def.Makes.HasValue).ToList();
            double going = plants.Count > 0 ? Math.Max(1, plants.Average(b => b.WageMult)) : 1;
            double cost = 0; foreach (var j in d.Jobs) cost += j.Count * Math.Max(Policy.MinWage, Defs.Get(j.Role).Wage * going);
            cost += d.TotalJobs * Balance.UtilitiesJob + d.Value * (Balance.Maintenance + Policy.PropertyTax) / 12
                    + Annuity(d.StartupCost * (1 - FoundersShare(t)), Balance.PropertyRate, Balance.PropertyMonths);
            return margin >= cost * 1.1;
        }

        /// <summary>Would a new one pay its way on this much custom, with something to spare?</summary>
        bool PaysItsWay(BuildingType t, double revenue, double spare = 1.08)
        {
            return revenue > 0 && revenue * Margin(t) >= RunningCost(t, revenue) * spare;
        }

        /// <summary>A new one only after the case for it has held a few months, and not within a year of the last one failing
        /// unless the case is a strong one.</summary>
        [NonSerialized] public List<string> DevNotes;
        void DevNote(string s) { if (DevNotes != null) DevNotes.Add(s); }

        bool ReadyToOpen(BuildingType t, bool caseHolds, int months, bool strong)
        {
            int i = (int)t;
            if (caseHolds) DevNote(t + " case " + (EntryMonths[i] + 1) + "/" + months + (Cooling(t, strong) ? " (failed recently)" : ""));
            if (!caseHolds) { EntryMonths[i] = 0; return false; }
            EntryMonths[i]++;
            if (EntryMonths[i] < months) return false;
            return !Cooling(t, strong);
        }

        /// <summary>Nobody tries again within a year of one going under, unless the case is strong; and after two in a few years,
        /// not for two years whatever the case.</summary>
        bool Cooling(BuildingType t, bool strong)
        {
            int i = (int)t, y = Balance.DaysPerYear;
            if (Day - PrevFailDay[i] < y * 3 && Day - LastFailDay[i] < y * 2) return true;
            return Day - LastFailDay[i] < y && !strong;
        }

        // ================================================================== the month's building decisions
        void Developers()
        {
            for (int i = 0; i < LotShort.Length; i++) LotShort[i] = false;
            NoInvestor = BuildingType.FreightTerminal;
            for (int i = 0; i < WantAvg.Length; i++) WantAvg[i] = WantAvg[i] * 0.55f + LastWants[i] * 0.45f;
            int pop = Population;
            int building = Buildings.Count(b => !b.Built && b.Owner != Owner.Town);   // public works don't hold up private building
            int buildCap = 6 + Buildings.Count(b => b.Type == BuildingType.Construction && b.Open) * 5;
            // ---------------------------------------------------------------- homes, built by the property firms
            int free = VacantUnits() + Buildings.Count(b => b.ForSale && b.Households.Count == 0);
            int coming = Buildings.Where(b => !b.Built && b.Def.Homes > 0).Sum(b => b.Def.Homes);
            int openings = Openings().Sum(o => o.Count);
            // and when homes are dear, building them pays: developers build ahead of the queue
            float dear = Math.Max(0, (HousingIndex - 1.25f) / 0.25f);
            int wantHomes = (int)(LastWants[(int)BuildingType.House] + openings * 0.6f + pop * (0.03f + 0.015f * dear));
            float resDemand = (wantHomes - free - coming) / (float)Math.Max(6, pop / 12);
            ZoneDemand[(int)Zone.Residential] = Math.Max(-1, Math.Min(1, resDemand));
            int gap = wantHomes - free - coming;
            bool anyFirm = Buildings.Any(b => b.Type == BuildingType.Realty && (b.Open || !b.Built));
            bool firstFirm = !anyFirm && gap > 0 && pop >= 25 && HasLot(BuildingType.House);   // nobody builds homes here yet: a family starts a property firm
            while (gap > 0 && building < buildCap)
            {
                Building b = null;
                if (gap >= 7 && Chance(0.6f))
                {
                    var firm = HousingFirm(BuildingType.Apartments);
                    if (firm != null) b = Develop(BuildingType.Apartments, null, firm);
                }
                if (b == null)
                {
                    var firm = HousingFirm(BuildingType.House);
                    if (firm != null) b = Develop(BuildingType.House, null, firm);
                }
                if (b == null) break;
                gap -= b.Def.Homes; building++;
            }
            // nobody here building homes yet: houses for the families who want to move in
            if (HousingFirm(BuildingType.House) == null) HousesForSettlers(ref building, buildCap);
            // another property firm when the ones here can't keep up and are doing well
            var firms = PropertyFirms();
            bool stretched = firms.Count > 0 && gap > 4 && HasLot(BuildingType.House) && firms.All(f => f.LastProfit > 0 && Portfolio(f).Sum(o => o.Def.Homes) > 60);
            if (ReadyToOpen(BuildingType.Realty, anyFirm ? stretched : firstFirm, anyFirm ? 3 : 2, false) && building < buildCap)
            {
                var f = Founder(BuildingType.Realty);
                if (f != null && Expand(BuildingType.Realty, f) != null) { building++; EntryMonths[(int)BuildingType.Realty] = 0; }
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
                if (same.Any(b => !b.Built)) continue;
                bool caseHolds, strong = false;
                if (t == BuildingType.Bank) caseHolds = open.Count == 0;
                else if (open.Count == 0)
                {
                    // nobody sells it here: is there enough custom going out of town to keep one going?
                    double rev = WantAvg[(int)t] * SpendPerWant(t);
                    caseHolds = PaysItsWay(t, rev);
                    strong = PaysItsWay(t, rev, 1.8);
                }
                else
                {
                    // the ones here are full, all making money and still turning people away: room for one more
                    float lost = open.Sum(b => b.LastLost), served = open.Sum(b => b.LastCustomers);
                    bool full = open.All(b => b.Staffing >= 0.95f && b.LastUse > 0.85f);
                    bool earning = open.All(b => b.LastProfit > 0 && b.LossMonths == 0);
                    bool room = open.Count < 1 + pop / PopPer(t);
                    double price = open.Average(b => b.Price) * (t == BuildingType.GroceryStore ? 4 : 1);
                    double rev = lost * price;
                    caseHolds = full && earning && room && lost > Math.Max(8, served * 0.15f) && PaysItsWay(t, rev);
                }
                if (caseHolds) comDemand += open.Count == 0 ? 0.3f : 0.2f;
                if (!ReadyToOpen(t, caseHolds, open.Count == 0 ? 2 : 3, strong)) continue;
                if (building >= buildCap) continue;
                var founder = Founder(t);
                if (founder == null) { NoInvestor = t; DevNote("no founder for " + t); continue; }
                if (Expand(t, founder) != null) { building++; EntryMonths[(int)t] = 0; }
            }
            ZoneDemand[(int)Zone.Commercial] = Math.Min(1, comDemand);
            // ---------------------------------------------------------------- work: offices and industry follow the workforce
            // people looking for work, and those driving out of town for it who'd rather work here
            var looking = People.Where(c => (c.InLaborForce && c.WorkId < 0 && c.JoblessDays >= 2) || (Commutes(c) && c.Wage < 3700)).ToList();
            int grads = looking.Count(c => c.Edu >= Edu.Degree) + People.Count(c => c.Edu >= Edu.Degree && c.WorkId >= 0 && Defs.Get(c.Role).Needs < Edu.HighSchool);
            int school = looking.Count(c => c.Edu == Edu.HighSchool), trade = looking.Count(c => c.Edu == Edu.Trade), basic = looking.Count(c => c.Edu == Edu.None);
            float indDemand = (looking.Count - pop * 0.04f) / Math.Max(6f, pop / 20f);
            ZoneDemand[(int)Zone.Industrial] = Math.Max(-1, Math.Min(1, indDemand));
            bool officePending = Buildings.Any(b => b.Type == BuildingType.Office && !b.Built);
            bool officeCase = !officePending && (grads >= 5 || (grads >= 3 && school >= 4)) && Buildings.Where(b => b.Type == BuildingType.Office && b.Open).All(b => b.LastProfit > 0);
            if (ReadyToOpen(BuildingType.Office, officeCase, 2, grads >= 10) && building < buildCap)
            {
                var founder = Founder(BuildingType.Office);
                if (founder == null) NoInvestor = BuildingType.Office;
                else if (Expand(BuildingType.Office, founder) != null) { building++; EntryMonths[(int)BuildingType.Office] = 0; }
            }
            bool factoryPending = Buildings.Any(b => !b.Built && b.Def.Cat == Category.Industrial);
            bool factoryCase = !factoryPending && looking.Count >= 7 + pop / 40;
            FactoryMonths = factoryCase ? FactoryMonths + 1 : 0;
            if (FactoryMonths >= 2 && building < buildCap)
            {
                // the kind of plant that would hire the people who are looking, leaving out any that failed here this past year
                var kinds = new List<BuildingType>();
                if (grads >= 2 && trade >= 2) kinds.Add(BuildingType.ElectronicsPlant);
                if (basic > school && basic >= 4) { kinds.Add(BuildingType.FoodPlant); if (ZonedFree(Zone.Farmland) >= 9) kinds.Add(BuildingType.Farm); }
                if (trade >= 2) kinds.Add(BuildingType.FurnitureWorkshop);
                kinds.Add(BuildingType.GarmentFactory);
                bool strong = looking.Count >= 20 + pop / 20;
                // and any kind whose plants here are already struggling, short-handed or only scraping by
                kinds.RemoveAll(k => Cooling(k, strong) || !PlantPays(k) || Buildings.Count(b => b.Type == k && (b.Open || !b.Built)) >= 2 + pop / 200 ||
                                     looking.Count < Defs.Get(k).TotalJobs || pop < 60 ||
                                     Buildings.Any(b => b.Type == k && b.Open && (b.LossMonths > 0 || b.Staffing < 0.9f || b.LastProfit < b.LastRevenue * 0.04)));
                // something the town doesn't make yet before more of the same
                if (kinds.Any(k => !Buildings.Any(b => b.Type == k && (b.Open || !b.Built))))
                    kinds.RemoveAll(k => Buildings.Any(b => b.Type == k && (b.Open || !b.Built)));
                DevNote("factory: looking " + looking.Count + " basic " + basic + " school " + school + " trade " + trade + " grads " + grads + " -> " + string.Join(",", kinds));
                if (kinds.Count > 0)
                {
                    var t = kinds[RandInt(0, kinds.Count)];
                    var founder = Founder(t);
                    if (founder == null) { NoInvestor = t; DevNote("no founder for " + t); }
                    else if (Expand(t, founder) != null) { building++; FactoryMonths = 0; }
                }
            }
            // food: a plant when the stores import groceries, farms when the plant imports crops
            double grocImports = LastImports[(int)Good.Groceries], cropImports = LastImports[(int)Good.Crops];
            ZoneDemand[(int)Zone.Farmland] = Math.Max(-1, Math.Min(1, (float)(cropImports / 30000.0)));
            if (building < buildCap && !factoryPending)
            {
                bool plantCase = grocImports > 25000 && Buildings.Where(b => b.Type == BuildingType.FoodPlant && b.Open).All(b => b.LastUse > 0.8f) && PlantPays(BuildingType.FoodPlant);
                if (ReadyToOpen(BuildingType.FoodPlant, plantCase, 3, grocImports > 60000))
                {
                    var founder = Founder(BuildingType.FoodPlant);
                    if (founder == null) NoInvestor = BuildingType.FoodPlant;
                    else if (Expand(BuildingType.FoodPlant, founder) != null) { building++; EntryMonths[(int)BuildingType.FoodPlant] = 0; }
                }
                var farms = Buildings.Where(b => b.Type == BuildingType.Farm && b.Open).ToList();
                bool farmCase = cropImports > 12000 && !Buildings.Any(b => b.Type == BuildingType.Farm && !b.Built) &&
                                farms.All(b => b.LossMonths < 4) && (farms.Count == 0 || farms.Average(b => b.Cash) > 0) && PlantPays(BuildingType.Farm);
                if (ReadyToOpen(BuildingType.Farm, farmCase, 2, cropImports > 30000) && building < buildCap)
                {
                    var founder = Founder(BuildingType.Farm);
                    if (founder == null) NoInvestor = BuildingType.Farm;
                    else if (Expand(BuildingType.Farm, founder) != null) { building++; EntryMonths[(int)BuildingType.Farm] = 0; }
                }
            }
            // builders: one if there's none; another only when the ones here are flat out with months of work queued
            var builders = Buildings.Where(b => b.Type == BuildingType.Construction && b.Open).ToList();
            bool builderPending = Buildings.Any(b => b.Type == BuildingType.Construction && !b.Built);
            // the steady work: homes and businesses started each month, over the last half year or so. A rush of public works
            // is a one-off and doesn't keep a second firm going
            WorkRate = WorkRate * 0.85f + WorkStarted * 0.15f; WorkStarted = 0;
            float backlog = Buildings.Where(b => !b.Built && b.Owner != Owner.Town).Sum(b => b.Def.Work - b.Progress);
            bool builderCase;
            if (builders.Count == 0) builderCase = !builderPending && (backlog > 0 || pop >= 120);
            else
            {
                float crews = builders.Sum(b => b.Def.TotalJobs) * Balance.DaysPerMonth;
                builderCase = !builderPending && WorkRate > crews * 1.2f && backlog > crews * 6 && builders.All(b => b.Staffing >= 0.95f && b.LastProfit > 0 && b.LossMonths == 0);
            }
            if (ReadyToOpen(BuildingType.Construction, builderCase, builders.Count == 0 ? 1 : 4, builders.Count == 0))
            {
                var founder = Founder(BuildingType.Construction);
                if (founder == null) NoInvestor = BuildingType.Construction;
                else if (Expand(BuildingType.Construction, founder) != null) EntryMonths[(int)BuildingType.Construction] = 0;
            }
            // hauliers: when outside trucking costs enough that a depot here would pay its way
            bool haulCase = pop >= Defs.Get(BuildingType.Trucking).MinPop && !Buildings.Any(b => b.Type == BuildingType.Trucking && (b.Open || !b.Built)) &&
                            PaysItsWay(BuildingType.Trucking, LastFreightOutside * Balance.FreightLocal / Balance.FreightOutside, 1.4);
            if (ReadyToOpen(BuildingType.Trucking, haulCase, 4, false))
            {
                var founder = Founder(BuildingType.Trucking);
                if (founder == null) NoInvestor = BuildingType.Trucking;
                else if (Expand(BuildingType.Trucking, founder) != null) EntryMonths[(int)BuildingType.Trucking] = 0;
            }
        }
        public int FactoryMonths;            // months in a row there have been enough people looking for work to staff a plant
        public float WorkStarted, WorkRate;  // labour-days of private building started this month, and the running average
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

        static bool BigFirm(BuildingType t)
        {
            return Defs.Get(t).StartupCost > 500000 || t == BuildingType.Bank || t == BuildingType.Realty || t == BuildingType.Construction || t == BuildingType.Trucking;
        }

        /// <summary>A family here to start this business: someone who can work in it and has the drive and the savings for the
        /// founders' share, or a family with money to put to work (for the bigger firms, or one that already runs a business).</summary>
        /// <summary>The founders' share of the cost: a smaller share of a big building the bank can lend against.</summary>
        static double FoundersShare(BuildingType t) { return BigFirm(t) ? Balance.Equity * 0.5 : Balance.Equity; }

        /// <summary>Who starts it: a family here; failing that, while the town is young, or for the first builder or property
        /// firm, a family who moves here to start it, if there's a home for them.</summary>
        Household Founder(BuildingType t)
        {
            var h = Entrepreneur(t);
            if (h != null) return h;
            bool first = (t == BuildingType.Construction || t == BuildingType.Realty) && !Buildings.Any(b => b.Type == t && (b.Open || !b.Built));
            if (!(first || Population < 250) || !Policy.Immigration || Terminal == null || FounderArrived == Day) return null;
            var d = Defs.Get(t);
            int lx, ly;
            if (!Buildings.Any(b => b.Type == t && b.Closed && b.Built && Connected(b)) && !FindLot(t, out lx, out ly)) return null;
            // they'll run it: schooled for its best-paid job
            var edu = d.Jobs.OrderByDescending(j => Defs.Get(j.Role).Wage).Select(j => Defs.Get(j.Role).Needs).FirstOrDefault();
            if (edu == Edu.Medical) edu = Edu.Degree;
            var home = Buildings.Where(b => b.ForSale && b.Built && b.Households.Count == 0 && Connected(b)).OrderBy(b => b.Value).FirstOrDefault();
            bool buys = home != null;
            if (!buys) home = FindHome(3, 6000, 0.45);
            if (home == null) { DevNote("no home for a founder of " + t); return null; }
            double need = Math.Max(12000, d.StartupCost * FoundersShare(t));
            h = Newcomers(edu, Chance(0.75f));
            h.Cars = Math.Max(1, h.Cars); h.Fuel = Math.Max(h.Fuel, h.Cars);
            h.Cash += need + 25000 + Rand() * 40000 + (buys ? home.Value * 0.14 : 0);
            foreach (var m in Members(h)) if (!m.IsChild) m.Ambition = Math.Max(m.Ambition, 0.6f);
            if (buys) BuyHome(h, home, true); else MoveIn(h, home, false);
            ArrivalsMonth += h.Members.Count;
            FounderArrived = Day;
            var a = Members(h).First();
            AddLog("The " + h.Name + " family moved to town to start " + (("aeiou".IndexOf(char.ToLower(d.Name[0])) >= 0) ? "an " : "a ") + d.Name.ToLower() + ".", LogKind.Good, a.Id, true);
            Note(a, "Moved to " + Name + " to start a business.");
            return h;
        }
        [NonSerialized] int FounderArrived = -1;

        /// <summary>Is there anyone in town who could work there: looking for work, or driving out of town for it.</summary>
        bool CanStaff(BuildingType t)
        {
            var d = Defs.Get(t);
            return People.Any(c => ((c.InLaborForce && c.WorkId < 0) || Commutes(c)) && d.Jobs.Any(j => Defs.Qualifies(c.Edu, Defs.Get(j.Role).Needs)));
        }

        Household Entrepreneur(BuildingType t)
        {
            var d = Defs.Get(t);
            double need = Math.Max(12000, d.StartupCost * FoundersShare(t));
            bool big = BigFirm(t);
            foreach (var h in Households.OrderByDescending(x => x.Cash))
            {
                if (h.Cash < need + 10000) break;
                int owned = Buildings.Count(b => b.Owner == Owner.Household && b.OwnerId == h.Id && IsBusiness(b));
                bool runs = owned == 0 && Members(h).Any(m => !m.IsChild && !m.Retired && m.Age <= 60 && !m.Owner && m.Ambition >= 0.55f &&
                                                           d.Jobs.Any(j => Defs.Qualifies(m.Edu, Defs.Get(j.Role).Needs)));
                bool invests = owned < 3 && h.Cash >= need * 2 + 30000 && (big || owned > 0) && CanStaff(t);
                if ((runs || invests) && Chance(0.6f)) return h;
            }
            return null;
        }

        /// <summary>A founder reopens an empty building of this kind (bought from the bank), or builds a new one.</summary>
        Building Expand(BuildingType t, Household founder)
        {
            if (founder == null) return null;
            var shut = Buildings.FirstOrDefault(b => b.Type == t && b.Closed && b.Built && Connected(b));
            if (shut == null) return Develop(t, founder, null);
            double price = shut.Value * 0.6;
            double down = Math.Min(price, Math.Max(price * Balance.Equity, Math.Min(Math.Max(0, founder.Cash - 8000), price * 0.4)));
            HouseholdPays(founder, down, HouseholdBook.Goods);
            SellerGets(shut, price, false);
            FamilyOwns(shut, founder);
            shut.Brand = BrandFor(shut);
            if (price > down) PropertyLoan(shut, founder, shut, price - down, false);
            OpenBusiness(shut);
            return shut;
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
