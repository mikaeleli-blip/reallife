using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    public struct Opening
    {
        public Building Building; public Role Role; public double Wage; public int Count;
    }

    /// <summary>
    /// The labour market. Every workplace has job slots, each needing a credential. Employers decide how many of them they want
    /// filled and what they pay; the unemployed take the best-paid job they qualify for and can get to; people in poorly paid jobs
    /// move on when something better comes up. Pay climbs where nobody can be found and drifts down where too many are looking.
    /// </summary>
    public partial class Town
    {
        // ================================================================== who's wanted
        public int DesiredStaff(Building b, JobSlot j)
        {
            var d = b.Def;
            if (!b.Open) return 0;
            if (d.Cat == Category.Public)
            {
                switch (b.Type)
                {
                    case BuildingType.Elementary:
                    case BuildingType.HighSchool:
                        if (j.Role == Role.Teacher) return Math.Min(j.Count, Math.Max(1, (int)Math.Ceiling(Enrolled(b) / (float)Balance.KidsPerTeacher * Policy.SchoolStaffing)));
                        return j.Count;
                    case BuildingType.College:
                        if (j.Role == Role.Professor) return Math.Min(j.Count, Math.Max(1, (int)Math.Ceiling(Enrolled(b) / (float)Balance.StudentsPerProfessor * Policy.SchoolStaffing)));
                        return j.Count;
                    case BuildingType.Clinic:
                    case BuildingType.Hospital:
                        return Math.Max(1, (int)Math.Round(j.Count * Policy.HealthStaffing));
                    case BuildingType.Police:
                    case BuildingType.FireStation:
                        return (int)Math.Round(j.Count * Policy.SafetyStaffing);
                    default: return j.Count;
                }
            }
            if (d.Cat == Category.Residential || d.Cat == Category.Special) return 0;
            if (b.Type == BuildingType.Realty) return j.Role == Role.PropertyManager ? Math.Max(1, b.WantManagers) : b.WantHands;
            // a head office takes people on once it has a store open to run
            if (d.HeadOf.HasValue && StoresOf(b).Count == 0) return 0;
            bool first = d.Jobs.Length > 0 && d.Jobs[0].Role == j.Role;
            return Math.Max(first ? 1 : 0, (int)Math.Round(j.Count * b.Staffing));
        }

        public int StaffIn(Building b, Role r)
        {
            int n = 0;
            foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null && c.Role == r) n++; }
            return n;
        }

        public double OfferWage(Building b, Role r)
        {
            var rd = Defs.Get(r);
            double w = rd.Wage * (b.Def.Cat == Category.Public ? Policy.PublicPay : b.WageMult);
            return Math.Max(Policy.MinWage, Math.Round(w / 10) * 10);
        }

        /// <summary>What a particular person is paid in a role: the going rate plus a little for experience.</summary>
        public double PayFor(Building b, Citizen c, Role r)
        {
            if (r == Role.Commuter) return CommuteWage(c.Edu, c.Experience);
            return Math.Round(OfferWage(b, r) * (1 + Math.Min(0.35, c.Experience * 0.012)) / 10) * 10;
        }

        public List<Opening> Openings()
        {
            var l = new List<Opening>();
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (!b.Open || d.Jobs.Length == 0 || !Connected(b)) continue;
                foreach (var j in d.Jobs)
                {
                    int n = DesiredStaff(b, j) - StaffIn(b, j.Role);
                    if (n > 0) l.Add(new Opening { Building = b, Role = j.Role, Wage = OfferWage(b, j.Role), Count = n });
                }
            }
            return l;
        }

        /// <summary>The best-paid open role in a building that someone qualifies for (Role.None: nothing).</summary>
        public Role QualifiesFor(Citizen c, Building b)
        {
            Role best = Role.None; double bw = 0;
            foreach (var j in b.Def.Jobs)
            {
                if (!Defs.Qualifies(c.Edu, Defs.Get(j.Role).Needs)) continue;
                if (StaffIn(b, j.Role) >= Math.Max(1, DesiredStaff(b, j)) && !(b.Employees.Count == 0)) continue;
                double w = OfferWage(b, j.Role);
                if (w > bw) { bw = w; best = j.Role; }
            }
            return best;
        }

        public bool HasCar(Citizen c) { var h = HouseholdOf(c); return h != null && HasUsableCar(h); }

        /// <summary>Can they get to work there every day: on foot if it's close, otherwise by car or bus.</summary>
        public bool CanReach(Citizen c, Building work)
        {
            if (!Connected(work)) return false;
            var home = HomeOf(c);
            if (home == null || !Connected(home)) return false;
            float d = Distance(home, work);
            if (d <= Balance.WalkTiles) return true;
            if (HasCar(c)) return true;
            return BusConnects(home, work);
        }

        // ================================================================== hiring and leaving
        public void Hire(Building b, Citizen c, Role r, bool silent)
        {
            if (c.WorkId >= 0) LeaveJob(c, null);
            c.WorkId = b.Id; c.Role = r; c.Wage = PayFor(b, c, r); c.JoblessDays = 0; c.DaysWorked = 0;
            if (!b.Employees.Contains(c.Id)) b.Employees.Add(c.Id);
            if (!silent) Note(c, "Started as " + Defs.Get(r).Name.ToLower() + " at " + Ref(b) + ".");
        }

        public void LeaveJob(Citizen c, string reason)
        {
            var b = GetBuilding(c.WorkId);
            if (b != null) b.Employees.Remove(c.Id);
            c.WorkId = -1; c.Role = Role.None; c.Wage = 0; c.AtWork = false;
        }

        static string ShortName(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.Construction: return "Construction";
                case BuildingType.Trucking: return "Trucking";
                case BuildingType.ClothingStore: return "Clothing";
                case BuildingType.ElectronicsStore: return "Electronics";
                case BuildingType.FurnitureStore: return "Furniture";
                case BuildingType.FurnitureWorkshop: return "Furniture Works";
                case BuildingType.GroceryStore: return "Market";
                case BuildingType.AutoRepair: return "Auto Repair";
                case BuildingType.Office: return "Offices";
                case BuildingType.FoodPlant: return "Foods";
                case BuildingType.GarmentFactory: return "Garments";
                case BuildingType.ElectronicsPlant: return "Electronics Plant";
                case BuildingType.Realty: return "Properties";
                default: return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Defs.Get(t).Name);
            }
        }

        /// <summary>A place as it's mentioned in a sentence: "the clinic", "Garcia's Restaurant", "Northwind Market".</summary>
        public string Ref(Building b)
        {
            if (b == null) return "";
            var d = b.Def;
            bool named = d.Cat != Category.Public && d.Cat != Category.Residential && d.Cat != Category.Special && !(b.Closed && b.Built) && !string.IsNullOrEmpty(b.OwnerName);
            return named ? PlaceName(b) : "the " + PlaceName(b);
        }

        /// <summary>A place's name with a capital letter, for the start of a line or a title.</summary>
        public string Title(Building b)
        {
            var s = PlaceName(b);
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>"Northwind Market", "Garcia's Restaurant", "the clinic".</summary>
        public string PlaceName(Building b)
        {
            if (b == null) return "";
            var d = b.Def;
            if (d.Cat == Category.Public || d.Cat == Category.Residential || d.Cat == Category.Special) return d.Name.ToLower();
            if (b.Closed && b.Built) return "empty " + d.Name.ToLower();
            if (!string.IsNullOrEmpty(b.Brand)) return b.Brand;
            if (b.Owner == Owner.Household && !string.IsNullOrEmpty(b.OwnerName)) return b.OwnerName + (b.OwnerName.EndsWith("s") ? "' " : "'s ") + ShortName(b.Type);
            if (!string.IsNullOrEmpty(b.OwnerName)) return b.OwnerName;
            return d.Name.ToLower();
        }

        // ================================================================== every day
        void LaborDaily()
        {
            var open = Openings();
            var filled = new HashSet<int>();
            var seekers = People.Where(c => c.InLaborForce && c.WorkId < 0).ToList();
            Shuffle(seekers);
            foreach (var c in seekers)
            {
                c.JoblessDays++;
                int best = -1; double bestW = 0;
                for (int i = 0; i < open.Count; i++)
                {
                    var o = open[i];
                    if (o.Count <= 0) continue;
                    var need = Defs.Get(o.Role).Needs;
                    if (!Defs.Qualifies(c.Edu, need)) continue;
                    // a graduate holds out a while for a job that uses what they learned
                    if (c.Edu >= Edu.Degree && need < Edu.HighSchool && c.JoblessDays < 6) continue;
                    if (c.Edu == Edu.Trade && need == Edu.None && c.JoblessDays < 4) continue;
                    if (!CanReach(c, o.Building)) continue;
                    double w = o.Wage + (need == c.Edu ? 300 : 0);
                    if (w > bestW) { bestW = w; best = i; }
                }
                if (best < 0) continue;
                var op = open[best];
                Hire(op.Building, c, op.Role, false);
                op.Count--; open[best] = op;
                filled.Add(op.Building.Id);
                if (c.JoblessDays > 8 && Chance(0.3f)) AddLog(c.Name + " found work as " + Defs.Get(op.Role).Name.ToLower() + " after " + (c.JoblessDays / Balance.DaysPerMonth) + " months looking.", LogKind.Good, c.Id, true);
            }
            var stillOpen = new HashSet<int>();
            foreach (var o in open) if (o.Count > 0) stillOpen.Add(o.Building.Id);
            foreach (var b in Buildings)
            {
                if (b.Def.Jobs.Length == 0) continue;
                if (stillOpen.Contains(b.Id)) b.Unfilled++;
                else b.Unfilled = 0;
            }
        }

        /// <summary>Who shows up today. Everyone with a job is paid for the day, sick or not.</summary>
        void Attendance()
        {
            foreach (var c in People)
            {
                var w = GetBuilding(c.WorkId);
                if (w == null) { c.AtWork = false; continue; }
                c.DaysWorked++;
                c.AtWork = w.Open && c.Sick == 0 && !(c.Pregnant > 0 && c.Pregnant < 4);
                if (c.AtWork) c.Experience += Balance.YearPerDay;
            }
        }

        public int PresentStaff(Building b)
        {
            int n = 0;
            foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null && c.AtWork) n++; }
            return n;
        }

        // ================================================================== every month
        /// <summary>People in jobs that pay well below what they could earn elsewhere move on.</summary>
        void JobHopping()
        {
            var open = Openings();
            if (open.Count == 0) return;
            foreach (var c in People.Where(p => p.WorkId >= 0 && !p.Owner).OrderBy(p => Rand()).ToList())
            {
                bool commuter = Commutes(c);
                if (!Chance(commuter ? 0.7f : 0.35f)) continue;
                var cur = WorkOf(c);
                // a commuter takes a job in town for a bit less, to be spared the drive
                int best = -1; double bestW = commuter ? c.Wage * 0.92 : c.Wage * 1.12 + 150;
                for (int i = 0; i < open.Count; i++)
                {
                    var o = open[i];
                    if (o.Count <= 0 || o.Building == cur) continue;
                    if (!Defs.Qualifies(c.Edu, Defs.Get(o.Role).Needs) || !CanReach(c, o.Building)) continue;
                    if (o.Wage > bestW) { bestW = o.Wage; best = i; }
                }
                if (best < 0) continue;
                var op = open[best];
                double was = c.Wage;
                Hire(op.Building, c, op.Role, false);
                op.Count--; open[best] = op;
                if (commuter) { if (Chance(0.2f)) AddLog(c.Name + " stopped commuting: took a job in town as " + Defs.Get(op.Role).Name.ToLower() + " at " + Ref(op.Building) + ".", LogKind.Good, c.Id, true); }
                else if (Chance(0.25f)) AddLog(c.Name + " left " + Ref(cur) + " for a better-paid job as " + Defs.Get(op.Role).Name.ToLower() + " (" + Money(was) + " → " + Money(c.Wage) + " a month).", LogKind.Info, c.Id, true);
            }
        }

        /// <summary>Employers who couldn't hire all month raise pay; with a crowd of qualified people looking they hold it down.</summary>
        void WageSetting()
        {
            var lookingByEdu = new int[5];
            foreach (var c in People) if (c.InLaborForce && c.WorkId < 0) lookingByEdu[(int)c.Edu]++;
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (!b.Open || d.Jobs.Length == 0) continue;
                if (d.Cat == Category.Public) { RepayStaff(b); continue; }
                // short of hands: pay more, if the business is making money to pay it with
                if (b.Unfilled >= Balance.DaysPerMonth && (b.LossMonths == 0 || b.Employees.Count < d.TotalJobs / 2)) b.WageMult = Math.Min(Balance.WageHigh, b.WageMult + Balance.WageStep);
                else if (b.Unfilled == 0)
                {
                    int pool = 0; foreach (var j in d.Jobs) pool += lookingByEdu[(int)Defs.Get(j.Role).Needs];
                    if (pool > 4 && b.LastProfit < 0) b.WageMult = Math.Max(0.85f, b.WageMult - 0.02f);
                }
                RepayStaff(b);
            }
        }

        /// <summary>Everyone's pay follows the current rate for their role.</summary>
        void RepayStaff(Building b)
        {
            foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null) c.Wage = PayFor(b, c, c.Role); }
        }

        /// <summary>Lets go whoever is over the number wanted in each role: the least experienced first, never the owner.</summary>
        void TrimStaff(Building b, string why)
        {
            foreach (var j in b.Def.Jobs)
            {
                int want = DesiredStaff(b, j);
                var staff = b.Employees.Select(GetCitizen).Where(c => c != null && c.Role == j.Role && !c.Owner).OrderBy(c => c.Experience).ToList();
                int have = StaffIn(b, j.Role);
                for (int i = 0; i < staff.Count && have > want; i++, have--)
                {
                    var c = staff[i];
                    LeaveJob(c, why);
                    Note(c, "Laid off from " + Ref(b) + ".");
                    if (Chance(0.35f)) AddLog(Title(b) + " laid off " + c.Name + (why != null ? ": " + why + "." : "."), LogKind.Bad, c.Id, true);
                }
            }
        }
    }
}
