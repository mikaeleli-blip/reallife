using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>The kinds of work offices do for their clients.</summary>
    public enum Practice { Accounting, Legal, Insurance, Realty, Architecture, Engineering, Marketing, Design, Software, Calls }

    /// <summary>
    /// Offices. Their clients are mostly here: businesses pay for their books, their ads, their IT and a lawyer; families for
    /// their tax returns, and their insurance carries an agent's commission; every house sold carries an agent's and a
    /// lawyer's fee, every new building an architect's and an engineer's. Those fees are owed whether or not there's an
    /// office in town to earn them: with none, or with the ones here full, the work goes to firms out of town. The offices
    /// here share the work by how much their staff can take on, and clients from out of town fill a little of whatever time
    /// they have spare, but only a little: the region's own firms have most of that work.
    /// </summary>
    public partial class Town
    {
        public static readonly int PracticeCount = Enum.GetValues(typeof(Practice)).Length;
        static readonly BuildingType[] practiceKind =
        {
            BuildingType.AccountingFirm, BuildingType.LawFirm, BuildingType.InsuranceAgency, BuildingType.RealEstateAgency, BuildingType.ArchitectureFirm,
            BuildingType.EngineeringFirm, BuildingType.MarketingAgency, BuildingType.DesignStudio, BuildingType.SoftwareStudio, BuildingType.CallCenter
        };
        // clients from out of town: the most of an office's spare time they'll fill, and the most they'll bring all the town's
        // offices of a kind together in a month
        static readonly double[] outsideFill = { 0.12, 0.15, 0.08, 0.08, 0.2, 0.2, 0.12, 0.2, 0.35, 0.5 };
        static readonly double[] outsideCap = { 6000, 10000, 4000, 4000, 10000, 14000, 6000, 6000, 20000, 18000 };
        const double OutsideRate = 0.85;   // what clients out of town pay, against the going rate

        // what the work is: shares of a house sale, of the cost of a new building, of insurance premiums
        public const double AgentShare = 0.02, LawyerShare = 0.01;   // of a house's price (the 3% closing costs)
        public const double InsuranceCommission = 0.15;              // of premiums
        public const double BusinessInsurance = 0.0005;              // a month, of a business's building's value
        public const double OpeningLegal = 2000, OpeningDesign = 2500;

        public double[] PracticeOwed;   // fees owed so far this month, by practice, to be earned at the start of the next
        public float[] PracticeAway;    // by practice: work that went to firms out of town, averaged over the last half year or so
        [NonSerialized] public double[] LastPracticeOwed = new double[PracticeCount], LastPracticeLocal = new double[PracticeCount], LastPracticeOutside = new double[PracticeCount];

        public static BuildingType PracticeKind(Practice p) { return practiceKind[(int)p]; }
        public static bool IsOffice(BuildingType t) { return Defs.Get(t).Bills || t == BuildingType.Office; }

        /// <summary>The practice an office of this kind is in. The big office building does books (its accountants and clerks)
        /// and software (its developers).</summary>
        public static Practice? PracticeOf(BuildingType t)
        {
            int i = Array.IndexOf(practiceKind, t);
            if (i >= 0) return (Practice)i;
            return t == BuildingType.Office ? Practice.Accounting : (Practice?)null;
        }
        static Practice PracticeOf(BuildingType t, Role r) { return t == BuildingType.Office && r == Role.Developer ? Practice.Software : PracticeOf(t).Value; }

        /// <summary>Does an office of this kind do this sort of work.</summary>
        public static bool Does(BuildingType t, Practice p)
        {
            if (t == BuildingType.Office) return p == Practice.Accounting || p == Practice.Software;
            return PracticeOf(t) == p;
        }

        /// <summary>A fee owed for office work, already paid by whoever owes it.</summary>
        void Fee(Practice p, double amount) { if (amount > 0) PracticeOwed[(int)p] += amount; }

        void HouseholdFee(Household h, Practice p, double amount, int line)
        {
            amount = Math.Min(amount, Math.Max(0, h.Cash));
            if (amount <= 0) return;
            HouseholdPays(h, amount, line); Fee(p, amount);
        }

        /// <summary>What a business of this kind pays a month for one sort of office work, on this much takings and staff:
        /// its books, IT for its staff, ads (most for the shops, restaurants and services), design (packaging for the plants),
        /// a phone line for the big ones, and a lawyer on retainer for the bigger employers.</summary>
        static double OfficeBill(BuildingType t, Practice p, double revenue, int staff)
        {
            if (Does(t, p)) return 0;
            var d = Defs.Get(t);
            bool facing = d.Sells.HasValue || d.Serves != Service.None || t == BuildingType.CarDealer || t == BuildingType.Bank;
            bool maker = d.Makes.HasValue && !IsFarm(t);
            switch (p)
            {
                case Practice.Accounting: return 100 + 0.006 * revenue;
                case Practice.Software: return 40 * staff;
                case Practice.Marketing: return facing ? 0.008 * revenue : maker ? 0.002 * revenue : t == BuildingType.Realty || t == BuildingType.Construction ? 0.003 * revenue : 0;
                case Practice.Design: return maker ? 0.003 * revenue : facing ? 0.001 * revenue : 0;
                case Practice.Calls: return revenue > 60000 ? 0.0025 * revenue : 0;
                case Practice.Legal: return 0.003 * revenue + (staff >= 8 ? 100 : 0);
                case Practice.Engineering: return maker ? 0.004 * revenue : 0;
                case Practice.Realty: return t == BuildingType.Realty ? 0.03 * revenue : 0;   // a property firm's listings and lettings
                default: return 0;
            }
        }

        /// <summary>All of a business's office bills and its insurance for a month, for working out whether one would pay.</summary>
        static double OfficeBills(BuildingType t, double revenue, int staff)
        {
            double s = Defs.Get(t).Value * BusinessInsurance;
            for (int i = 0; i < PracticeCount; i++) s += OfficeBill(t, (Practice)i, revenue, staff);
            return s;
        }

        /// <summary>A business pays its month's office bills and insurance (the premium goes out of town, the agent's
        /// commission to an office here if there is one).</summary>
        void PayOfficeBills(Building b)
        {
            double rev = b.Revenue; int staff = b.Employees.Count;
            for (int i = 0; i < PracticeCount; i++)
            {
                double v = OfficeBill(b.Type, (Practice)i, rev, staff);
                if (v <= 0) continue;
                b.Cash -= v; b.OtherCosts += v; Fee((Practice)i, v);
            }
            double prem = b.Value * BusinessInsurance;
            b.Cash -= prem; b.OtherCosts += prem;
            Fee(Practice.Insurance, prem * InsuranceCommission); Trade.Utilities += prem * (1 - InsuranceCommission);
        }

        /// <summary>What an office's staff can take on in a month in one practice: each one's billing rate over the days they
        /// work, a little more for experience.</summary>
        public double OfficeCapacity(Building b, Practice p)
        {
            double c = 0;
            foreach (var id in b.Employees)
            {
                var w = GetCitizen(id);
                if (w == null) continue;
                var r = Defs.Get(w.Role);
                if (r.Billing <= 0 || PracticeOf(b.Type, w.Role) != p) continue;
                c += r.Billing * Balance.DaysPerMonth * (1 + Math.Min(0.4, w.Experience * 0.015));
            }
            return c;
        }

        /// <summary>The start of the month: last month's fees go to the offices here, a share each by how much they can take
        /// on; what they can't goes to firms out of town. Then clients from out of town fill a little of their spare time.</summary>
        void OfficeWork()
        {
            if (PracticeOwed == null || PracticeOwed.Length != PracticeCount) PracticeOwed = new double[PracticeCount];
            if (PracticeAway == null || PracticeAway.Length != PracticeCount) PracticeAway = new float[PracticeCount];
            var offices = Buildings.Where(b => b.Open && IsOffice(b.Type) && Connected(b)).ToList();
            foreach (var b in offices) { b.LastLocalWork = 0; b.LastOutsideWork = 0; }
            for (int i = 0; i < PracticeCount; i++)
            {
                var p = (Practice)i;
                double owed = PracticeOwed[i]; PracticeOwed[i] = 0;
                var caps = new List<KeyValuePair<Building, double>>();
                foreach (var b in offices) { double c = OfficeCapacity(b, p); if (c > 0) caps.Add(new KeyValuePair<Building, double>(b, c)); }
                double total = caps.Sum(kv => kv.Value);
                double local = Math.Min(owed, total), away = owed - local;
                if (away > 0) { Trade.Imports += away; OutOfTown += away; }
                PracticeAway[i] = PracticeAway[i] * 0.8f + (float)away * 0.2f;
                double spare = total - local;
                double outside = Math.Min(spare, Math.Min(total * outsideFill[i], outsideCap[i]));
                foreach (var kv in caps)
                {
                    var b = kv.Key;
                    double mine = total > 0 ? local * kv.Value / total : 0;
                    double theirs = spare > 0 ? outside * (kv.Value - mine) / spare : 0;
                    OfficeEarns(b, mine, theirs * OutsideRate);
                    b.CapTotal += (float)kv.Value; b.CapUsed += (float)(mine + theirs);
                }
                Trade.Services += outside * OutsideRate;
                LastPracticeOwed[i] = owed; LastPracticeLocal[i] = local; LastPracticeOutside[i] = outside * OutsideRate;
            }
        }

        void OfficeEarns(Building b, double local, double outside)
        {
            double v = local + outside;
            if (v <= 0) return;
            double overhead = v * Balance.OfficeOverhead;
            b.Cash += v - overhead; b.Revenue += v; b.OtherCosts += overhead; Trade.Imports += overhead;
            b.LastLocalWork += local; b.LastOutsideWork += outside;
        }

        /// <summary>The staff a new office of this kind starts with (a quarter of each job, at least one of the first).</summary>
        static IEnumerable<KeyValuePair<Role, int>> StartingStaff(BuildingType t)
        {
            var d = Defs.Get(t);
            for (int i = 0; i < d.Jobs.Length; i++)
            {
                int n = Math.Max(i == 0 ? 1 : 0, (int)Math.Round(d.Jobs[i].Count * 0.25f));
                if (n > 0) yield return new KeyValuePair<Role, int>(d.Jobs[i].Role, n);
            }
        }

        /// <summary>What a new office of this kind costs a month: its starting staff, utilities, upkeep, tax and loan.</summary>
        double SmallOfficeCost(BuildingType t)
        {
            var d = Defs.Get(t);
            double w = 0; int n = 0;
            foreach (var kv in StartingStaff(t)) { w += kv.Value * Math.Max(Policy.MinWage, Defs.Get(kv.Key).Wage * 1.05); n += kv.Value; }
            return w + d.TotalJobs * Balance.UtilitiesJob + d.Value * (Balance.Maintenance + Policy.PropertyTax) / 12 + d.Value * BusinessInsurance
                   + Annuity(d.StartupCost * (1 - FoundersShare(t)), Balance.PropertyRate, Balance.PropertyMonths) + OfficeBills(t, 0, n);
        }

        /// <summary>The fees a month from clients here that would keep a new office of this kind going, with a little to spare,
        /// counting on what work it could pick up from out of town.</summary>
        double OfficeNeeds(BuildingType t, bool first)
        {
            double need = SmallOfficeCost(t) * 1.15 / (1 - Balance.OfficeOverhead);
            if (!first) return need;
            var p = PracticeOf(t).Value;
            double cap = 0;
            foreach (var kv in StartingStaff(t)) cap += kv.Value * Defs.Get(kv.Key).Billing * Balance.DaysPerMonth;
            double outside = Math.Min(cap * outsideFill[(int)p], outsideCap[(int)p]) * OutsideRate;
            return Math.Max(need * 0.4, need - outside);
        }

        /// <summary>The month's decisions for offices: one of a kind when the work going to firms out of town would keep a small
        /// office going; another when the ones here are full and work still goes away. The big office building isn't built
        /// new: firms of each kind are.</summary>
        void Offices(int pop, ref int building, int buildCap)
        {
            foreach (var p0 in Enum.GetValues(typeof(Practice)))
            {
                var pr = (Practice)p0;
                var t = PracticeKind(pr);
                var d = Defs.Get(t);
                var firms = Buildings.Where(b => b.Open && Does(b.Type, pr)).ToList();
                double away = PracticeAway[(int)pr];
                var p = Pro(d.Name, t, 2, firms.Count, away, Outlook.Promising);
                if (pop < d.MinPop) { p.State = Outlook.TooSmall; p.MinPop = d.MinPop; ReadyToOpen(t, false, 2, false); continue; }
                var site = Buildings.FirstOrDefault(b => b.Type == t && !b.Built);
                if (site != null) { p.State = Outlook.Building; p.Site = site.Id; continue; }
                double need = OfficeNeeds(t, firms.Count == 0);
                bool caseHolds;
                if (firms.Count == 0)
                {
                    caseHolds = away >= need;
                    if (!caseHolds) { p.State = Outlook.Thin; p.Need = need; }
                }
                else
                {
                    bool full = firms.All(b => b.LastUse > 0.85f && b.Staffing >= 0.95f);
                    bool earning = firms.All(b => b.LastProfit > 0 && b.LossMonths == 0);
                    caseHolds = full && earning && away >= need;
                    p.State = !earning ? Outlook.Struggling : !full ? Outlook.HaveRoom : away < need ? Outlook.Thin : Outlook.Promising;
                    if (p.State == Outlook.Thin) p.Need = need;
                }
                if (!Ready(p, t, caseHolds, firms.Count == 0 ? 2 : 3, away >= need * 2)) continue;
                if (building >= buildCap) { p.State = Outlook.BuildersBusy; continue; }
                var founder = Founder(t);
                if (founder == null) { p.State = Outlook.NoBacker; NoInvestor = t; DevNote("no founder for " + t); continue; }
                var nb = Expand(t, founder);
                if (nb != null) { building++; EntryMonths[(int)t] = 0; p.State = Outlook.Opening; p.Site = nb.Id; }
                else p.State = Outlook.NoLot;
            }
        }
    }
}
