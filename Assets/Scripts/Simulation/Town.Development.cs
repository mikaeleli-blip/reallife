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
            // the engineers who lay it out
            double eng = amount * 0.06; Fee(Practice.Engineering, eng); amount -= eng;
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
            // the local builder with the least on its books; when they're all booked up for months, contractors from
            // outside take it on (and charge more), which is what brings another builder to town
            Building best = null; float least = float.MaxValue;
            foreach (var c in Buildings)
            {
                if (c.Type != BuildingType.Construction || !c.Open || !Connected(c)) continue;
                float backlog = 0; foreach (var o in Buildings) if (!o.Built && o.ContractorId == c.Id) backlog += o.Def.Work - o.Progress;
                float months = backlog / (Math.Max(4, c.Employees.Count) * Balance.DaysPerMonth);
                if (months < least) { least = months; best = c; }
            }
            if (best != null && least > 3 && b.Owner != Owner.Town) best = null;
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
                    double net = DesignCut(b, first);
                    if (con != null)
                    {
                        con.Cash += net; con.Revenue += net;
                        double mat = d.Materials;
                        double fee = Freight(mat, -1, con.Id, Good.Materials);
                        con.Cash -= mat + fee; con.Supplies += mat + fee;
                        Trade.Imports += mat; ImportsByGood[(int)Good.Materials] += mat;
                    }
                    else Trade.Imports += net;
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
                else work = Math.Min(d.Work - b.Progress, 6f);
                float before = b.Progress;
                b.Progress += work;
                double pay = d.Work > 0 ? b.ContractLeft * (work / Math.Max(0.01f, d.Work - before)) : b.ContractLeft;
                pay = Math.Min(pay, b.ContractLeft);
                ClientPays(b, pay);
                double paid = DesignCut(b, pay);
                if (con != null) { con.Cash += paid; con.Revenue += paid; con.Customers++; }
                else Trade.Imports += paid;
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
                if (firm != null && b.Type == BuildingType.Apartments) firm.LossMonths = 0;   // its flats start bringing rent in
                if (b.Type == BuildingType.House && b.ForSale) AddLog((firm != null ? Title(firm) + " put a new house" : "A new house went") + " on the market for " + Money(b.Value) + ".", LogKind.Info, b.Id);
                else AddLog((firm != null ? Title(firm) + "'s" : "A") + " new " + d.Name.ToLower() + " is finished: " + d.Homes + " homes to let.", LogKind.Info, b.Id);
                return;
            }
            OpenBusiness(b);
        }

        void OpenBusiness(Building b)
        {
            var d = b.Def;
            // a builder starts with a small crew and takes on hands as work comes in
            // (an office starts small and takes people on as its clients keep it busy)
            b.Closed = false; b.ClosedDays = 0; b.Staffing = b.Type == BuildingType.Construction ? 0.3f : d.Cat == Category.Industrial ? 0.75f : IsOffice(b.Type) ? 0.25f : 0.5f; b.WageMult = 1; b.LossMonths = 0; b.BuiltDay = Day;
            if (d.Sells.HasValue) b.Price = d.Chain != null ? ChainPrice(b.Type) : Defs.Retail[(int)d.Sells.Value] * d.PriceFactor;
            else if (d.Serves != Service.None) b.Price = d.ServicePrice;
            else if (d.Makes.HasValue) b.Price = Defs.Wholesale[(int)d.Makes.Value];
            // enough to see it through to the first sales: longer for a property firm waiting on its first houses
            // and its first stock: a shop's shelves, or a month of a plant's materials
            double stock = d.Sells.HasValue ? d.StockCap * Defs.Wholesale[(int)d.Sells.Value] * 0.4 : SuppliesPerMonth(b);
            double working = PayAndBills(b) * (b.Type == BuildingType.Realty ? 6 : b.Type == BuildingType.Construction ? 3.5 : 2.5) + stock + OpeningLegal + OpeningDesign;
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
            // the lawyer who draws up the papers, and the sign and the logo
            b.Cash -= OpeningLegal + OpeningDesign; b.OtherCosts += OpeningLegal + OpeningDesign;
            Fee(Practice.Legal, OpeningLegal); Fee(Practice.Design, OpeningDesign);
            if (b.Type == BuildingType.Realty) PlanFirmStaff(b);
            MapVersion++;
        }

        /// <summary>The architect's and the engineer's fees come out of each payment for a building as the work goes on; the
        /// builder gets the rest.</summary>
        double DesignCut(Building b, double amount)
        {
            double arch = amount * ArchitectShare(b), eng = amount * EngineerShare(b);
            Fee(Practice.Architecture, arch); Fee(Practice.Engineering, eng);
            return amount - arch - eng;
        }

        /// <summary>The architect's share of what a building costs: less for a house or a block of flats built to a plan.</summary>
        static double ArchitectShare(Building b) { return b.Def.Homes > 0 ? 0.02 : 0.05; }
        /// <summary>The engineer's: most for plants and public works.</summary>
        static double EngineerShare(Building b) { var c = b.Def.Cat; return c == Category.Industrial || c == Category.Public ? 0.03 : 0.005; }

        // ================================================================== developers
        /// <summary>Where a building of this type could go on land zoned for it: the most central free spot.</summary>
        bool FindLot(BuildingType t, out int bx, out int by)
        {
            var d = Defs.Get(t);
            bx = by = -1; float best = float.MaxValue;
            float cx, cy; TownCentre(out cx, out cy);
            // a chain's next store goes where it would be the nearest of its stores for a good many families: another part
            // of town, not next door to its first
            var sister = d.Chain != null ? Buildings.Where(b => b.Type == t && (b.Open || !b.Built)).ToList() : null;
            if (sister != null && sister.Count == 0) sister = null;
            var homes = sister != null ? Households.Select(HomeOf).Where(h => h != null).ToList() : null;
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
                    if (sister != null)
                    {
                        int near = Catchment(homes, sister, x + d.W / 2f, y + d.H / 2f);
                        if (near < homes.Count * ChainCatchment) continue;
                        score -= near * 0.3f;
                    }
                    if (score < best) { best = score; bx = x; by = y; }
                }
            return bx >= 0;
        }

        const float ChainCatchment = 0.2f;   // the share of the town's families a chain's next store must be nearest for

        /// <summary>Would a chain store here be the nearest of the chain's stores for enough of the town's families.</summary>
        bool GoodChainSpot(BuildingType t, float x, float y)
        {
            var sister = Buildings.Where(b => b.Type == t && (b.Open || !b.Built)).ToList();
            if (sister.Count == 0) return true;
            var homes = Households.Select(HomeOf).Where(h => h != null).ToList();
            return Catchment(homes, sister, x, y) >= homes.Count * ChainCatchment;
        }

        /// <summary>The homes that would have a store here nearer (by a few tiles) than any of the chain's others.</summary>
        static int Catchment(List<Building> homes, List<Building> stores, float x, float y)
        {
            int n = 0;
            foreach (var h in homes)
            {
                float d = Math.Abs(h.CenterX - x) + Math.Abs(h.CenterY - y) + 3;
                bool nearer = true;
                foreach (var o in stores) if (Math.Abs(h.CenterX - o.CenterX) + Math.Abs(h.CenterY - o.CenterY) <= d) { nearer = false; break; }
                if (nearer) n++;
            }
            return n;
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

        /// <summary>A block of flats the bank puts up itself, one at a time, when renters are waiting and no property firm can.</summary>
        Building BankFlats()
        {
            if (Buildings.Any(b => !b.Built && b.Type == BuildingType.Apartments && BankHeld(b))) return null;
            int x, y;
            if (!FindLot(BuildingType.Apartments, out x, out y)) return null;
            var b2 = AddBuilding(BuildingType.Apartments, x, y, FindFacing(x, y, 2, 2), false);
            BankTakes(b2);
            StartConstruction(b2, null);
            WorkStarted += b2.Def.Work;
            AddLog("No property firm here can take on a block of flats, so the bank is building one to let.", LogKind.Info, b2.Id);
            return b2;
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
        public float[] ZoneDemand = new float[6];

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
        [NonSerialized] public bool[] LotShort = new bool[6];
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
                default: return Defs.Get(t).Ticket;
            }
        }

        /// <summary>The share of each sale left after stock and supplies.</summary>
        static double Margin(BuildingType t)
        {
            var d = Defs.Get(t);
            if (d.Sells.HasValue) { int g = (int)d.Sells.Value; double p = Defs.Retail[g] * d.PriceFactor; return (p - Defs.Wholesale[g] * d.BulkBuy) / p; }
            if (d.Serves != Service.None) { double c = 0; foreach (var i in d.Inputs) c += i.Per * Defs.Wholesale[(int)i.Good]; return 1 - c / d.ServicePrice; }
            return 1;
        }

        /// <summary>What it would cost a month to run one, with just the staff that much business needs, paying off the building.</summary>
        double RunningCost(BuildingType t, double revenue)
        {
            var d = Defs.Get(t);
            double unit = d.Sells.HasValue ? Defs.Retail[(int)d.Sells.Value] * d.PriceFactor : d.Serves != Service.None ? d.ServicePrice : 1;
            double perWorker = Math.Max(0.01, d.Output * Balance.DaysPerMonth);
            int crew = d.Output > 0 ? (int)Math.Round(revenue / unit / perWorker + 0.3) : 3;
            crew = Math.Max(1, Math.Min(d.TotalJobs, crew));
            double wage = 0; int n = 0;
            foreach (var j in d.Jobs) for (int k = 0; k < j.Count && n < crew; k++, n++) wage += Math.Max(Policy.MinWage, Defs.Get(j.Role).Wage * 1.12);   // with a few years' experience
            return wage + d.TotalJobs * Balance.UtilitiesJob + d.Value * (Balance.Maintenance + Policy.PropertyTax) / 12
                   + Annuity(d.StartupCost * (1 - FoundersShare(t)), Balance.PropertyRate, Balance.PropertyMonths) + OfficeBills(t, revenue, crew);
        }

        /// <summary>Would a new plant pay its way fully staffed: what it makes, sold to the shops and plants here that now import
        /// it (at the wholesale price) and the rest out of town (at the world price less freight, and less again if the town
        /// already ships a lot of it), less what its inputs cost, against wages, upkeep and its loan. Returns its margin over
        /// costs (above 1.1 it pays).</summary>
        double PlantCase(BuildingType t)
        {
            var d = Defs.Get(t);
            if (!d.Makes.HasValue) return 2;
            var good = d.Makes.Value;
            int g = (int)good;
            double units = d.TotalJobs * d.Output * Balance.DaysPerMonth * (IsFarm(t) ? 0.95 : 1);
            // what the businesses here bring in from out of town now, in units: a plant here would take its place
            double local = Math.Min(units, (LastImports != null ? LastImports[g] : 0) / Math.Max(1, ImportPrice(good)));
            double exports = units - local;
            double lastOut = LastExports != null ? LastExports[g] : 0;
            double world = World(good) * (1 - Balance.FreightOutside) * ExportFactor(good, lastOut + exports * World(good));
            double revenue = local * Defs.Wholesale[g] * (1 - Balance.FreightLocal) + exports * world;
            double input = 0;
            foreach (var i in d.Inputs) input += i.Per * InputPrice(i.Good);
            double margin = revenue - units * input;
            // at what the plants here are paying now, not the textbook rate
            var plants = Buildings.Where(b => b.Open && b.Def.Cat == Category.Industrial && b.Def.Makes.HasValue).ToList();
            double going = plants.Count > 0 ? Math.Max(1, plants.Average(b => b.WageMult)) : 1;
            double cost = 0; foreach (var j in d.Jobs) cost += j.Count * Math.Max(Policy.MinWage, Defs.Get(j.Role).Wage * going);
            cost += d.TotalJobs * Balance.UtilitiesJob + d.Value * (Balance.Maintenance + Policy.PropertyTax) / 12
                    + Annuity(d.StartupCost * (1 - FoundersShare(t)), Balance.PropertyRate, Balance.PropertyMonths) + OfficeBills(t, revenue, d.TotalJobs);
            return margin / Math.Max(1, cost);
        }

        bool PlantPays(BuildingType t) { return PlantCase(t) >= 1.1; }

        /// <summary>What a plant pays for an input: from the cheapest maker here, or imported.</summary>
        double InputPrice(Good g)
        {
            double p = ImportPrice(g) * (1 + Balance.FreightOutside);
            foreach (var b in Buildings) if (b.Open && b.Def.Makes == g && b.Stock[(int)g] > 1) p = Math.Min(p, b.Price * (1 + Balance.FreightLocal));
            return p;
        }

        public static bool IsFarm(BuildingType t) { return Defs.Get(t).Zone == Zone.Farmland; }

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
            pros = new List<Prospect>();
            for (int i = 0; i < WantAvg.Length; i++) WantAvg[i] = WantAvg[i] * 0.55f + LastWants[i] * 0.45f;
            int pop = Population;
            int building = Buildings.Count(b => !b.Built && b.Owner != Owner.Town);   // public works don't hold up private building
            int buildCap = 8 + Buildings.Count(b => b.Type == BuildingType.Construction && b.Open) * 6;
            // ---------------------------------------------------------------- homes, built by the property firms
            // Two markets, each on its own zone: flats to let, for singles, young people leaving home and workers who come for a
            // job; and houses to sell, for families who can get a mortgage.
            int openings = Openings().Sum(o => o.Count);
            // and when homes are dear, building them pays: developers build ahead of the queue
            float dear = Math.Max(0, (HousingIndex - 1.25f) / 0.25f);
            int rentFree = VacantUnits(), saleFree = Buildings.Count(b => b.ForSale && b.Households.Count == 0);
            int flatsComing = Buildings.Where(b => !b.Built && b.Type == BuildingType.Apartments).Sum(b => b.Def.Homes);
            int housesComing = Buildings.Count(b => !b.Built && b.Type == BuildingType.House);
            int wantRent = (int)(LastWants[(int)BuildingType.Apartments] + openings * 0.6f + pop * (0.02f + 0.01f * dear));
            int wantBuy = (int)(LastWants[(int)BuildingType.House] + pop * (0.01f + 0.005f * dear));
            int rentGap = wantRent - rentFree - flatsComing, buyGap = wantBuy - saleFree - housesComing;
            bool flatLand = HasLot(BuildingType.Apartments), houseLand = HasLot(BuildingType.House);
            float scale = Math.Max(6, pop / 12);
            ZoneDemand[(int)Zone.Apartments] = Math.Max(-1, Math.Min(1, rentGap / scale));
            // with no land zoned for flats, renters end up in houses
            ZoneDemand[(int)Zone.Residential] = Math.Max(-1, Math.Min(1, (buyGap + (flatLand ? 0 : Math.Max(0, rentGap))) / scale));
            if (!flatLand && rentGap > 0) LotShort[(int)Zone.Apartments] = true;
            bool anyFirm = Buildings.Any(b => b.Type == BuildingType.Realty && (b.Open || !b.Built));
            // nobody builds homes here yet: a family starts a property firm
            bool firstFirm = !anyFirm && (buyGap > 0 || rentGap > 0) && pop >= 25 && (houseLand || flatLand);
            // a block of flats once enough renters are waiting; houses for those who'd buy, and for renters too when there's no
            // land for flats or no firm can take a block on
            bool flatsStuck = !flatLand;
            while (building < buildCap)
            {
                if (rentGap >= 5 && !flatsStuck)
                {
                    var firm = HousingFirm(BuildingType.Apartments);
                    var f = firm != null ? Develop(BuildingType.Apartments, null, firm) : null;
                    // no firm can take a block on: with enough renters waiting, the bank puts one up, lets it, and sells it to a
                    // firm when one can buy (BankSales)
                    if (f == null && firm == null && rentGap >= 8) f = BankFlats();
                    if (f != null) { rentGap -= f.Def.Homes; building++; continue; }
                    flatsStuck = true;
                    if (firm == null) DevNote("flats wanted (" + rentGap + ") but no firm can take a block on");
                }
                if (buyGap + (flatsStuck ? Math.Max(0, rentGap) : 0) <= 0) break;
                var hf = HousingFirm(BuildingType.House);
                var h = hf != null ? Develop(BuildingType.House, null, hf) : null;
                if (h == null) break;
                building++;
                if (buyGap > 0) buyGap--; else rentGap--;
            }
            // nobody here building homes yet: houses for the families who want to move in
            HousesForSettlers(ref building, buildCap);
            // another property firm when the ones here are doing well but can't take on what's wanted
            var firms = PropertyFirms();
            bool cantKeepUp = (flatLand && rentGap >= 5 && HousingFirm(BuildingType.Apartments) == null) || (houseLand && buyGap > 4 && HousingFirm(BuildingType.House) == null);
            bool stretched = firms.Count > 0 && cantKeepUp && firms.All(f => f.LastProfit > 0 && Portfolio(f).Sum(o => o.Def.Homes) > 60);
            if (ReadyToOpen(BuildingType.Realty, anyFirm ? stretched : firstFirm, anyFirm ? 3 : 2, false) && building < buildCap)
            {
                var f = Founder(BuildingType.Realty);
                if (f != null && Expand(BuildingType.Realty, f) != null) { building++; EntryMonths[(int)BuildingType.Realty] = 0; }
            }
            // ---------------------------------------------------------------- shops and services
            float comDemand = 0;
            // the car trade and the bank, each one kind of place
            foreach (var t in new[] { BuildingType.CarDealer, BuildingType.AutoRepair, BuildingType.Bank })
            {
                var d = Defs.Get(t);
                var same = Buildings.Where(b => b.Type == t).ToList();
                var open = same.Where(b => b.Open).ToList();
                double away = t == BuildingType.Bank ? 0 : SpendAway[(int)t];
                if (pop < d.MinPop) { Pro(d.Name, t, 0, open.Count, away, Outlook.TooSmall).MinPop = d.MinPop; continue; }
                if (same.Any(b => !b.Built)) { Pro(d.Name, t, 0, open.Count, away, Outlook.Building).Site = same.First(b => !b.Built).Id; continue; }
                bool strong = false, caseHolds;
                Prospect p;
                if (t == BuildingType.Bank) { caseHolds = open.Count == 0; p = Pro(d.Name, t, 0, open.Count, 0, caseHolds ? Outlook.Promising : Outlook.Enough); }
                else { caseHolds = TradeCase(t, t, open, pop, out strong, null); p = Pro(d.Name, t, 0, open.Count, caseAway, caseState); p.Need = caseNeed; p.Per = casePer; }
                if (caseHolds) comDemand += open.Count == 0 ? 0.3f : 0.2f;
                if (!Ready(p, t, caseHolds, open.Count == 0 ? 2 : 3, strong)) continue;
                if (building >= buildCap) { p.State = Outlook.BuildersBusy; continue; }
                var founder = Founder(t);
                if (founder == null) { p.State = Outlook.NoBacker; NoInvestor = t; DevNote("no founder for " + t); continue; }
                var nb = Expand(t, founder);
                if (nb != null) { building++; EntryMonths[(int)t] = 0; p.State = Outlook.Opening; p.Site = nb.Id; }
                else p.State = Outlook.NoLot;
            }
            // everything else by trade: meals out might be a pizzeria, a diner or a sushi bar; clothes a boutique or a thrift store.
            // The town gets what it hasn't got before more of the same.
            foreach (var m in Defs.Markets)
            {
                var kinds = m.Kinds.Where(t => pop >= Defs.Get(t).MinPop).ToList();
                var same = Buildings.Where(b => Array.IndexOf(m.Kinds, b.Type) >= 0).ToList();
                var open = same.Where(b => b.Open).ToList();
                if (kinds.Count == 0)
                {
                    var first = m.Kinds.OrderBy(k => Defs.Get(k).MinPop).First();
                    Pro(m.Name, first, 0, open.Count, MarketAway(m), Outlook.TooSmall).MinPop = Defs.Get(first).MinPop;
                    continue;
                }
                if (same.Any(b => !b.Built)) { var site = same.First(b => !b.Built); Pro(m.Name, site.Type, 0, open.Count, MarketAway(m), Outlook.Building).Site = site.Id; continue; }
                var t0 = m.Primary;
                bool strong;
                BuildingType pick;
                bool caseHolds = TradeCase(t0, BuildingType.FreightTerminal, open, pop, out strong, kinds, m, out pick);
                var p = Pro(m.Name, pick, 0, open.Count, caseAway, caseState); p.Need = caseNeed; p.Per = casePer;
                if (caseHolds) comDemand += open.Count == 0 ? 0.3f : 0.2f;
                if (!Ready(p, t0, caseHolds, open.Count == 0 ? 2 : 3, strong)) continue;
                if (building >= buildCap) { p.State = Outlook.BuildersBusy; continue; }
                // an empty shop of the trade the bank holds goes first, as it was; otherwise the kind picked
                var shut = Buildings.FirstOrDefault(b => Array.IndexOf(m.Kinds, b.Type) >= 0 && b.Closed && b.Built && Connected(b) && pop >= b.Def.MinPop);
                var kind = shut != null ? shut.Type : pick;
                p.Kind = kind;
                var founder = Founder(kind);
                if (founder == null) { p.State = Outlook.NoBacker; NoInvestor = kind; DevNote("no founder for " + kind); continue; }
                var nb = shut != null ? Reopen(shut, founder) : Develop(kind, founder, null);
                if (nb != null) { building++; EntryMonths[(int)t0] = 0; p.State = Outlook.Opening; p.Site = nb.Id; }
                else p.State = Outlook.NoLot;
            }
            Chains(ref building, buildCap);
            ZoneDemand[(int)Zone.Commercial] = Math.Min(1, comDemand);
            // ---------------------------------------------------------------- work: offices and industry follow the workforce
            // people looking for work, and those driving out of town for it who'd rather work here
            var looking = People.Where(c => (c.InLaborForce && c.WorkId < 0 && c.JoblessDays >= 2) || (Commutes(c) && c.Wage < 3700)).ToList();
            int grads = looking.Count(c => c.Edu >= Edu.Degree) + People.Count(c => c.Edu >= Edu.Degree && c.WorkId >= 0 && Defs.Get(c.Role).Needs < Edu.HighSchool);
            int school = looking.Count(c => c.Edu == Edu.HighSchool), trade = looking.Count(c => c.Edu == Edu.Trade), basic = looking.Count(c => c.Edu == Edu.None);
            float indDemand = (looking.Count - pop * 0.04f) / Math.Max(6f, pop / 20f);
            ZoneDemand[(int)Zone.Industrial] = Math.Max(-1, Math.Min(1, indDemand));
            // offices: where the town's own work (its books, its ads, its house sales, its new buildings) goes to firms out
            // of town for want of one here
            Offices(pop, ref building, buildCap);
            // industry: a plant of whatever kind would hire the people who are looking and pay its way, leaving out any that
            // failed here this past year; first what the town's businesses now import, then what the town doesn't make yet
            bool factoryPending = Buildings.Any(b => !b.Built && b.Def.Cat == Category.Industrial);
            bool factoryCase = !factoryPending && looking.Count >= 7 + pop / 40;
            FactoryMonths = factoryCase ? FactoryMonths + 1 : 0;
            bool fstrong = looking.Count >= 20 + pop / 20;
            // a line for every kind of plant and farm: what's in the way of one, or that it could open
            var plantRows = new Dictionary<BuildingType, Prospect>();
            var candidates = new List<BuildingType>();
            foreach (var k in Defs.All)
            {
                var d = Defs.Get(k);
                if (d.Cat != Category.Industrial || !d.Makes.HasValue) continue;
                var p = Pro(d.Name, k, 1, Buildings.Count(b => b.Type == k && b.Open), LastImports[(int)d.Makes.Value], Outlook.Promising);
                plantRows[k] = p;
                var site = Buildings.FirstOrDefault(b => b.Type == k && !b.Built);
                if (site != null) { p.State = Outlook.Building; p.Site = site.Id; continue; }
                if (PlantHeld(k, pop, fstrong, looking, grads, p)) continue;
                if (factoryPending) p.State = Outlook.Queued;
                else if (FactoryMonths < 2)
                {
                    if (factoryCase) { p.Have = FactoryMonths; p.Of = 2; }
                    else { p.State = Outlook.NoStaff; p.Have = looking.Count; p.Need = 7 + pop / 40; p.Of = -1; }
                }
                else if (building >= buildCap) p.State = Outlook.BuildersBusy;
                else candidates.Add(k);
            }
            if (FactoryMonths >= 2 && building < buildCap)
            {
                BuildingType best = BuildingType.FreightTerminal; double bestScore = double.MinValue;
                var notes = new List<string>();
                foreach (var k in candidates)
                {
                    var d = Defs.Get(k);
                    double pays = PlantCase(k);
                    int g = (int)d.Makes.Value;
                    double score = Math.Min(2, pays) + (LastImports[g] > 5000 ? 1.5 : 0) + (Buildings.Any(b => b.Def.Makes == d.Makes && b.Open) ? 0 : 0.8) + Rand() * 0.6;
                    notes.Add(k + " " + pays.ToString("0.00"));
                    if (score > bestScore) { bestScore = score; best = k; }
                }
                DevNote("factory: looking " + looking.Count + " basic " + basic + " school " + school + " trade " + trade + " grads " + grads + " -> " + string.Join(",", notes));
                if (best != BuildingType.FreightTerminal)
                {
                    var bp = plantRows[best];
                    var founder = Founder(best);
                    if (founder == null) { NoInvestor = best; bp.State = Outlook.NoBacker; DevNote("no founder for " + best); }
                    else
                    {
                        var nb = Expand(best, founder);
                        if (nb != null) { building++; FactoryMonths = 0; bp.State = Outlook.Opening; bp.Site = nb.Id; }
                        else bp.State = Outlook.NoLot;
                    }
                }
            }
            // supply: a plant or farm when the town's businesses import a lot of what one would make, and the ones here are busy
            // (a food plant when the stores import groceries, farms when it imports crops, a sawmill when the furniture makers
            // import lumber)
            double farmImports = 0;
            foreach (var g in new[] { Good.Crops, Good.Milk, Good.Livestock, Good.Logs, Good.Plants }) farmImports += LastImports[(int)g];
            ZoneDemand[(int)Zone.Farmland] = Math.Max(-1, Math.Min(1, (float)(farmImports / 30000.0)));
            if (building < buildCap && !factoryPending)
                foreach (var gd in Defs.Goods)
                {
                    var g = gd.Good;
                    double imports = LastImports[(int)g];
                    if (imports < 12000) continue;
                    var makers = Defs.All.Where(k => Defs.Get(k).Makes == g && Defs.Get(k).Cat == Category.Industrial).ToList();
                    if (makers.Count == 0) continue;
                    var key = makers[0];
                    if (Buildings.Any(b => makers.Contains(b.Type) && !b.Built)) { ReadyToOpen(key, false, 3, false); continue; }
                    var mine = Buildings.Where(b => makers.Contains(b.Type) && b.Open).ToList();
                    if (mine.Count >= 2 + pop / 500) { ReadyToOpen(key, false, 3, false); continue; }
                    bool busy = mine.All(b => b.LastUse > 0.8f && b.LossMonths < 4);
                    // the kind that pays best, with land for it
                    BuildingType kind = BuildingType.FreightTerminal; double kp = 1.1;
                    foreach (var k in makers)
                    {
                        var d = Defs.Get(k);
                        if (pop < d.MinPop || ZonedFree(d.Zone) < d.W * d.H || Cooling(k, imports > 60000)) continue;
                        if (Buildings.Count(b => b.Type == k && (b.Open || !b.Built)) >= 1 + pop / 600) continue;
                        double p = PlantCase(k) + (mine.Any(b => b.Type == k) ? 0 : 0.15) + Rand() * 0.1;
                        if (p > kp) { kp = p; kind = k; }
                    }
                    bool supplyCase = busy && kind != BuildingType.FreightTerminal && imports > Math.Max(12000, 0.25 * Defs.Get(kind).TotalJobs * Defs.Get(kind).Output * Balance.DaysPerMonth * gd.Wholesale);
                    var sp = plantRows[supplyCase ? kind : key];
                    if (!Ready(sp, key, supplyCase, IsFarm(kind) ? 2 : 3, imports > 60000)) continue;
                    var founder = Founder(kind);
                    if (founder == null) { NoInvestor = kind; sp.State = Outlook.NoBacker; continue; }
                    var nb = Expand(kind, founder);
                    if (nb != null) { building++; EntryMonths[(int)key] = 0; sp.State = Outlook.Opening; sp.Site = nb.Id; if (building >= buildCap) break; }
                    else sp.State = Outlook.NoLot;
                }
            // builders: one if there's none; another only when the ones here are flat out with months of work queued
            var builders = Buildings.Where(b => b.Type == BuildingType.Construction && b.Open).ToList();
            bool builderPending = Buildings.Any(b => b.Type == BuildingType.Construction && !b.Built);
            // the steady work: homes and businesses started each month, over the last half year or so. A rush of public works
            // is a one-off and doesn't keep a second firm going
            WorkRate = WorkRate * 0.85f + WorkStarted * 0.15f; WorkStarted = 0;
            float backlog = Buildings.Where(b => !b.Built && b.Owner != Owner.Town).Sum(b => b.Def.Work - b.Progress);
            bool builderCase;
            // the first builder once there's steady work to keep a crew going
            if (builders.Count == 0) builderCase = !builderPending && ((WorkRate > 40 && pop >= 40) || pop >= 150);
            else
            {
                float crews = builders.Sum(b => b.Def.TotalJobs) * Balance.DaysPerMonth;
                // more steady work than the builders here can do, for months, with work going to outside contractors
                bool overflow = Buildings.Any(b => !b.Built && b.ContractorId < 0 && b.Owner != Owner.Town);
                builderCase = !builderPending && WorkRate > crews && overflow && builders.All(b => b.Staffing >= 0.9f && b.LastProfit > 0);
            }
            if (ReadyToOpen(BuildingType.Construction, builderCase, builders.Count == 0 ? 1 : 3, builders.Count == 0))
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
            Prospects = pros; pros = null;
        }

        /// <summary>Is something in the way of a new plant or farm of this kind, as the town sees it this month? Notes what on
        /// the prospect.</summary>
        bool PlantHeld(BuildingType k, int pop, bool strong, List<Citizen> looking, int grads, Prospect p)
        {
            var d = Defs.Get(k);
            int minPop = Math.Max(150, d.MinPop);
            if (pop < minPop) { p.State = Outlook.TooSmall; p.MinPop = minPop; return true; }
            if (Cooling(k, strong)) { p.State = Outlook.Failed; p.Until = RetryDay(k, strong); return true; }
            if (Buildings.Count(b => b.Type == k && (b.Open || !b.Built)) >= 1 + pop / 600) { p.State = Outlook.Enough; p.Per = 600; return true; }
            if (Buildings.Count(b => b.Def.Makes == d.Makes && (b.Open || !b.Built)) >= 2 + pop / 500) { p.State = Outlook.Enough; p.Per = 500; return true; }
            int fit = Qualified(looking, k, grads);
            if (looking.Count < d.TotalJobs * 0.7f || fit < d.TotalJobs * 0.6f) { p.State = Outlook.NoStaff; p.Have = fit; p.Need = Math.Ceiling(d.TotalJobs * 0.7f); return true; }
            if (ZonedFree(d.Zone) < d.W * d.H) { p.State = Outlook.NoLot; return true; }
            // any of its kind here already struggling, short-handed or only scraping by
            if (Buildings.Any(b => b.Type == k && b.Open && (b.LossMonths > 0 || b.Staffing < 0.9f || b.LastProfit < b.LastRevenue * 0.04))) { p.State = Outlook.Struggling; return true; }
            if (PlantCase(k) < 1.1) { p.State = Outlook.WontPay; return true; }
            return false;
        }
        public int FactoryMonths;            // months in a row there have been enough people looking for work to staff a plant
        public float WorkStarted, WorkRate;  // labour-days of private building started this month, and the running average
        [NonSerialized] public double[] LastImports = new double[Defs.GoodCount];
        [NonSerialized] public double[] LastExports = new double[Defs.GoodCount];
        [NonSerialized] public double LastFreightOutside;

        bool TradeCase(BuildingType t0, BuildingType unused, List<Building> open, int pop, out bool strong, List<BuildingType> kinds)
        {
            BuildingType pick;
            return TradeCase(t0, unused, open, pop, out strong, kinds, null, out pick);
        }

        /// <summary>Is there a case for another place in this trade, and which kind it would be. With none here: is there enough
        /// custom going out of town (or wanted) to keep one going. With some: are they full, all making money and still turning
        /// people away.</summary>
        bool TradeCase(BuildingType t0, BuildingType unused, List<Building> open, int pop, out bool strong, List<BuildingType> kinds, Market m, out BuildingType pick)
        {
            strong = false;
            pick = t0;
            caseNeed = 0; casePer = 0;
            var d0 = Defs.Get(t0);
            bool goods = d0.Sells.HasValue || t0 == BuildingType.AutoRepair;
            // the kinds in the order this town would try them: what it hasn't got first
            var order = kinds == null ? new List<BuildingType> { t0 } : kinds.OrderBy(k => open.Any(b => b.Type == k) ? 1 : 0).ThenBy(k => Fancy(k)).ToList();
            if (open.Count == 0)
            {
                // nobody does it here: is there enough custom going out of town to keep one going? For goods and repairs
                // that's what people actually spend elsewhere (most of which a shop here would win); for the rest, the
                // visits people wanted to make
                // (some of those who can't buy something here go without, and would buy if it were sold here; but
                // groceries and fuel they always fetch, and some would carry on shopping where they do)
                // and a dealer here won't have the very car everyone wants: many would still buy where they did
                double away = SpendAway[(int)t0] * (t0 == BuildingType.GroceryStore || t0 == BuildingType.GasStation ? 0.85 : t0 == BuildingType.CarDealer ? 0.7 : 1.0);
                double rev = goods ? away : WantAvg[(int)t0] * SpendPerWant(t0);
                // thin retail margins need a clearer case, except for the everyday things people buy every week
                bool everyday = t0 == BuildingType.GroceryStore || t0 == BuildingType.GasStation;
                double spare = everyday ? 1.0 : goods ? 1.25 : 1.08;
                caseAway = rev; caseState = Outlook.Promising;
                foreach (var k in order)
                {
                    if (!PaysItsWay(k, rev, spare)) continue;
                    pick = k;
                    DevNote(k + " est rev " + Money(rev) + " margin " + Margin(k).ToString("0.00") + " cost " + Money(RunningCost(k, rev)));
                    strong = PaysItsWay(k, rev, 1.8);
                    return true;
                }
                // not enough yet: what the one it would try first would need
                var first = order[0];
                pick = first;
                caseState = Outlook.Thin;
                caseNeed = LeastThatPays(r => PaysItsWay(first, r, spare));
                return false;
            }
            // the ones here are full, all making money and still turning people away: room for one more
            float lost = open.Sum(b => b.LastLost), served = open.Sum(b => b.LastCustomers);
            bool full = open.All(b => b.Staffing >= 0.95f && b.LastUse > 0.85f);
            bool earning = open.All(b => b.LastProfit > 0 && b.LossMonths == 0);
            bool room = open.Count < 1 + pop / PopPer(t0);
            double price = open.Average(b => b.Price) * (t0 == BuildingType.GroceryStore ? 4 : TicketSize(t0));
            double rev2 = lost * price;
            caseAway = rev2;
            casePer = PopPer(t0);
            caseState = !room ? Outlook.Enough : !earning ? Outlook.Struggling : !full || lost <= Math.Max(8, served * 0.15f) ? Outlook.HaveRoom : Outlook.Promising;
            if (!(full && earning && room && lost > Math.Max(8, served * 0.15f))) return false;
            foreach (var k in order) if (PaysItsWay(k, rev2)) { pick = k; return true; }
            var next = order[0];
            pick = next;
            caseState = Outlook.Thin;
            caseNeed = LeastThatPays(r => PaysItsWay(next, r));
            return false;
        }

        /// <summary>A fixed order for each town in which it takes to new kinds of place: one town's first restaurant is a
        /// diner, another's a pizzeria. Cheaper, plainer kinds tend to come first.</summary>
        float Fancy(BuildingType t)
        {
            uint x = 2166136261u;
            foreach (char ch in Name ?? "") { x ^= ch; x *= 16777619u; }
            x ^= (uint)((int)t * 2654435761u); x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
            return (x % 1000) / 1000f + (float)(Defs.Get(t).StartupCost / 400000.0);
        }

        /// <summary>How many of the people looking could fill this kind of business's jobs (up to how many of each it has).</summary>
        int Qualified(List<Citizen> looking, BuildingType t, int grads)
        {
            var d = Defs.Get(t);
            int n = 0;
            var used = new HashSet<int>();
            foreach (var j in d.Jobs)
            {
                var need = Defs.Get(j.Role).Needs;
                int got = 0;
                foreach (var c in looking)
                {
                    if (got >= j.Count) break;
                    if (used.Contains(c.Id) || !Defs.Qualifies(c.Edu, need)) continue;
                    used.Add(c.Id); got++;
                }
                // graduates in jobs below them would move for one that needs their degree
                if (need >= Edu.Degree && got < j.Count) got = Math.Min(j.Count, got + Math.Max(0, grads - used.Count(id => GetCitizen(id) != null && GetCitizen(id).Edu >= Edu.Degree)));
                n += got;
            }
            return n;
        }

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
                default: { var d = Defs.Get(t); return d.PopPer > 0 ? d.PopPer : 2500; }
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
            if (CarCount(h) == 0) GiveCar(h, edu);
            h.Fuel = Math.Max(h.Fuel, 1);
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
                // a family with one grocery chain (or their grown children) doesn't start a rival one
                if (d.Chain != null && Buildings.Any(b => b.Owner == Owner.Household && b.Def.Chain != null && b.Type != t && (b.OwnerId == h.Id || SameFamily(GetHousehold(b.OwnerId), h)))) continue;
                bool runs = owned == 0 && Members(h).Any(m => !m.IsChild && !m.Retired && m.Age <= 60 && !m.Owner && m.Ambition >= 0.55f &&
                                                           d.Jobs.Any(j => Defs.Qualifies(m.Edu, Defs.Get(j.Role).Needs)));
                bool invests = owned < 3 && h.Cash >= need * 2 + 30000 && (big || owned > 0) && CanStaff(t);
                if ((runs || invests) && Chance(0.6f)) return h;
            }
            return null;
        }

        static bool SameFamily(Household a, Household b) { return a != null && b != null && a.Name == b.Name; }

        /// <summary>A founder reopens an empty building of this kind (bought from the bank), or builds a new one.</summary>
        Building Expand(BuildingType t, Household founder)
        {
            if (founder == null) return null;
            var shut = Buildings.FirstOrDefault(b => b.Type == t && b.Closed && b.Built && Connected(b));
            if (shut == null) return Develop(t, founder, null);
            return Reopen(shut, founder);
        }

        /// <summary>A family buys an empty business building from the bank and opens it again.</summary>
        Building Reopen(Building shut, Household founder)
        {
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
                Treasury -= u; Book.Upkeep += u; Trade.Imports += u * 0.95; Fee(Practice.Engineering, u * 0.05);
                // IT for its staff
                double it = 40 * b.Employees.Count;
                Treasury -= it; Book.Upkeep += it; Fee(Practice.Software, it);
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
