using System.Linq;
using RealLife.Game.UI;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>The inspector on the right: everything about one building or one person.</summary>
    public sealed partial class Hud
    {
        Vector2 inspScroll;
        float inspHeight = 600;
        int inspFor = -999;

        void Inspector()
        {
            var b = T.GetBuilding(gc.SelectedBuilding);
            var c = T.GetCitizen(gc.SelectedCitizen);
            if (b == null && c == null) return;
            int key = b != null ? b.Id : -c.Id - 1;
            if (key != inspFor) { inspFor = key; inspScroll = Vector2.zero; }
            float W = Screen.width / Ui.S, H = Screen.height / Ui.S;
            float w = 410, x = W - w - 10, y = 58, h = H - y - 94;
            var r = Claim(Ui.R(x, y, w, h));
            Ui.PanelBox(r);
            if (Ui.Button(Ui.R(x + w - 38, y + 8, 30, 30), "X")) { gc.ClearSelection(); return; }
            var view = Ui.R(x + 2, y + 2, w - 4, h - 4);
            var content = new Rect(0, 0, view.width - 18 * Ui.S, Mathf.Max(view.height, inspHeight * Ui.S));
            inspScroll = GUI.BeginScrollView(view, inspScroll, content);
            var rows = new Rows(12, 8, w - 46);
            if (b != null) BuildingInfo(b, rows); else CitizenInfo(c, rows);
            inspHeight = rows.Y + 20;
            GUI.EndScrollView();
        }

        // ================================================================== buildings
        void BuildingInfo(Building b, Rows rows)
        {
            var d = b.Def;
            Ui.Title(rows.Next(28, 0), T.Title(b));
            Ui.Small(rows.Next(20), d.Name + " · " + OwnerText(b));
            string status = !b.Built ? "Under construction" : b.Closed ? "Closed" : b.ForSale ? "For sale: " + Town.Money(b.Value) : Town.IsBusiness(b) ? (T.Connected(b) ? "Open" : "Open, but not connected to the highway") : "";
            if (status != "") Ui.Bold(rows.Next(22), status, b.Closed || !T.Connected(b) ? Ui.Bad : !b.Built ? Ui.Warn : Ui.Good);
            if (!b.Built)
            {
                Ui.Bar(rows.Next(10), b.BuildFraction, Ui.Warn);
                rows.Pair("Progress", Mathf.RoundToInt(b.BuildFraction * 100) + "% of " + d.Work.ToString("0") + " labour-days");
                var con = T.Contractor(b);
                rows.Pair("Builder", con != null ? T.Title(con) : "Outside contractors");
                rows.Pair("Still to pay", Town.Money(b.ContractLeft));
                Blurb(d, rows);
                return;
            }
            if (Town.IsBusiness(b)) Business(b, rows);
            else if (d.Homes > 0) Homes(b, rows);
            else if (d.Cat == Category.Public) PublicInfo(b, rows);
            else if (b.Type == BuildingType.FreightTerminal) Terminal(rows);
            Blurb(d, rows);
        }

        string OwnerText(Building b)
        {
            switch (b.Owner)
            {
                case Owner.Town: return "run by the town";
                case Owner.Household:
                    var h = T.GetHousehold(b.OwnerId);
                    return h != null ? "owned by the " + h.Name + " family" : "privately owned";
                case Owner.Firm:
                    var f = T.OwnerFirm(b);
                    if (f != null) return "owned by " + T.Title(f);
                    return "held by " + T.BankName + (b.Closed || b.ForSale || b.Def.Homes > 0 ? ", for sale" : "");
                default: return !string.IsNullOrEmpty(b.OwnerName) ? "owned by " + b.OwnerName : "privately owned";
            }
        }

        /// <summary>A link to the company that owns this, if it's a firm in town.</summary>
        void OwnerLink(Building b, Rows rows)
        {
            var f = T.OwnerFirm(b);
            if (f == null) return;
            var r = rows.Next(22, 1);
            Ui.Small(new Rect(r.x, r.y, r.width * 0.45f, r.height), "Landlord");
            if (Ui.Link(new Rect(r.x + r.width * 0.45f, r.y, r.width * 0.55f, r.height), T.Title(f))) gc.FocusBuilding(f);
        }

        /// <summary>A property firm's homes: how many, how full, the rent coming in and what it owes on them.</summary>
        void PropertyFirm(Building b, Rows rows)
        {
            var homes = T.Portfolio(b).ToList();
            int units = homes.Where(h => h.Built).Sum(h => h.Def.Homes), lived = homes.Sum(h => h.Households.Count);
            double rent = homes.Sum(h => h.Rent * h.Households.Count), value = homes.Sum(h => h.Value);
            rows.Header("Homes");
            rows.Pair("Homes to let", units + (units > 0 ? " · " + Mathf.RoundToInt(lived * 100f / units) + "% let" : ""));
            int building = homes.Count(h => !h.Built), selling = homes.Count(h => h.ForSale);
            if (building > 0) rows.Pair("Going up", building + (building == 1 ? " building" : " buildings"));
            if (selling > 0) rows.Pair("New houses for sale", selling.ToString(), Ui.Warn);
            rows.Pair("Rent coming in", Town.Money(rent) + "/mo");
            rows.Pair("Worth", Town.Money(value));
            rows.Pair("Owes the bank", Town.Money(T.Debt(b)) + " · " + Town.Money(T.LoanPayments(b)) + "/mo");
        }

        void Blurb(BuildingDef d, Rows rows)
        {
            rows.Space(8);
            var r = rows.Peek(10);
            float h = Ui.Wrap(r, Ui.Col(d.Blurb, Ui.Dim));
            rows.Space(h / Ui.S + 4);
        }

        void Business(Building b, Rows rows)
        {
            var d = b.Def;
            rows.Header("Last month");
            rows.Pair("Revenue", Town.Money(b.LastRevenue));
            rows.Pair("Wages", "-" + Town.Money(b.LastWages));
            if (b.LastSupplies > 0) rows.Pair(d.Type == BuildingType.Construction ? "Materials" : "Stock & supplies", "-" + Town.Money(b.LastSupplies));
            rows.Pair("Taxes", "-" + Town.Money(b.LastTax));
            rows.Pair("Rent, utilities, upkeep, loans", "-" + Town.Money(b.LastOther));
            rows.Pair("Profit", Town.Money(b.LastProfit), b.LastProfit >= 0 ? Ui.Good : Ui.Bad);
            rows.Pair("In the bank", Town.Money(b.Cash), b.Cash < 0 ? Ui.Bad : Ui.Text);
            if (b.LossMonths >= 2) Ui.Small(rows.Next(20), Ui.Col("Losing money " + b.LossMonths + " months running.", Ui.Bad));

            if (d.Sells.HasValue || d.Serves != Service.None || d.Makes.HasValue || b.Type == BuildingType.Office || b.Type == BuildingType.Construction) rows.Header("Trade");
            if (d.Sells.HasValue)
            {
                var g = d.Sells.Value;
                rows.Pair("Price of " + Defs.Name(g).ToLower(), Town.Money(b.Price) + PriceVs(b.Price, Defs.Retail[(int)g]));
                StockBar(rows, Defs.Name(g) + " in stock", b.Stock[(int)g], d.StockCap);
            }
            if (d.Serves != Service.None) rows.Pair("Price per " + ServiceUnit(d.Serves), Town.Money(b.Price) + PriceVs(b.Price, d.ServicePrice));
            if (d.Makes.HasValue)
            {
                var g = d.Makes.Value;
                rows.Pair("Sells " + Defs.Name(g).ToLower() + " at", Town.Money(b.Price) + " (world " + Town.Money(T.World(g)) + ")");
                StockBar(rows, Defs.Name(g) + " in store", b.Stock[(int)g], d.StockCap);
                rows.Pair("Made today", b.Produced.ToString("0.#") + " " + Defs.GoodUnits[(int)g]);
            }
            if (d.Input.HasValue) rows.Pair("Has " + Defs.Name(d.Input.Value).ToLower(), b.Stock[(int)d.Input.Value].ToString("0.#") + " " + Defs.GoodUnits[(int)d.Input.Value]);
            if (b.Type == BuildingType.Office) rows.Pair("Clients", "billed out of town");
            if (b.Type == BuildingType.Realty) PropertyFirm(b, rows);
            double owed = T.Debt(b);
            if (owed > 1) rows.Pair("Loans", Town.Money(owed) + " · " + Town.Money(T.LoanPayments(b)) + "/mo");
            if (b.Type == BuildingType.Construction)
            {
                int jobs = T.Buildings.Count(o => !o.Built && o.ContractorId == b.Id);
                rows.Pair("Sites on the go", jobs.ToString());
            }
            if (d.Sells.HasValue || d.Serves != Service.None)
            {
                rows.Pair("Customers last month", b.LastCustomers.ToString("#,0"));
                if (b.LastLost > 0) rows.Pair("Turned away", b.LastLost.ToString("#,0"), Ui.Warn);
                rows.Pair("Busy", Mathf.RoundToInt(b.LastUse * 100) + "%");
            }
            Staff(b, rows);
        }

        static string ServiceUnit(Service s)
        {
            switch (s)
            {
                case Service.Meals: return "meal";
                case Service.Coffee: return "coffee";
                case Service.Drinks: return "drink";
                case Service.Haircut: return "haircut";
                case Service.Fitness: return "visit";
                default: return "repair";
            }
        }

        static string PriceVs(double p, double normal)
        {
            double f = p / normal - 1;
            if (System.Math.Abs(f) < 0.03) return "";
            return Ui.Col(" (" + (f > 0 ? "+" : "") + Mathf.RoundToInt((float)f * 100) + "%)", f > 0 ? Ui.Warn : Ui.Good);
        }

        void StockBar(Rows rows, string label, float v, float cap)
        {
            rows.Pair(label, v.ToString("#,0") + " / " + cap.ToString("#,0"));
            Ui.Bar(rows.Next(6), cap > 0 ? v / cap : 0, v / Mathf.Max(1, cap) < 0.2f ? Ui.Warn : Ui.Accent);
        }

        void Staff(Building b, Rows rows)
        {
            var d = b.Def;
            if (d.Jobs.Length == 0) return;
            int want = 0; foreach (var j in d.Jobs) want += T.DesiredStaff(b, j);
            rows.Header("Staff: " + b.Employees.Count + (want != b.Employees.Count ? " (wants " + want + ")" : "") + " of " + d.TotalJobs + " posts");
            if (d.Cat != Category.Public) rows.Pair("Pay", Mathf.RoundToInt(b.WageMult * 100) + "% of the going rate");
            foreach (var j in d.Jobs)
            {
                int have = T.StaffIn(b, j.Role), wanted = T.DesiredStaff(b, j);
                var rd = Defs.Get(j.Role);
                string open = wanted > have ? Ui.Col("  " + (wanted - have) + " open", Ui.Warn) : "";
                rows.Pair(rd.Name + " · " + Defs.EduName(rd.Needs).ToLower(), have + "/" + wanted + "  " + Town.Money(T.OfferWage(b, j.Role)) + open);
            }
            rows.Space(4);
            foreach (var id in b.Employees.ToList())
            {
                var c = T.GetCitizen(id);
                if (c == null) continue;
                var r = rows.Next(22, 1);
                if (Ui.Link(new Rect(r.x, r.y, r.width * 0.55f, r.height), c.Name + (c.Owner ? " ★" : ""), c.AtWork ? Ui.Text : Ui.Dim)) gc.FocusCitizen(c);
                Ui.Small(new Rect(r.x + r.width * 0.55f, r.y, r.width * 0.45f, r.height), Defs.Get(c.Role).Name);
                Ui.Right(r, Town.Money(c.Wage), Ui.Dim);
            }
        }

        void Homes(Building b, Rows rows)
        {
            var d = b.Def;
            bool ownerLives = b.Owner == Owner.Household && b.Households.Contains(b.OwnerId);
            rows.Header("Homes");
            rows.Pair("Lived in", b.Households.Count + " of " + d.Homes);
            rows.Pair("Value", Town.Money(b.Value));
            if (!ownerLives) rows.Pair("Rent", Town.Money(b.Rent) + " a month" + PriceVs(b.Rent, d.Rent));
            OwnerLink(b, rows);
            if (b.ForSale) rows.Pair("For sale", Town.Money(b.Value), Ui.Warn);
            rows.Header("Who lives here");
            if (b.Households.Count == 0) Ui.Small(rows.Next(20), "Empty.");
            foreach (var hid in b.Households)
            {
                var h = T.GetHousehold(hid);
                if (h == null) continue;
                var ms = T.Members(h);
                var head = ms.FirstOrDefault(m => !m.IsChild) ?? ms.FirstOrDefault();
                var r = rows.Next(22, 1);
                if (head != null && Ui.Link(new Rect(r.x, r.y, r.width * 0.5f, r.height), "The " + h.Name + "s")) gc.FocusCitizen(head);
                Ui.Small(new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.5f, r.height), ms.Count + (ms.Count == 1 ? " person" : " people") + " · " + (h.Owns ? "own it" : "rent"));
                Ui.Right(r, Town.Money(T.MonthlyIncome(h)) + "/mo", Ui.Dim);
            }
        }

        void PublicInfo(Building b, Rows rows)
        {
            var d = b.Def;
            if (b.Type == BuildingType.Elementary || b.Type == BuildingType.HighSchool || b.Type == BuildingType.College)
            {
                rows.Header("Pupils");
                int n = T.Enrolled(b);
                rows.Pair("Enrolled", n + " of " + d.Seats);
                Ui.Bar(rows.Next(6), n / (float)d.Seats, Ui.Accent);
                float q = T.SchoolQuality(b);
                rows.Pair("Teaching", Mathf.RoundToInt(q * 100) + "%" + (q < 0.95f ? " — needs more teachers" : ""), q < 0.8f ? Ui.Warn : Ui.Good);
                if (b.Type == BuildingType.College)
                {
                    int trade = T.People.Count(c => c.SchoolId == b.Id && c.Stage == Stage.TradeSchool);
                    int deg = T.People.Count(c => c.SchoolId == b.Id && c.Stage == Stage.College);
                    int med = T.People.Count(c => c.SchoolId == b.Id && c.Stage == Stage.MedSchool);
                    rows.Pair("Trade / degree / medicine", trade + " / " + deg + " / " + med);
                    rows.Pair("Tuition", Town.Money(Balance.TuitionDegree * (1 - T.Policy.TuitionAid)) + "/mo for a degree");
                }
            }
            if (b.Type == BuildingType.Clinic || b.Type == BuildingType.Hospital)
            {
                rows.Header("Patients");
                rows.Pair("Seen today", b.ServedToday + " of " + T.PatientCapacity(b));
                if (T.LastUntreated > 0) rows.Pair("Untreated in town last month", T.LastUntreated.ToString(), Ui.Warn);
            }
            if (d.Radius > 0 && d.Radius < 900) rows.Pair("Serves within", d.Radius + " tiles");
            double wages = 0; foreach (var id in b.Employees) { var c = T.GetCitizen(id); if (c != null) wages += c.Wage; }
            rows.Pair("Running cost", Town.Money(wages + d.Upkeep) + " a month");
            Staff(b, rows);
        }

        void Terminal(Rows rows)
        {
            rows.Header("Across the town line last month");
            var tr = T.LastTrade;
            rows.Pair("Goods sold out of town", Town.Money(tr.Exports), Ui.Good);
            rows.Pair("Office work billed outside", Town.Money(tr.Services), Ui.Good);
            rows.Pair("Pensions paid in", Town.Money(tr.Pensions), Ui.Good);
            rows.Pair("Outside investment", Town.Money(tr.Investment), Ui.Good);
            rows.Pair("Imports", "-" + Town.Money(tr.Imports), Ui.Bad);
            rows.Pair("Utilities & insurance", "-" + Town.Money(tr.Utilities), Ui.Bad);
            rows.Pair("Profits to outside owners", "-" + Town.Money(tr.Profits), Ui.Bad);
            rows.Pair("Outside hauliers", "-" + Town.Money(tr.Freight), Ui.Bad);
            rows.Pair("Retirement savings", "-" + Town.Money(tr.Savings), Ui.Bad);
            double net = tr.In - tr.Outflow;
            rows.Pair("Net", Town.Money(net), net >= 0 ? Ui.Good : Ui.Bad);
        }

        // ================================================================== people
        void CitizenInfo(Citizen c, Rows rows)
        {
            var h = T.HouseholdOf(c);
            var home = T.HomeOf(h);
            Ui.Title(rows.Next(28, 0), c.Name);
            Ui.Small(rows.Next(20), (int)c.Age + (c.Female ? ", woman" : ", man") + (c.IsChild ? "" : " · " + Defs.EduName(c.Edu).ToLower()) + (c.Dropout ? " (dropped out)" : ""));
            var work = T.WorkOf(c);
            string status; Color sc = Ui.Text;
            if (work != null) status = Defs.Get(c.Role).Name + (c.Owner ? " and owner" : "") + " at " + T.Ref(work) + " · " + Town.Money(c.Wage) + "/mo";
            else if (c.Stage != Stage.None) { var s = T.GetBuilding(c.SchoolId); status = s != null ? Defs.StageName(c.Stage) + ", year " + (1 + (int)c.StageYears) : "No school place"; if (s == null) sc = Ui.Bad; }
            else if (c.Retired) status = "Retired";
            else if (c.IsChild) status = c.Age < 6 ? "Too young for school" : "Not in school";
            else if (c.InLaborForce) { status = "Looking for work" + (c.JoblessDays >= 4 ? " for " + c.JoblessDays / 4 + " months" : ""); sc = Ui.Warn; }
            else status = c.Pregnant > 0 ? "Expecting a baby" : "At home";
            var sr = rows.Next(22);
            if (work != null) { if (Ui.Link(sr, status)) gc.FocusBuilding(work); }
            else if (c.Stage != Stage.None && T.GetBuilding(c.SchoolId) != null) { if (Ui.Link(sr, status)) gc.FocusBuilding(T.GetBuilding(c.SchoolId)); }
            else Ui.Label(sr, status, sc);
            if (c.Sick > 0) Ui.Small(rows.Next(20), Ui.Col("Ill" + (c.ErrandId >= 0 && T.GetBuilding(c.ErrandId) != null && (T.GetBuilding(c.ErrandId).Type == BuildingType.Clinic || T.GetBuilding(c.ErrandId).Type == BuildingType.Hospital) ? ", seeing the doctor" : ""), Ui.Warn));
            rows.Space(4);
            var hr = rows.Next(18, 2); Ui.Small(hr, "Happiness"); Ui.Right(hr, Mathf.RoundToInt(c.Happiness) + "%");
            Ui.Bar(rows.Next(6), c.Happiness / 100f, c.Happiness >= 60 ? Ui.Good : c.Happiness >= 40 ? Ui.Warn : Ui.Bad);
            var he = rows.Next(18, 2); Ui.Small(he, "Health"); Ui.Right(he, Mathf.RoundToInt(c.Health) + "%");
            Ui.Bar(rows.Next(6), c.Health / 100f, c.Health >= 60 ? Ui.Good : c.Health >= 35 ? Ui.Warn : Ui.Bad);
            if (!c.IsChild) Ui.Small(rows.Next(20), Traits(c));
            if (!string.IsNullOrEmpty(c.Note)) { var nr = rows.Peek(10); float nh = Ui.Wrap(nr, Ui.Col(c.Note, Ui.Dim)); rows.Space(nh / Ui.S + 4); }

            // where they are
            var br = rows.Next(30);
            float bw = br.width / 3 - 4 * Ui.S;
            if (Ui.Button(new Rect(br.x, br.y, bw, br.height), "Home", false, home != null)) gc.FocusBuilding(home);
            var place = work ?? T.GetBuilding(c.SchoolId);
            if (Ui.Button(new Rect(br.x + bw + 6 * Ui.S, br.y, bw, br.height), work != null ? "Work" : "School", false, place != null)) gc.FocusBuilding(place);
            Vector3 p;
            bool out_ = gc.Agents != null && gc.Agents.Where(c.Id, out p);
            if (Ui.Button(new Rect(br.x + (bw + 6 * Ui.S) * 2, br.y, bw, br.height), gc.Following ? "Stop" : "Follow", gc.Following, out_ || gc.Following)) gc.Following = !gc.Following;

            if (h == null) return;
            rows.Header("The " + h.Name + " household");
            foreach (var m in T.Members(h))
            {
                if (m.Id == c.Id) continue;
                var r = rows.Next(22, 1);
                string rel = m.Id == c.PartnerId ? "partner" : (m.MotherId == c.Id || m.FatherId == c.Id) ? (m.Female ? "daughter" : "son") : (c.MotherId == m.Id ? "mother" : c.FatherId == m.Id ? "father" : "");
                if (Ui.Link(new Rect(r.x, r.y, r.width * 0.5f, r.height), m.Name)) gc.FocusCitizen(m);
                Ui.Small(new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.5f, r.height), (int)m.Age + (rel != "" ? " · " + rel : ""));
            }
            if (home != null)
            {
                var r = rows.Next(22, 2);
                Ui.Small(r, h.Owns ? "Owns" : "Rents");
                if (Ui.Link(new Rect(r.x + 60 * Ui.S, r.y, r.width - 60 * Ui.S, r.height), (home.Type == BuildingType.Apartments ? "a flat" : "a house") + (h.Owns ? "" : " · " + Town.Money(home.Rent) + "/mo"))) gc.FocusBuilding(home);
            }
            else Ui.Small(rows.Next(20), Ui.Col("Homeless", Ui.Bad));
            rows.Pair("Savings", Town.Money(h.Cash), h.Cash < 0 ? Ui.Bad : Ui.Text);
            double owed = T.DebtOwed(h);
            if (owed > 0) rows.Pair("Owes", Town.Money(owed) + " (" + Town.Money(T.DebtPayments(h)) + "/mo)", h.MissedRent > 0 ? Ui.Bad : Ui.Text);
            if (h.Cars > 0) rows.Pair("Cars", h.Cars + (h.CarBroken ? " (broken down)" : ""));
            rows.Header("Last month's money");
            double tin = 0, tout = 0;
            for (int i = 0; i < HouseholdBook.Count; i++)
            {
                if (h.LastIn[i] > 0.5 && HouseholdBook.InNames[i] != "" && i != HouseholdBook.Loans) { rows.Pair(HouseholdBook.InNames[i], "+" + Town.Money(h.LastIn[i]), Ui.Good); tin += h.LastIn[i]; }
            }
            for (int i = 0; i < HouseholdBook.Count; i++)
            {
                if (h.LastOut[i] > 0.5) { rows.Pair(HouseholdBook.OutNames[i], "-" + Town.Money(h.LastOut[i])); tout += h.LastOut[i]; }
            }
            if (tin > 0 || tout > 0) rows.Pair("Left over", Town.Money(tin - tout + h.LastIn[HouseholdBook.Loans]), tin - tout >= 0 ? Ui.Good : Ui.Bad);
            var loans = T.Loans.Where(l => l.HouseholdId == h.Id && l.Kind != LoanKind.Business).ToList();
            if (loans.Count > 0)
            {
                rows.Header("Loans");
                foreach (var l in loans)
                    rows.Pair(LoanName(l), Town.Money(l.Principal) + (l.Deferred ? " (still studying)" : " · " + Town.Money(l.Payment) + "/mo"), l.Missed > 0 ? Ui.Bad : Ui.Text);
            }
        }

        string LoanName(Loan l)
        {
            switch (l.Kind)
            {
                case LoanKind.Mortgage: return "Mortgage";
                case LoanKind.Car: return "Car loan";
                case LoanKind.Student:
                    var c = T.GetCitizen(l.CitizenId);
                    return "Student loan" + (c != null ? " (" + c.First + ")" : "");
                default: return "Business loan";
            }
        }

        static string Traits(Citizen c)
        {
            string a = c.Ambition > 0.7f ? "Ambitious" : c.Ambition < 0.3f ? "Easy-going" : "Steady";
            string t = c.Thrift > 0.7f ? "careful with money" : c.Thrift < 0.3f ? "spends freely" : "sensible with money";
            return a + ", " + t + ". " + (c.Experience >= 1 ? (int)c.Experience + " years working." : "");
        }
    }
}
