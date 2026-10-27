using System;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// The grocery chains: Valley Value (the discounter), Redwood Market (the nicer supermarket) and Bulkworks Club (the
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

        /// <summary>The month's decisions for the chains: a first store, or another one.</summary>
        void Chains(ref int building, int buildCap)
        {
            int pop = Population;
            foreach (var t in ChainTypes)
            {
                var d = Defs.Get(t);
                var mine = Buildings.Where(b => b.Type == t).ToList();
                if (mine.Any(b => !b.Built)) continue;
                var open = mine.Where(b => b.Open).ToList();
                if (pop < d.MinPop || building >= buildCap) { ReadyToOpen(t, false, 2, false); continue; }
                if (open.Count == 0)
                {
                    double rev;
                    bool pays = ChainPays(t, out rev);
                    if (pays) DevNote(d.Chain + " at pop " + pop + " est rev " + Money(rev) + " margin " + ChainMargin(t).ToString("0.00") + " cost " + Money(RunningCost(t, rev)));
                    if (!ReadyToOpen(t, pays, 2, false)) continue;
                    var founder = Entrepreneur(t);
                    if (founder == null) { NoInvestor = t; DevNote("no founder for " + d.Chain); continue; }
                    var b = Expand(t, founder);
                    if (b == null) continue;
                    building++; EntryMonths[(int)t] = 0;
                    AddLog("The " + founder.Name + " family is opening " + d.Chain + ", the town's first " + d.Name.ToLower() + ".", LogKind.Good, b.Id);
                }
                else
                {
                    // another store: the town has grown into it, and the ones here are earning and full
                    bool room = pop >= (open.Count + 1) * d.PopPerStore;
                    bool earning = open.All(b => b.LastProfit > 0 && b.LossMonths == 0);
                    bool busy = open.All(b => b.LastUse > 0.8f) || open.Sum(b => b.LastLost) > 20;
                    if (!ReadyToOpen(t, room && earning && busy, 3, false)) continue;
                    var owner = open[0].Owner == Owner.Household ? GetHousehold(open[0].OwnerId) : null;
                    if (owner == null || owner.Cash < d.StartupCost * FoundersShare(t) + 10000) { DevNote(d.Chain + ": owners can't fund another store yet"); continue; }
                    // a store of theirs the bank took back, if it's in a part of town that needs one; else a new one
                    var shut = Buildings.FirstOrDefault(o => o.Type == t && o.Closed && o.Built && Connected(o) && GoodChainSpot(t, o.CenterX, o.CenterY));
                    var b = shut != null ? Reopen(shut, owner) : Develop(t, owner, null);
                    if (b == null) continue;
                    building++; EntryMonths[(int)t] = 0;
                    AddLog(d.Chain + " is opening store number " + (open.Count + 1) + " in town.", LogKind.Good, b.Id);
                }
            }
        }
    }
}
