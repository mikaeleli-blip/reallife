using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// Lives: being born, growing up, school and college, health, marrying, moving out of your parents' place, retiring,
    /// dying, and coming to town or leaving it.
    /// </summary>
    public partial class Town
    {
        // ================================================================== making people
        public Citizen NewPerson(string last, bool female, float age, Edu edu)
        {
            var c = new Citizen
            {
                Id = NextCitizen++, First = Pick(female ? Names.Female : Names.Male), Last = last, Female = female, Age = Math.Max(0, age), Edu = edu,
                Seed = Rand(), Ambition = Clamp01(Gauss(0.5f, 0.2f)), Thrift = Clamp01(Gauss(0.5f, 0.2f)), ArrivedDay = Day,
                Health = Math.Min(100, Math.Max(35, 96 - Math.Max(0, age - 45) * 0.6f + Gauss(0, 5))), Happiness = 60
            };
            c.BornDay = Day - (int)(c.Age * Balance.DaysPerYear);
            if (!c.IsChild) c.Elementary = 1;
            People.Add(c); peopleById[c.Id] = c;
            return c;
        }

        public Household NewHousehold(string name)
        {
            var h = new Household { Id = NextHousehold++, Name = name, MovedIn = Day };
            Households.Add(h); householdsById[h.Id] = h;
            return h;
        }

        public static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }

        void Join(Household h, Citizen c)
        {
            var old = GetHousehold(c.HouseholdId);
            if (old == h) return;
            if (old != null) old.Members.Remove(c.Id);
            c.HouseholdId = h.Id;
            if (!h.Members.Contains(c.Id)) h.Members.Add(c.Id);
            if (old != null && old.Members.Count == 0) Dissolve(old, h);
        }

        void Marry(Citizen a, Citizen b) { a.PartnerId = b.Id; b.PartnerId = a.Id; }

        public void MoveIn(Household h, Building home, bool owns)
        {
            if (h.HomeId >= 0) MoveOut(h);
            h.HomeId = home.Id; h.Owns = owns; h.MovedIn = Day; h.MissedRent = 0;
            if (!home.Households.Contains(h.Id)) home.Households.Add(h.Id);
            home.VacantDays = 0;
            if (owns) { home.Owner = Owner.Household; home.OwnerId = h.Id; home.OwnerName = h.Name; home.ForSale = false; }
        }

        void MoveOut(Household h)
        {
            var home = GetBuilding(h.HomeId);
            if (home != null) home.Households.Remove(h.Id);
            h.HomeId = -1; h.Owns = false;
        }

        /// <summary>An empty household goes. What it owned passes to the household its last member joined (or is sold if none).</summary>
        void Dissolve(Household h, Household heir)
        {
            var home = GetBuilding(h.HomeId);
            MoveOut(h);
            if (heir != null) heir.Cash += h.Cash;
            HandOverCars(h, heir);
            foreach (var b in Buildings)
            {
                if (b.Owner != Owner.Household || b.OwnerId != h.Id) continue;
                if (heir != null) { b.OwnerId = heir.Id; b.OwnerName = heir.Name; }
                else SellLocally(b);
            }
            foreach (var l in Loans.ToList())
            {
                if (l.HouseholdId != h.Id) continue;
                if (heir != null) l.HouseholdId = heir.Id;
                else if (l.Kind == LoanKind.Business) l.HouseholdId = -1;   // a business's loan stays with the business
                else Loans.Remove(l);
            }
            if (home != null && home.Owner == Owner.Household && home.OwnerId == (heir != null ? heir.Id : h.Id) && home.Households.Count == 0)
            {
                // the house they lived in is now let out by its owner
                home.Rent = home.Def.Rent * HousingIndex;
            }
            Households.Remove(h); householdsById.Remove(h.Id);
        }

        /// <summary>Someone leaves the town for good (or dies): job, school, partner, household.</summary>
        void RemovePerson(Citizen c)
        {
            LeaveJob(c, null);
            c.SchoolId = -1;
            var p = GetCitizen(c.PartnerId); if (p != null) p.PartnerId = -1;
            var h = GetHousehold(c.HouseholdId);
            People.Remove(c); peopleById.Remove(c.Id);
            if (h != null)
            {
                h.Members.Remove(c.Id);
                if (h.Members.Count == 0) Dissolve(h, null);
                else if (h.Members.All(id => GetCitizen(id).IsChild))
                {
                    // orphans go to live with a relative out of town
                    foreach (var id in h.Members.ToList()) { var k = GetCitizen(id); People.Remove(k); peopleById.Remove(k.Id); }
                    h.Members.Clear(); Dissolve(h, null);
                }
            }
        }

        public List<Citizen> Members(Household h)
        {
            var l = new List<Citizen>();
            if (h == null) return l;
            foreach (var id in h.Members) { var c = GetCitizen(id); if (c != null) l.Add(c); }
            return l;
        }

        public float FoodNeed(Household h)
        {
            float n = 0;
            foreach (var c in Members(h)) n += c.IsChild ? Balance.ChildFood : 1f;
            return n;
        }

        // ================================================================== every day
        void PeopleDaily()
        {
            foreach (var c in People.ToList())
            {
                if (!peopleById.ContainsKey(c.Id)) continue;
                float before = c.Age;
                c.Age += Balance.YearPerDay;
                // health
                if (c.Age > 55) c.Health -= (c.Age - 55) * 0.0035f;
                if (c.Sick > 0)
                {
                    c.Health -= 1.2f; c.Sick--;
                    if (c.Sick == 0) c.Health += 4;
                }
                else if (Chance(0.004f + Math.Max(0, c.Age - 50) * 0.0002f + (c.Health < 50 ? 0.004f : 0)))
                {
                    c.Sick = RandInt(2, 6); c.Health -= RandInt(3, 12);
                }
                else c.Health = Math.Min(100, c.Health + 0.15f);
                // death
                double annual = c.Age < 1 ? 0.004 : c.Age < 50 ? 0.0015 : 0.0015 * Math.Exp((c.Age - 50) / 8.5);
                if (c.Health < 30) annual *= 3; if (c.Health < 12) annual *= 4;
                if (c.Health <= 0 || Chance((float)(annual / Balance.DaysPerYear)))
                {
                    Die(c);
                    continue;
                }
                // pregnancy
                if (c.Pregnant > 0 && --c.Pregnant == 0) GiveBirth(c);
                // growing up
                if (before < Balance.SchoolAge && c.Age >= Balance.SchoolAge) Enrol(c);
                else if (before < Balance.HighSchoolAge && c.Age >= Balance.HighSchoolAge && c.Stage == Stage.Elementary)
                {
                    c.Elementary = Math.Min(1f, c.StageYears / Balance.YearsElementary);
                    c.Stage = Stage.HighSchool; c.StageYears = 0; c.SchoolId = -1; Enrol(c);
                }
                else if (before < Balance.AdultAge && c.Age >= Balance.AdultAge) FinishHighSchool(c);
                Study(c);
                // retiring
                if (!c.IsChild && !c.Retired && !c.IsStudent && c.Age >= Balance.RetireAge && (c.Age >= 70 || Chance(0.06f)))
                {
                    c.Retired = true;
                    if (c.WorkId >= 0) { var w = WorkOf(c); AddLog(c.Name + " retired" + (w != null ? " from the " + w.Def.Name.ToLower() : "") + ".", LogKind.Info, c.Id, true); }
                    LeaveJob(c, "retired");
                    Note(c, "Retired.");
                }
            }
        }

        void Die(Citizen c)
        {
            var w = WorkOf(c);
            AddLog(c.Name + " died, aged " + (int)c.Age + ".", LogKind.Death, c.Id, true);
            DeathsMonth++;
            var h = HouseholdOf(c);
            RemovePerson(c);
        }

        void GiveBirth(Citizen mum)
        {
            var h = HouseholdOf(mum);
            if (h == null) return;
            var dad = GetCitizen(mum.PartnerId);
            var baby = NewPerson(dad != null ? dad.Last : mum.Last, Chance(0.49f), 0, Edu.None);
            baby.MotherId = mum.Id; baby.FatherId = dad != null ? dad.Id : -1;
            baby.Elementary = 0; baby.Health = 95;
            Join(h, baby);
            BirthsMonth++;
            AddLog(mum.Name + (dad != null ? " and " + dad.First : "") + " had a baby " + (baby.Female ? "girl" : "boy") + ", " + baby.First + ".", LogKind.Birth, baby.Id, true);
            Note(mum, "Had a baby, " + baby.First + ".");
        }

        // ================================================================== school
        public int Enrolled(Building s) { int n = 0; foreach (var c in People) if (c.SchoolId == s.Id) n++; return n; }

        BuildingType SchoolFor(Stage s)
        {
            switch (s)
            {
                case Stage.Elementary: return BuildingType.Elementary;
                case Stage.HighSchool: return BuildingType.HighSchool;
                default: return BuildingType.College;
            }
        }

        /// <summary>Finds a seat at the nearest school with room. Kids who can't get one learn nothing that year.</summary>
        public void Enrol(Citizen c)
        {
            if (c.Stage == Stage.None)
            {
                if (c.Age >= Balance.AdultAge || c.Age < Balance.SchoolAge) return;
                c.Stage = c.Age < Balance.HighSchoolAge ? Stage.Elementary : Stage.HighSchool;
                c.StageYears = 0;
            }
            var home = HomeOf(c);
            var type = SchoolFor(c.Stage);
            Building best = null; float bestScore = float.MaxValue;
            foreach (var b in Buildings)
            {
                if (b.Type != type || !b.Open || !Connected(b)) continue;
                var d = b.Def;
                float dist = home != null ? Distance(home, b) : 0;
                if (dist > d.Radius) continue;
                int n = Enrolled(b);
                if (n >= d.Seats) continue;
                float score = dist + n * 0.05f;
                if (score < bestScore) { bestScore = score; best = b; }
            }
            c.SchoolId = best != null ? best.Id : -1;
        }

        /// <summary>How well a school is teaching: 1 when it has enough teachers in today for its pupils.</summary>
        public float SchoolQuality(Building s)
        {
            if (s == null || !s.Open) return 0;
            int pupils = Enrolled(s);
            if (pupils == 0) return 1;
            int staff = 0;
            foreach (var id in s.Employees)
            {
                var t = GetCitizen(id);
                if (t != null && (t.Role == Role.Teacher || t.Role == Role.Professor) && t.Sick == 0) staff++;
            }
            int per = s.Type == BuildingType.College ? Balance.StudentsPerProfessor : Balance.KidsPerTeacher;
            return Math.Min(1f, staff * per / (float)pupils);
        }

        void Study(Citizen c)
        {
            if (c.Stage == Stage.None) return;
            var s = GetBuilding(c.SchoolId);
            if (s == null)
            {
                if (Day % Balance.DaysPerMonth == 1) Enrol(c);
                return;
            }
            float q = SchoolQualityCached(s);
            float gain = Balance.YearPerDay * q * (c.Sick > 0 ? 0.5f : 1f);
            if (c.Stage == Stage.HighSchool) gain *= 0.6f + 0.4f * c.Elementary;
            if (c.Stage >= Stage.TradeSchool) gain *= 0.75f + 0.5f * c.Ambition;
            c.StageYears += gain;
            if (c.Stage == Stage.TradeSchool && c.StageYears >= Balance.YearsTrade) Graduate(c, Edu.Trade);
            else if (c.Stage == Stage.College && c.StageYears >= Balance.YearsDegree) Graduate(c, Edu.Degree);
            else if (c.Stage == Stage.MedSchool && c.StageYears >= Balance.YearsMedical) Graduate(c, Edu.Medical);
            else if (c.Stage >= Stage.TradeSchool && c.StageYears < 0.5f && Day - c.ArrivedDay > 0 && Chance(0.0015f * (1.2f - c.Ambition)))
            {
                AddLog(c.Name + " dropped out of " + Defs.StageName(c.Stage).ToLower() + ".", LogKind.Bad, c.Id, true);
                Note(c, "Dropped out of " + Defs.StageName(c.Stage).ToLower() + ".");
                c.Stage = Stage.None; c.SchoolId = -1; StartRepaying(c);
            }
        }

        [NonSerialized] Dictionary<int, float> qualityCache = new Dictionary<int, float>();
        float SchoolQualityCached(Building s)
        {
            if (qualityCache == null) qualityCache = new Dictionary<int, float>();
            float q;
            if (!qualityCache.TryGetValue(s.Id, out q)) { q = SchoolQuality(s); qualityCache[s.Id] = q; }
            return q;
        }

        void FinishHighSchool(Citizen c)
        {
            if (c.Stage != Stage.HighSchool && c.Stage != Stage.Elementary) { c.Stage = Stage.None; c.SchoolId = -1; return; }
            float done = c.StageYears * (0.6f + 0.4f * c.Elementary);
            bool hadSchool = c.Stage == Stage.HighSchool;
            c.Stage = Stage.None; c.SchoolId = -1; c.StageYears = 0;
            if (hadSchool && done >= Balance.YearsHighSchool * 0.72f)
            {
                c.Edu = Edu.HighSchool;
                Note(c, "Graduated from high school.");
                if (Chance(0.25f)) AddLog(c.Name + " graduated from high school.", LogKind.Good, c.Id, true);
                ChooseCollege(c);
            }
            else
            {
                c.Dropout = true; c.Edu = Edu.None;
                Note(c, "Turned 18 without a high school diploma.");
                AddLog(c.Name + " turned 18 without a high school diploma" + (Buildings.Any(b => b.Type == BuildingType.HighSchool && b.Open) ? "." : ": there's no high school in town."), LogKind.Bad, c.Id, true);
            }
        }

        public Building OpenCollege() { return Buildings.FirstOrDefault(b => b.Type == BuildingType.College && b.Open && Connected(b)); }
        bool HasHospital() { return Buildings.Any(b => b.Type == BuildingType.Hospital && b.Open); }

        public double Tuition(Stage s) { return s == Stage.TradeSchool ? Balance.TuitionTrade : s == Stage.College ? Balance.TuitionDegree : s == Stage.MedSchool ? Balance.TuitionMedical : 0; }

        /// <summary>A new graduate (or someone retraining) decides on college or trade school, if there is one.</summary>
        void ChooseCollege(Citizen c)
        {
            var col = OpenCollege();
            if (col == null || Enrolled(col) >= col.Def.Seats) return;
            var h = HouseholdOf(c);
            bool parentsDegree = Members(h).Any(m => m.Id != c.Id && !m.IsChild && m.Edu >= Edu.Degree);
            float aid = Policy.TuitionAid;
            float pDegree = c.Ambition * 0.75f + (parentsDegree ? 0.2f : 0) + aid * 0.25f - 0.15f;
            float pTrade = 0.35f + c.Ambition * 0.2f + aid * 0.15f;
            Stage s;
            if (Chance(pDegree)) s = Stage.College;
            else if (Chance(pTrade)) s = Stage.TradeSchool;
            else return;
            StartStudies(c, s, col);
        }

        void StartStudies(Citizen c, Stage s, Building col)
        {
            LeaveJob(c, null);
            c.Stage = s; c.StageYears = 0; c.SchoolId = col.Id;
            Note(c, "Started " + Defs.StageName(s).ToLower() + ".");
            if (Chance(0.4f)) AddLog(c.Name + " started " + Defs.StageName(s).ToLower() + " at the community college.", LogKind.Good, c.Id, true);
        }

        void Graduate(Citizen c, Edu e)
        {
            c.Edu = e; c.Stage = Stage.None; c.SchoolId = -1; c.StageYears = 0;
            string what = e == Edu.Trade ? "a trade certificate" : e == Edu.Degree ? "a college degree" : "a medical degree";
            AddLog(c.Name + " graduated with " + what + ".", LogKind.Good, c.Id, true);
            Note(c, "Graduated with " + what + ".");
            if (e == Edu.Degree && HasHospital() && c.Ambition > 0.65f && Chance(0.35f))
            {
                var col = OpenCollege();
                if (col != null) { StartStudies(c, Stage.MedSchool, col); return; }
            }
            StartRepaying(c);
        }

        void StartRepaying(Citizen c)
        {
            foreach (var l in Loans) if (l.Kind == LoanKind.Student && l.Deferred && l.CitizenId == c.Id)
                {
                    l.Deferred = false; l.Payment = Annuity(l.Principal, l.Rate, Balance.StudentMonths);
                }
        }

        // ================================================================== every month
        void PeopleMonthly()
        {
            Births();
            LeaveHome();
            Weddings();
            BackToSchool();
            Happiness();
            Emigration();
            Immigration();
            Settlers();
        }

        void Births()
        {
            foreach (var h in Households.ToList())
            {
                var ms = Members(h);
                int kids = ms.Count(m => m.IsChild);
                foreach (var w in ms)
                {
                    if (!w.Female || w.Age < 21 || w.Age > 43 || w.Pregnant > 0 || w.IsStudent) continue;
                    var p = GetCitizen(w.PartnerId);
                    if (p == null || p.HouseholdId != h.Id) continue;
                    if (kids >= 3 && !Chance(0.15f)) continue;
                    float pr = kids == 0 ? 0.045f : kids == 1 ? 0.035f : 0.018f;
                    if (h.Cash < 4000) pr *= 0.6f;
                    if (w.Age > 37) pr *= 0.5f;
                    var home = HomeOf(h);
                    if (home != null && home.Type == BuildingType.Apartments && kids >= 2) pr *= 0.5f;
                    if (Chance(pr)) { w.Pregnant = 36; Note(w, "Expecting a baby."); }
                }
            }
        }

        /// <summary>Grown children with a job move into a place of their own when they can afford one.</summary>
        void LeaveHome()
        {
            foreach (var c in People.ToList())
            {
                if (c.Age < 20 || c.IsStudent || c.WorkId < 0 || c.PartnerId >= 0) continue;
                var h = HouseholdOf(c);
                if (h == null || h.Members.Count < 2) continue;
                bool withParents = h.Members.Contains(c.MotherId) || h.Members.Contains(c.FatherId);
                if (!withParents && h.Members.Count > 1 && !(Members(h).Count(m => !m.IsChild) > 2)) continue;
                if (!Chance(c.Age > 26 ? 0.35f : 0.18f)) continue;
                double take = c.Wage * (1 - Policy.IncomeTax);
                var home = FindHome(1, take, 0.4);
                if (home == null) continue;
                var nh = NewHousehold(c.Last);
                double gift = Math.Min(Math.Max(0, h.Cash * 0.08), 6000) + c.Wage * c.Thrift * 2;
                gift = Math.Min(gift, Math.Max(0, h.Cash * 0.5));
                h.Cash -= gift; nh.Cash = gift;
                Join(nh, c);
                MoveIn(nh, home, false);
                nh.Pantry = 2; nh.Furniture = 0.3f; nh.Electronics = 0.4f; nh.Clothes = h.Clothes;
                Note(c, "Moved into a place of their own.");
                if (Chance(0.3f)) AddLog(c.Name + " moved out of " + (c.Female ? "her" : "his") + " parents' place into " + DescribeHome(home) + ".", LogKind.Info, c.Id, true);
            }
        }

        string DescribeHome(Building b) { return b.Type == BuildingType.Apartments ? "a flat" : "a house"; }

        void Weddings()
        {
            var singles = People.Where(c => !c.IsChild && c.PartnerId < 0 && c.Age >= 21 && c.Age < 55 && !c.Retired).ToList();
            Shuffle(singles);
            var taken = new HashSet<int>();
            foreach (var a in singles)
            {
                if (taken.Contains(a.Id) || !Chance(0.05f)) continue;
                var b = singles.FirstOrDefault(o => !taken.Contains(o.Id) && o.Id != a.Id && o.HouseholdId != a.HouseholdId && Math.Abs(o.Age - a.Age) < 9 &&
                                                    (o.Female != a.Female || Chance(0.06f)) && o.MotherId != a.MotherId);
                if (b == null) continue;
                taken.Add(a.Id); taken.Add(b.Id);
                MoveTogether(a, b);
            }
        }

        void MoveTogether(Citizen a, Citizen b)
        {
            var ha = HouseholdOf(a); var hb = HouseholdOf(b);
            if (ha == null || hb == null) return;
            // whoever has the better place keeps it: owned beats rented, a house beats a flat, the one living alone beats the one at their parents'
            Func<Household, Citizen, int> score = (h, c) =>
            {
                var home = HomeOf(h); int s = 0;
                if (home == null) return -100;
                if (h.Owns) s += 4;
                if (home.Type == BuildingType.House) s += 2;
                if (h.Members.Contains(c.MotherId) || h.Members.Contains(c.FatherId)) s -= 3;
                return s;
            };
            bool aStays = score(ha, a) >= score(hb, b);
            var stay = aStays ? ha : hb; var go = aStays ? hb : ha; var mover = aStays ? b : a; var keeper = aStays ? a : b;
            bool goAlone = !(go.Members.Contains(mover.MotherId) || go.Members.Contains(mover.FatherId));
            Marry(a, b);
            if (goAlone)
            {
                // the mover's whole household comes along (their kids too)
                foreach (var m in Members(go)) Join(stay, m);
            }
            else
            {
                double share = Math.Min(Math.Max(0, go.Cash * 0.1), 5000);
                go.Cash -= share; stay.Cash += share;
                Join(stay, mover);
            }
            Note(a, "Moved in with " + b.First + "."); Note(b, "Moved in with " + a.First + ".");
            AddLog(a.Name + " and " + b.Name + " moved in together.", LogKind.Good, a.Id, true);
        }

        /// <summary>People out of work for a while, or stuck in low pay, go back to college to learn a trade.</summary>
        void BackToSchool()
        {
            var col = OpenCollege();
            if (col == null) return;
            int seats = col.Def.Seats - Enrolled(col);
            foreach (var c in People.OrderBy(p => Rand()).ToList())
            {
                if (seats <= 0) break;
                if (c.IsChild || c.Retired || c.IsStudent || c.Age > 45 || c.Edu >= Edu.Trade || c.Owner) continue;
                bool stuck = c.JoblessDays >= 8 || (c.WorkId >= 0 && c.Wage < 2700 && c.Age < 30);
                if (!stuck || c.Ambition < 0.4f || !Chance(0.12f + c.Ambition * 0.1f)) continue;
                var h = HouseholdOf(c);
                if (h == null) continue;
                Stage s = c.Edu == Edu.HighSchool && c.Ambition > 0.65f && c.Age < 32 && Chance(0.5f) ? Stage.College : c.Edu == Edu.HighSchool ? Stage.TradeSchool : Stage.None;
                if (s == Stage.None) continue;
                // a family needs another income while they study
                bool otherEarner = Members(h).Any(m => m.Id != c.Id && m.WorkId >= 0);
                if (!otherEarner && h.Cash < Tuition(s) * 12) continue;
                StartStudies(c, s, col); seats--;
                AddLog(c.Name + " went back to school for " + (s == Stage.College ? "a degree" : "a trade") + ".", LogKind.Good, c.Id, true);
            }
        }

        // ================================================================== happiness
        public float Coverage(Building home, BuildingType type)
        {
            if (home == null) return 0;
            foreach (var b in Buildings)
                if (b.Type == type && b.Open && b.Employees.Count > 0 && Distance(home, b) <= b.Def.Radius) return 1;
            return 0;
        }

        void Happiness()
        {
            foreach (var h in Households)
            {
                var home = HomeOf(h);
                var ms = Members(h);
                double income = 0; foreach (var k in h.LastIn) income += k;
                double spend = 0; foreach (var k in h.LastOut) spend += k;
                double housing = h.LastOut[HouseholdBook.Housing];
                bool kids = ms.Any(m => m.IsChild && m.Age >= Balance.SchoolAge);
                float health = Coverage(home, BuildingType.Clinic) + Coverage(home, BuildingType.Hospital) > 0 ? 1 : 0;
                float safety = (Coverage(home, BuildingType.Police) + Coverage(home, BuildingType.FireStation)) * 0.5f;
                float park = Coverage(home, BuildingType.Park);
                float sum = 0; int n = 0;
                foreach (var c in ms)
                {
                    if (c.IsChild && c.Age < 12) continue;
                    float t = 52;
                    if (home == null) t -= 40;
                    if (c.WorkId >= 0) t += 10 + Math.Min(8, (float)(c.Wage / 1000));
                    else if (c.InLaborForce) t -= c.JoblessDays > 8 ? 22 : 10;
                    if (c.Retired) t += 6;
                    if (c.IsStudent && c.SchoolId >= 0) t += 6;
                    if (spend > 0) { double months = h.Cash / Math.Max(500, spend); t += months > 6 ? 10 : months > 2 ? 4 : months > 0.5 ? -4 : -14; }
                    if (income > 0 && housing / income > 0.45) t -= 10;
                    if (h.MissedRent > 0) t -= 12;
                    if (h.Pantry <= 0.01f) t -= 18;
                    if (c.Health < 50) t -= 10;
                    t += health * 5 + safety * 6 + park * 5;
                    if (kids && ms.Any(m => m.IsChild && m.Stage != Stage.None && m.SchoolId < 0)) t -= 12;
                    if (Commutes(c)) t -= 5;   // the drive
                    else if (c.Edu >= Edu.Degree && c.WorkId >= 0 && Defs.Get(c.Role).Needs < Edu.HighSchool) t -= 8;   // overqualified
                    if (h.Members.Count > (home != null && home.Type == BuildingType.Apartments ? 4 : 6)) t -= 6;
                    t += Math.Min(8, c.LastOutings * 1.5f);
                    c.Happiness = Math.Max(0, Math.Min(100, c.Happiness * 0.6f + t * 0.4f));
                    sum += c.Happiness; n++;
                }
                if (n > 0 && sum / n < 32) h.SadMonths++; else h.SadMonths = 0;
            }
        }

        public float AverageHappiness()
        {
            float s = 0; int n = 0;
            foreach (var c in People) if (!c.IsChild) { s += c.Happiness; n++; }
            return n > 0 ? s / n : 0;
        }

        // ================================================================== moving in and out of town
        void Emigration()
        {
            foreach (var h in Households.ToList())
            {
                var ms = Members(h);
                var adults = ms.Where(m => !m.IsChild).ToList();
                if (adults.Count == 0) continue;
                bool broke = adults.Where(a => a.InLaborForce).All(a => a.WorkId < 0 && a.JoblessDays >= 12) && adults.Any(a => a.InLaborForce) && h.Cash < 6000;
                bool homeless = h.HomeId < 0;
                bool miserable = h.SadMonths >= 4;
                bool evicted = h.MissedRent >= 3;
                if (!(broke && Chance(0.5f)) && !(homeless && Chance(0.6f)) && !(miserable && Chance(0.3f)) && !evicted) continue;
                string why = evicted ? "couldn't pay the rent" : homeless ? "had nowhere to live" : broke ? "couldn't find work" : "weren't happy here";
                Leave(h, why);
            }
        }

        void Leave(Household h, string why)
        {
            var ms = Members(h);
            AddLog("The " + h.Name + " family left town: they " + why + ".", LogKind.Bad);
            LeftMonth += ms.Count;
            // an owned home is sold before they go
            var home = HomeOf(h);
            foreach (var b in Buildings.ToList())
                if (b.Owner == Owner.Household && b.OwnerId == h.Id) SellLocally(b);
            foreach (var c in ms) { if (c.Owner) { var w = WorkOf(c); if (w != null) CloseBusiness(w, c.Name + " left town."); } }
            foreach (var c in ms) { LeaveJob(c, null); c.SchoolId = -1; People.Remove(c); peopleById.Remove(c.Id); var p = GetCitizen(c.PartnerId); if (p != null && p.HouseholdId != h.Id) p.PartnerId = -1; }
            h.Members.Clear();
            Dissolve(h, null);
        }

        /// <summary>Job openings nobody local can take bring people in from outside, if there's a home for them.</summary>
        void Immigration()
        {
            if (!Policy.Immigration) return;
            int cap = 6 + Population / 25;
            int came = 0;
            var openings = Openings().Where(o => o.Building.Unfilled >= 3).OrderByDescending(o => o.Wage).ToList();
            foreach (var o in openings)
            {
                if (came >= cap) break;
                if (!Chance(0.7f)) continue;
                // somebody already here could do it
                if (People.Any(c => ((c.InLaborForce && c.WorkId < 0) || Commutes(c)) && Defs.Qualifies(c.Edu, Defs.Get(o.Role).Needs) && CanReach(c, o.Building))) continue;
                bool family = Chance(0.5f);
                double take = o.Wage * (1 - Policy.IncomeTax) * (family ? 1.6 : 1);
                var home = FindHome(family ? 3 : 1, take, 0.42);
                if (home == null) { Wants[(int)BuildingType.House] += 1; continue; }
                var edu = Defs.Get(o.Role).Needs;
                var h = Arrive(home, edu, family);
                var worker = Members(h).FirstOrDefault(m => Defs.Qualifies(m.Edu, edu) && !m.IsChild);
                if (worker != null) Hire(o.Building, worker, o.Role, false);
                came++;
            }
            // a few people move here because they like it
            if (AverageHappiness() > 62 && VacantUnits() > 6 && Chance(0.5f))
            {
                var home = FindHome(2, 4000, 0.45);
                if (home != null) { Arrive(home, Chance(0.5f) ? Edu.HighSchool : Edu.Trade, Chance(0.5f)); came++; }
            }
        }

        Household Arrive(Building home, Edu edu, bool family)
        {
            var h = Newcomers(edu, family);
            MoveIn(h, home, false);
            ArrivalsMonth += h.Members.Count;
            var a = Members(h).First();
            AddLog("The " + h.Name + " " + (family ? "family" : "household") + " moved to town" + (edu >= Edu.Trade ? " (" + Defs.EduName(edu).ToLower() + ")" : "") + ".", LogKind.Good, a.Id, true);
            Note(a, "Moved to " + Name + ".");
            return h;
        }

        /// <summary>A household from outside, not yet living anywhere: one adult with this schooling, and with a family a partner
        /// and maybe children.</summary>
        Household Newcomers(Edu edu, bool family)
        {
            string last = Pick(Names.Last);
            var h = NewHousehold(last);
            float age = 22 + Rand() * 30;
            var a = NewPerson(last, Chance(0.5f), age, edu);
            Join(h, a);
            if (family)
            {
                float r = Rand();
                var b = NewPerson(last, !a.Female, Math.Max(21, age + Gauss(0, 3)), r < 0.15f ? Edu.None : r < 0.6f ? Edu.HighSchool : r < 0.75f ? Edu.Trade : Edu.Degree);
                Join(h, b); Marry(a, b);
                int kids = RandInt(0, 3);
                for (int k = 0; k < kids; k++)
                {
                    float ka = Math.Min(Math.Max(0, Math.Min(a.Age, b.Age) - 21), Rand() * 16);
                    var c = NewPerson(last, Chance(0.5f), ka, Edu.None);
                    c.MotherId = a.Female ? a.Id : b.Id; c.FatherId = a.Female ? b.Id : a.Id;
                    c.Elementary = Math.Min(1f, Math.Max(0, (ka - 6) / 6f));
                    c.StageYears = ka >= 12 ? (ka - 12) * 0.9f : 0;
                    Join(h, c);
                    Enrol(c);
                }
            }
            foreach (var m in Members(h)) m.Experience = Math.Max(0, m.Age - 22) * 0.7f;
            h.Cash = 4000 + Rand() * 22000 + (edu >= Edu.Degree ? 12000 : 0);
            h.Pantry = 2;
            if (Chance(0.75f)) GiveCar(h, edu);
            h.Fuel = CarCount(h);
            h.Clothes = 0.6f; h.Electronics = 0.5f; h.Furniture = 0.35f;
            return h;
        }
    }
}
