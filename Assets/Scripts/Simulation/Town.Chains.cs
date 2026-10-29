using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// The grocery chains: Buymore (the discounter), Megamart (the nicer supermarket) and Bulkworks Club (the
    /// warehouse club, for drivers). Each is a company here: a local family opens the first store when the town's grocery
    /// shopping would keep one going (sooner if the grocers here are dear), and the chain opens another as the town grows and
    /// its stores fill up. They buy in bulk and sell cheaper than a corner grocery; the small grocers keep the people who walk
    /// to the shops. Families who can afford to care prefer the nicer store.
    /// </summary>
    public partial class Town
    {
        public static readonly BuildingType[] ChainTypes = { BuildingType.DiscountStore, BuildingType.Supermarket, BuildingType.WarehouseClub };

        /// <summary>What the town spent on groceries last month: at the shops here and out of town.</summary>
        double GroceryMarket()
        {
            double m = SpendAway[(int)BuildingType.GroceryStore];
            foreach (var b in Buildings) if (b.Open && b.Def.Sells == Good.Groceries) m += b.LastRevenue;
            return m;
        }

        /// <summary>A chain's usual price for groceries: the going rate (which moves with world prices) times its own place on
        /// price.</summary>
        public double ChainPrice(BuildingType t)
        {
            int g = (int)Good.Groceries;
            return Defs.Retail[g] * PriceDrift[g] * Defs.Get(t).PriceFactor;
        }

        /// <summary>What a chain store pays for groceries by the truckload: from the cheapest food plant here, or imported.</summary>
        double ChainCost(BuildingType t)
        {
            var g = Good.Groceries;
            double bulk = Defs.Get(t).BulkBuy;
            double cost = ImportPrice(g) * bulk * (1 + Balance.FreightOutside);
            foreach (var s in Buildings)
                if (s.Open && s.Def.Makes == g) cost = Math.Min(cost, s.Price * bulk * (1 + Balance.FreightLocal));
            return cost;
        }

        /// <summary>A chain store's margin: its usual price against what it pays.</summary>
        double ChainMargin(BuildingType t)
        {
            double p = ChainPrice(t);
            return (p - ChainCost(t)) / p;
        }

        /// <summary>Would the chain's first store here pay its way: the share of the town's grocery shopping it would win (more the
        /// further it undercuts the cheapest grocer here, a nicer store counting as a bit cheaper; less with other chains already
        /// here), against its staff, upkeep and loan.</summary>
        bool ChainPays(BuildingType t, out double rev)
        {
            var d = Defs.Get(t);
            var sellers = Buildings.Where(b => b.Open && b.Def.Sells == Good.Groceries).ToList();
            double ours = ChainPrice(t) * (1 - d.Appeal * 0.8);
            double cheapest = sellers.Count > 0 ? sellers.Min(b => b.Price * (1 - b.Def.Appeal * 0.8)) : ours * 1.15;
            int chains = sellers.Count(b => b.Def.Chain != null);
            double r = cheapest / ours;
            double share = Math.Min(0.6, ChainShare * r * r * r) / (1 + 0.5 * chains);
            if (d.NeedsCar) share *= Households.Count > 0 ? Households.Count(h => CarCount(h) > 0) / (double)Households.Count : 0;
            rev = GroceryMarket() * share;
            return rev * ChainMargin(t) >= RunningCost(t, rev) * 1.2;
        }
        public const double ChainShare = 0.22;

        /// <summary>The chain's head office: open, or going up (null if it hasn't one).</summary>
        public Building HeadOffice(BuildingType chain)
        {
            var o = Defs.Get(chain).Office;
            if (!o.HasValue) return null;
            return Buildings.FirstOrDefault(b => b.Type == o.Value && !(b.Built && b.Closed));
        }

        /// <summary>The chain a store or a head office belongs to (null for anything else).</summary>
        public static BuildingType? ChainOf(Building b) { return b.Def.Chain != null ? b.Type : b.Def.HeadOf; }

        /// <summary>A chain's stores, open ones, under a head office.</summary>
        public List<Building> StoresOf(Building office) { return Portfolio(office).Where(o => o.Open).ToList(); }

        const float ChainSpacing = 15f;   // tiles: how far a chain's next store must be from its others
        Outlook officeWhy = Outlook.NoLot;   // why the head office couldn't be set up

        /// <summary>The month's decisions for the chains: a first store; a head office to run them; another store when the town
        /// needs one. A chain thinks about another store only when its own are full and turning people away and the town's
        /// other grocers can't take them either, the town has grown into it, and there's a lot in a part of town its stores are
        /// far from. Its head office decides, and pays.</summary>
        void Chains(ref int building, int buildCap)
        {
            int pop = Population;
            var grocers = Buildings.Where(b => b.Open && b.Def.Sells == Good.Groceries).ToList();
            float townLost = grocers.Sum(b => b.LastLost), townServed = grocers.Sum(b => b.LastCustomers);
            bool townShort = townLost > Math.Max(20f, (townLost + townServed) * 0.06f);
            foreach (var t in ChainTypes)
            {
                var d = Defs.Get(t);
                var mine = Buildings.Where(b => b.Type == t).ToList();
                var open = mine.Where(b => b.Open).ToList();
                var office = HeadOffice(t);
                var p = Pro(d.Chain, t, 0, open.Count, 0, Outlook.Promising);
                // a chain with stores and no head office sets one up to run them
                if (open.Count >= 2 && office == null && building < buildCap)
                {
                    office = SetUpOffice(t, open);
                    if (office != null) building++;
                }
                var site = mine.FirstOrDefault(b => !b.Built);
                if (site != null) { p.State = Outlook.Building; p.Site = site.Id; continue; }
                if (pop < d.MinPop) { p.State = Outlook.TooSmall; p.MinPop = d.MinPop; ReadyToOpen(t, false, 2, false); continue; }
                if (building >= buildCap) { p.State = Outlook.BuildersBusy; ReadyToOpen(t, false, 2, false); continue; }
                if (open.Count == 0)
                {
                    double rev;
                    bool pays = ChainPays(t, out rev);
                    p.Away = rev; p.Estimate = true;
                    if (!pays) { p.State = Outlook.Thin; p.Need = LeastThatPays(r => r * ChainMargin(t) >= RunningCost(t, r) * 1.2); }
                    if (pays) DevNote(d.Chain + " at pop " + pop + " est rev " + Money(rev) + " margin " + ChainMargin(t).ToString("0.00") + " cost " + Money(RunningCost(t, rev)));
                    if (!Ready(p, t, pays, 2, false)) continue;
                    // a head office still going with no stores opens one itself; otherwise a family here starts the chain
                    Building b;
                    if (office != null && office.Open) b = ChainStore(t, office, p);
                    else
                    {
                        var founder = Entrepreneur(t);
                        if (founder == null) { p.State = Outlook.NoBacker; NoInvestor = t; DevNote("no founder for " + d.Chain); continue; }
                        b = Expand(t, founder);
                        if (b != null) AddLog("The " + founder.Name + " family is opening " + d.Chain + ", the town's first " + d.Name.ToLower() + ".", LogKind.Good, b.Id);
                    }
                    if (b == null) { if (p.State == Outlook.Promising) p.State = Outlook.NoLot; continue; }
                    building++; EntryMonths[(int)t] = 0;
                    p.State = Outlook.Opening; p.Site = b.Id;
                }
                else
                {
                    // do we need another one: our stores full, turning people away, and the town's other grocers can't take
                    // them either; making money; and a town big enough for one more
                    bool room = pop >= (open.Count + 1) * d.PopPerStore;
                    bool earning = open.All(b => b.LastProfit > 0 && b.LossMonths == 0);
                    float lost = open.Sum(b => b.LastLost), served = open.Sum(b => b.LastCustomers);
                    bool full = open.All(b => b.LastUse > 0.9f) && lost > Math.Max(10f, (lost + served) * 0.1f);
                    p.Away = lost * open.Average(b => b.Price) * 4;
                    if (!room) { p.State = Outlook.Enough; p.Per = d.PopPerStore; }
                    else if (!earning) p.State = Outlook.Struggling;
                    else if (!full) p.State = Outlook.HaveRoom;
                    else if (!townShort) p.State = Outlook.Served;
                    if (!Ready(p, t, room && earning && full && townShort, 3, false)) continue;
                    // the head office decides, and pays
                    // to grow past its first store a chain sets up a head office, which then decides and pays
                    if (office == null && building < buildCap) { office = SetUpOffice(t, open); if (office != null) building++; }
                    if (office == null || !office.Open)
                    {
                        p.Kind = d.Office.Value;
                        if (office != null) { p.State = Outlook.Building; p.Site = office.Id; }
                        else p.State = officeWhy;
                        continue;
                    }
                    var b = ChainStore(t, office, p);
                    if (b == null) continue;
                    building++; EntryMonths[(int)t] = 0;
                    p.State = Outlook.Opening; p.Site = b.Id;
                    AddLog(d.Chain + " is opening store number " + (open.Count + 1) + " in town.", LogKind.Good, b.Id);
                }
            }
        }

        /// <summary>The head office opens a store: one of theirs the bank took back, if it's in a part of town that needs one,
        /// or a new one on a lot far enough from their others. It puts in its share from its own money.</summary>
        Building ChainStore(BuildingType t, Building office, Prospect p)
        {
            var d = Defs.Get(t);
            var family = GetHousehold(office.OwnerId);
            double share = d.StartupCost * FoundersShare(t);
            double has = Math.Max(0, office.Cash - Cushion(office) * 0.5) + (family != null ? Math.Max(0, family.Cash - 20000) : 0);
            if (has < share) { p.State = Outlook.NoBacker; DevNote(d.Chain + ": head office can't fund another store yet"); return null; }
            var shut = Buildings.FirstOrDefault(o => o.Type == t && o.Closed && o.Built && Connected(o) && GoodChainSpot(t, o.CenterX, o.CenterY));
            if (shut != null)
            {
                double price = shut.Value * 0.6;
                double down = Math.Min(price, Math.Max(0, office.Cash - Cushion(office) * 0.5));
                office.Cash -= down;
                SellerGets(shut, price, false);
                FirmOwns(shut, office); shut.Brand = d.Chain;
                if (price > down) PropertyLoan(shut, family, office, price - down, false);
                OpenBusiness(shut);
                return shut;
            }
            var b = Develop(t, null, office);
            if (b == null) p.State = Outlook.NoLot;
            return b;
        }

        /// <summary>The family that runs a chain sets up its head office downtown, on a lot that doesn't break up a big one.
        /// When it opens it takes over their stores.</summary>
        Building SetUpOffice(BuildingType t, List<Building> stores)
        {
            var d = Defs.Get(t);
            var ot = d.Office.Value;
            officeWhy = Outlook.NoBacker;
            var owner = stores.Where(b => b.Owner == Owner.Household).Select(b => GetHousehold(b.OwnerId)).FirstOrDefault(h => h != null);
            if (owner == null) return null;
            if (owner.Cash < FoundersStake(ot) + 10000) { DevNote(d.Chain + ": owners can't fund a head office yet"); return null; }
            officeWhy = Outlook.NoLot;
            var o = Develop(ot, owner, null);
            if (o == null) return null;
            o.Brand = d.Chain + " head office";
            AddLog("The " + owner.Name + " family is setting up a head office to run " + d.Chain + ".", LogKind.Good, o.Id);
            return o;
        }

        /// <summary>A head office opens: it takes over its family's stores.</summary>
        void OfficeTakesOver(Building office)
        {
            var chain = office.Def.HeadOf.Value;
            var fam = GetHousehold(office.OwnerId);
            int n = 0;
            foreach (var s in Buildings.Where(b => b.Type == chain && b.Built && !b.Closed && b.Owner == Owner.Household).ToList())
            {
                var sf = GetHousehold(s.OwnerId);
                if (fam == null || !(s.OwnerId == fam.Id || SameFamily(sf, fam))) continue;
                FirmOwns(s, office); n++;
                // the family stop being the owners at the store; the head office is theirs
                foreach (var id in s.Employees) { var c = GetCitizen(id); if (c != null && c.Owner) c.Owner = false; }
            }
            if (n > 0) AddLog(Title(office) + " opened and took over " + (n == 1 ? "the chain's store." : "the chain's " + n + " stores."), LogKind.Good, office.Id);
        }

        /// <summary>A head office closes: its stores go back to the family that owned it, or are sold.</summary>
        void OfficeCloses(Building office)
        {
            var fam = GetHousehold(office.OwnerId);
            foreach (var l in Loans) if (l.FirmId == office.Id && l.BuildingId != office.Id) l.FirmId = -1;   // each store pays its own loan
            foreach (var s in Portfolio(office).ToList())
            {
                if (fam != null) { FamilyOwns(s, fam); continue; }
                if (s.Open) TransferBusiness(s);
            }
        }
    }
}
