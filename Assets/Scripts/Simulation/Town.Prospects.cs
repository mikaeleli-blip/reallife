using System;
using System.Collections.Generic;

namespace RealLife.Simulation
{
    /// <summary>Where a kind of business stands this month.</summary>
    public enum Outlook
    {
        Opening,        // someone is starting one this month
        Building,       // one is going up
        Promising,      // the numbers have added up Have of Of months running
        NoLot,          // it would open, but there's no free lot of its size and zone by a road
        NoBacker,       // it would pay, but nobody here has the savings to start it
        BuildersBusy,   // it would open, but the builders are booked up this month
        Queued,         // another of its sort (an office, a plant) is going up first
        Failed,         // one went under lately: nobody tries again before Until
        TooSmall,       // the town isn't big enough yet (MinPop)
        Thin,           // not enough custom yet: Away a month, about Need wanted
        HaveRoom,       // the ones here aren't full or turning people away
        Struggling,     // one here is losing money or short of staff
        Enough,         // as many as a town this size keeps (about one per Per people)
        NoStaff,        // not enough people looking for work who could do the jobs (Have of Need)
        WontPay,        // it wouldn't cover its costs
        Served,         // the town's other shops of its trade can take the custom (a chain thinking about another store)
    }

    /// <summary>One line of the market report: a trade or a kind of business, what the town spends elsewhere for want of
    /// it, and what's holding the next one back. Filled in as the month's building decisions are made; nothing in the
    /// simulation reads it back.</summary>
    public sealed class Prospect
    {
        public string Name;         // the trade ("Meals") or the kind ("Sawmill")
        public BuildingType Kind;   // the kind that would open next
        public int Group;           // 0 shops and services, 1 industry and farms, 2 offices
        public int Here;            // how many are open now
        public double Away;         // a month: spent out of town or turned away for want of one (for a plant: what businesses here import)
        public double Need;         // a month, the custom one would need (Thin); people (NoStaff)
        public int Have, Of;        // months the case has held, of how many (Promising); people who could do the jobs (NoStaff; Of -1: people looking for any work)
        public int Until;           // day (Failed)
        public int MinPop, Per;     // TooSmall; Enough
        public int Site = -1;       // the building going up (Building, Opening)
        public bool Estimate;       // Away is the custom a first one would win, not money now going elsewhere (a grocery chain)
        public Outlook State;
    }

    public partial class Town
    {
        /// <summary>Last month's market report, newest decisions first-hand.</summary>
        [NonSerialized] public List<Prospect> Prospects = new List<Prospect>();
        [NonSerialized] List<Prospect> pros;

        Prospect Pro(string name, BuildingType kind, int group, int here, double away, Outlook state)
        {
            var p = new Prospect { Name = name, Kind = kind, Group = group, Here = here, Away = away, State = state };
            if (pros != null) pros.Add(p);
            return p;
        }

        /// <summary>ReadyToOpen, noting on the prospect why it isn't ready yet: still proving itself, or one failed lately.</summary>
        bool Ready(Prospect p, BuildingType t, bool caseHolds, int months, bool strong)
        {
            bool ok = ReadyToOpen(t, caseHolds, months, strong);
            if (caseHolds && !ok)
            {
                if (EntryMonths[(int)t] < months) { p.State = Outlook.Promising; p.Have = EntryMonths[(int)t]; p.Of = months; }
                else { p.State = Outlook.Failed; p.Until = RetryDay(t, strong); }
            }
            return ok;
        }

        /// <summary>When nobody will hold off any longer after a failure (see Cooling).</summary>
        int RetryDay(BuildingType t, bool strong)
        {
            int i = (int)t, y = Balance.DaysPerYear;
            if (Day - PrevFailDay[i] < y * 3 && Day - LastFailDay[i] < y * 2) return Math.Min(PrevFailDay[i] + y * 3, LastFailDay[i] + y * 2);
            return LastFailDay[i] + y;
        }

        /// <summary>The least custom a month on which one would pay its way, or 0 if none would.</summary>
        static double LeastThatPays(Func<double, bool> pays)
        {
            for (double r = 300; r < 4e6; r *= 1.06) if (pays(r)) return r;
            return 0;
        }

        /// <summary>What goes out of town (or unbought) a month for want of a place in this trade.</summary>
        double MarketAway(Market m)
        {
            var t0 = m.Primary;
            bool goods = Defs.Get(t0).Sells.HasValue;
            return goods ? SpendAway[(int)t0] : WantAvg[(int)t0] * SpendPerWant(t0);
        }

        /// <summary>The founders' share of the cost of one of these: what a family needs saved to start it.</summary>
        public static double FoundersStake(BuildingType t) { return Math.Max(12000, Defs.Get(t).StartupCost * FoundersShare(t)); }

        // what TradeCase found
        Outlook caseState;
        double caseAway, caseNeed;
        int casePer;
    }
}
