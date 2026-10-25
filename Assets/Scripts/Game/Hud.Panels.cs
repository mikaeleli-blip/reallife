using System.Collections.Generic;
using System.Linq;
using RealLife.Game.UI;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>The big panels on the left: the laws and the budget, the economy, the people, the news.</summary>
    public sealed partial class Hud
    {
        Vector2 panelScroll;
        float panelHeight = 800;
        Panel scrolledFor;
        bool confirmNew;

        Rows BeginPanel(string title, out Rect area)
        {
            if (scrolledFor != Open) { scrolledFor = Open; panelScroll = Vector2.zero; }
            float H = Screen.height / Ui.S;
            float x = 10, y = 136, w = 470, h = H - y - 94;
            area = Claim(Ui.R(x, y, w, h));
            Ui.PanelBox(area);
            Ui.Title(Ui.R(x + 14, y + 8, w - 60, 28), title);
            if (Ui.Button(Ui.R(x + w - 38, y + 8, 30, 30), "X")) Open = Panel.None;
            var view = Ui.R(x + 2, y + 42, w - 4, h - 46);
            var content = new Rect(0, 0, view.width - 18 * Ui.S, Mathf.Max(view.height, panelHeight * Ui.S));
            panelScroll = GUI.BeginScrollView(view, panelScroll, content);
            return new Rows(12, 4, w - 46);
        }

        void EndPanel(Rows rows) { panelHeight = rows.Y + 20; GUI.EndScrollView(); }

        // ================================================================== laws and budget
        void TownPanel()
        {
            Rect area;
            var rows = BeginPanel("Laws & budget", out area);
            var p = T.Policy;
            var bk = T.LastBook;
            Ui.Big(rows.Next(32), Town.Money(T.Treasury));
            Ui.Small(rows.Next(20), T.Treasury < 0 ? Ui.Col("in debt, paying 5.5% a year", Ui.Bad) : "in the town's account");

            rows.Header("Last month: in " + Town.Money(bk.TotalIn) + ", out " + Town.Money(bk.TotalOut));
            rows.Pair("Income tax", Town.Money(bk.IncomeTax), Ui.Good);
            rows.Pair("Sales tax", Town.Money(bk.SalesTax), Ui.Good);
            rows.Pair("Property tax", Town.Money(bk.PropertyTax), Ui.Good);
            rows.Pair("Business tax", Town.Money(bk.BusinessTax), Ui.Good);
            rows.Pair("College tuition", Town.Money(bk.Fees), Ui.Good);
            rows.Pair("Public employees' pay", "-" + Town.Money(bk.PublicWages));
            rows.Pair("Upkeep of buildings and roads", "-" + Town.Money(bk.Upkeep));
            rows.Pair("Unemployment benefits", "-" + Town.Money(bk.Benefits));
            rows.Pair("Construction", "-" + Town.Money(bk.Construction));
            if (bk.Interest > 0) rows.Pair("Interest on debt", "-" + Town.Money(bk.Interest), Ui.Bad);

            rows.Header("Taxes");
            p.IncomeTax = Law(rows, "Income tax", p.IncomeTax, 0, 0.3f, 1, Town.Pct(p.IncomeTax), "Taken from every paycheck. The biggest source of money, but it leaves people less to spend in town.");
            p.SalesTax = Law(rows, "Sales tax", p.SalesTax, 0, 0.15f, 2, Town.Pct(p.SalesTax), "Added at the till in shops, restaurants and services. High sales tax sends shoppers out of town.");
            p.PropertyTax = Law(rows, "Property tax (a year)", p.PropertyTax, 0, 0.03f, 3, (p.PropertyTax * 100).ToString("0.0") + "%", "On the value of every building. Owners pay it; landlords pass it on in rent.");
            p.BusinessTax = Law(rows, "Business profit tax", p.BusinessTax, 0, 0.35f, 4, Town.Pct(p.BusinessTax), "On businesses' monthly profit. Too high and owners take fewer risks.");
            rows.Header("Work and welfare");
            p.MinWage = Mathf.Round(Law(rows, "Minimum wage (a month)", (float)p.MinWage, 0, 4000, 5, Town.Money(p.MinWage), "No job may pay less. Raises the lowest wages, but shops and services with thin margins hire fewer people and charge more.") / 50) * 50;
            p.Benefit = Mathf.Round(Law(rows, "Unemployment benefit", (float)p.Benefit, 0, 2500, 6, Town.Money(p.Benefit) + "/mo", "Paid by the town for up to six months to people who lost their job. Keeps families from leaving while they look.") / 50) * 50;
            p.TuitionAid = Law(rows, "College tuition paid by the town", p.TuitionAid, 0, 1, 7, Town.Pct(p.TuitionAid), "More students take degrees and trades when it's cheaper; the town collects less tuition.");
            rows.Header("Public services");
            p.PublicPay = Law(rows, "Public-sector pay", p.PublicPay, 0.7f, 1.5f, 8, Town.Pct(p.PublicPay) + " of normal", "Teachers, nurses, doctors, police, firefighters, clerks. Pay too little and the posts go unfilled.");
            p.SchoolStaffing = Law(rows, "Teachers funded", p.SchoolStaffing, 0.5f, 1.2f, 9, Town.Pct(p.SchoolStaffing), "Share of the teachers the schools need for their pupils. Fewer teachers, slower learning.");
            p.HealthStaffing = Law(rows, "Doctors and nurses funded", p.HealthStaffing, 0.4f, 1f, 10, Town.Pct(p.HealthStaffing), "Fewer staff, fewer patients seen.");
            p.SafetyStaffing = Law(rows, "Police and firefighters funded", p.SafetyStaffing, 0, 1f, 11, Town.Pct(p.SafetyStaffing), "People feel safer near a staffed police and fire station.");
            var ir = rows.Next(30);
            if (Ui.Button(ir, p.Immigration ? "Newcomers welcome" : "Town closed to newcomers", p.Immigration)) p.Immigration = !p.Immigration;
            Tip(ir, "When jobs go unfilled, people move here to take them if there's a home for them.");

            var advice = T.Advice();
            if (advice.Count > 0)
            {
                rows.Header("Things to look at");
                foreach (var a in advice) { var r = rows.Peek(10); float h = Ui.Wrap(r, "• " + a); rows.Space(h / Ui.S + 4); }
            }
            rows.Space(16);
            if (!confirmNew) { if (Ui.Button(rows.Next(30), "Start a new town")) confirmNew = true; }
            else
            {
                Ui.Small(rows.Next(20), Ui.Col("This town will be lost. Sure?", Ui.Warn));
                var r = rows.Next(30);
                if (Ui.Button(new Rect(r.x, r.y, r.width / 2 - 4, r.height), "Yes, new town")) { confirmNew = false; gc.NewTown(); }
                if (Ui.Button(new Rect(r.x + r.width / 2 + 4, r.y, r.width / 2 - 4, r.height), "Keep playing")) confirmNew = false;
            }
            EndPanel(rows);
        }

        float Law(Rows rows, string name, float v, float min, float max, int id, string shown, string tip)
        {
            var r = rows.Next(20, 0);
            Ui.Small(r, name); Ui.Right(r, shown);
            var sr = rows.Next(22, 6);
            Tip(new Rect(r.x, r.y, r.width, sr.yMax - r.y), tip);
            return Ui.Slider(sr, v, min, max, id);
        }

        // ================================================================== economy
        void EconomyPanel()
        {
            Rect area;
            var rows = BeginPanel("Economy", out area);
            int lf = T.LaborForce(), emp = T.Employed(), un = T.Unemployed();
            var open = T.Openings();
            int openN = open.Sum(o => o.Count);
            rows.Pair("Workforce", lf.ToString());
            rows.Pair("Working", emp.ToString());
            rows.Pair("Looking for work", un + " (" + Town.Pct(T.UnemploymentRate()) + ")", un > 0 && T.UnemploymentRate() > 0.08f ? Ui.Warn : Ui.Text);
            rows.Pair("Jobs open", openN.ToString());
            rows.Pair("Average pay", Town.Money(T.AverageWage()) + " a month");
            rows.Pair("Typical family savings", Town.Money(T.MedianCash()));
            rows.Pair("Prices against normal", Town.Pct(T.PriceLevel()), T.PriceLevel() > 1.25 ? Ui.Warn : Ui.Text);
            rows.Pair("Businesses open", T.Buildings.Count(b => Town.IsBusiness(b) && b.Open).ToString());

            var hist = T.History;
            if (hist.Count >= 2)
            {
                rows.Header("History (monthly)");
                Charts(rows, "People", hist.Select(h => (float)h.Population).ToList(), Ui.Accent, "Jobless %", hist.Select(h => h.Workers + h.Unemployed > 0 ? 100f * h.Unemployed / (h.Workers + h.Unemployed) : 0).ToList(), Ui.Bad);
                Charts(rows, "Average pay", hist.Select(h => (float)h.AvgWage).ToList(), Ui.Good, "Treasury", hist.Select(h => (float)h.Treasury).ToList(), Ui.Warn);
                Charts(rows, "Prices", hist.Select(h => (float)h.Prices * 100).ToList(), Ui.Warn, "Happiness", hist.Select(h => h.Happiness).ToList(), Ui.Good);
                Charts(rows, "Output (GDP)", hist.Select(h => (float)h.Gdp).ToList(), Ui.Accent, "Average rent", hist.Select(h => (float)h.AvgRent).ToList(), Ui.Dim);
            }

            rows.Header("Money across the town line");
            var tr = T.LastTrade;
            rows.Pair("Earned from outside", Town.Money(tr.In), Ui.Good);
            Ui.Small(rows.Next(18), "  exports " + Town.Money(tr.Exports) + " · office work " + Town.Money(tr.Services) + " · pensions " + Town.Money(tr.Pensions));
            rows.Pair("Spent outside", Town.Money(tr.Outflow), Ui.Bad);
            Ui.Small(rows.Next(18), "  imports " + Town.Money(tr.Imports) + " · profits " + Town.Money(tr.Profits) + " · bills " + Town.Money(tr.Utilities));

            rows.Header("Jobs");
            var byRole = new Dictionary<Role, int[]>();
            foreach (var c in T.People) if (c.WorkId >= 0) { if (!byRole.ContainsKey(c.Role)) byRole[c.Role] = new int[2]; byRole[c.Role][0]++; }
            foreach (var o in open) { if (!byRole.ContainsKey(o.Role)) byRole[o.Role] = new int[2]; byRole[o.Role][1] += o.Count; }
            foreach (var kv in byRole.OrderByDescending(k => k.Value[0] + k.Value[1]))
            {
                var rd = Defs.Get(kv.Key);
                var r = rows.Next(20, 1);
                Ui.Small(new Rect(r.x, r.y, r.width * 0.5f, r.height), rd.Name, Ui.Text);
                Ui.Small(new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.3f, r.height), kv.Value[0] + (kv.Value[1] > 0 ? Ui.Col(" +" + kv.Value[1] + " open", Ui.Warn) : ""));
                Ui.Right(r, Town.Money(rd.Wage), Ui.Dim);
            }

            rows.Header("Businesses");
            foreach (var b in T.Buildings.Where(Town.IsBusiness).Where(b => b.Built).OrderBy(b => b.Closed).ThenByDescending(b => b.LastRevenue))
            {
                var r = rows.Next(22, 1);
                if (Ui.Link(new Rect(r.x, r.y, r.width * 0.62f, r.height), T.Title(b), b.Closed ? Ui.Dim : Ui.Text)) gc.FocusBuilding(b);
                if (b.Closed) Ui.Right(r, "closed", Ui.Dim);
                else Ui.Right(r, b.Employees.Count + " staff  " + Town.Money(b.LastProfit), b.LastProfit >= 0 ? Ui.Good : Ui.Bad);
            }
            EndPanel(rows);
        }

        void Charts(Rows rows, string a, List<float> va, Color ca, string b, List<float> vb, Color cb)
        {
            var r = rows.Next(18, 0);
            float half = r.width / 2;
            Ui.Small(new Rect(r.x, r.y, half - 6, r.height), a + "  " + Ui.Col(Last(va), Ui.Text));
            Ui.Small(new Rect(r.x + half + 6, r.y, half - 6, r.height), b + "  " + Ui.Col(Last(vb), Ui.Text));
            var c = rows.Next(54, 8);
            Ui.Chart(new Rect(c.x, c.y, half - 6, c.height), Tail(va), ca);
            Ui.Chart(new Rect(c.x + half + 6, c.y, half - 6, c.height), Tail(vb), cb);
        }

        static List<float> Tail(List<float> v) { return v.Count > 60 ? v.GetRange(v.Count - 60, 60) : v; }
        static string Last(List<float> v)
        {
            if (v.Count == 0) return "";
            float x = v[v.Count - 1];
            return Mathf.Abs(x) >= 1000 ? Town.Money(x) : x.ToString("0.#");
        }

        // ================================================================== people
        void PeoplePanel()
        {
            Rect area;
            var rows = BeginPanel("People", out area);
            rows.Pair("Living here", T.Population.ToString());
            rows.Pair("Households", T.Households.Count.ToString());
            rows.Pair("Last month", T.LastBirths + " born · " + T.LastDeaths + " died · " + T.LastArrivals + " moved in · " + T.LastLeft + " left");
            rows.Header("Ages");
            int[] edges = { 0, 6, 12, 18, 30, 50, 67, 200 };
            string[] names = { "Under 6", "6–11", "12–17", "18–29", "30–49", "50–66", "67 and over" };
            int maxN = 1; var counts = new int[names.Length];
            foreach (var c in T.People) for (int i = 0; i < names.Length; i++) if (c.Age >= edges[i] && c.Age < edges[i + 1]) { counts[i]++; break; }
            foreach (var n in counts) maxN = Mathf.Max(maxN, n);
            for (int i = 0; i < names.Length; i++) BarRow(rows, names[i], counts[i], maxN, Ui.Accent);
            rows.Header("Education (adults)");
            var edu = new int[5]; int adults = 0;
            foreach (var c in T.People) if (!c.IsChild) { edu[(int)c.Edu]++; adults++; }
            for (int i = 0; i < 5; i++) BarRow(rows, Defs.EduName((Edu)i), edu[i], Mathf.Max(1, adults), i >= 3 ? Ui.Good : i == 0 ? Ui.Warn : Ui.Accent);
            int students = T.People.Count(c => c.Stage >= Stage.TradeSchool);
            rows.Pair("In college or trade school", students.ToString());
            int noPlace = T.People.Count(c => c.Stage != Stage.None && c.SchoolId < 0);
            if (noPlace > 0) rows.Pair("Children without a school place", noPlace.ToString(), Ui.Bad);
            rows.Header("Housing");
            int owners = T.Households.Count(h => h.Owns);
            rows.Pair("Own their home", owners + " of " + T.Households.Count);
            rows.Pair("Empty homes", T.VacantUnits() + " of " + T.TotalUnits());
            rows.Pair("Average rent", Town.Money(T.AverageRent()) + "/mo");
            rows.Pair("House prices", Town.Pct(T.HousingIndex) + " of normal");
            int forSale = T.Buildings.Count(b => b.ForSale && b.Households.Count == 0);
            if (forSale > 0) rows.Pair("Houses for sale", forSale.ToString());
            var looking = T.People.Where(c => c.InLaborForce && c.WorkId < 0).OrderByDescending(c => c.JoblessDays).ToList();
            if (looking.Count > 0)
            {
                rows.Header("Looking for work (" + looking.Count + ")");
                foreach (var c in looking.Take(60))
                {
                    var r = rows.Next(22, 1);
                    if (Ui.Link(new Rect(r.x, r.y, r.width * 0.5f, r.height), c.Name)) gc.FocusCitizen(c);
                    Ui.Small(new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.5f, r.height), Defs.EduName(c.Edu));
                    Ui.Right(r, c.JoblessDays >= 4 ? c.JoblessDays / 4 + " mo" : "new", Ui.Dim);
                }
            }
            EndPanel(rows);
        }

        void BarRow(Rows rows, string name, int n, int max, Color c)
        {
            var r = rows.Next(20, 2);
            Ui.Small(new Rect(r.x, r.y, r.width * 0.36f, r.height), name);
            Ui.Bar(new Rect(r.x + r.width * 0.36f, r.y + 5 * Ui.S, r.width * 0.5f, r.height - 10 * Ui.S), n / (float)max, c);
            Ui.Right(r, n.ToString(), Ui.Dim);
        }

        // ================================================================== news
        void NewsPanel()
        {
            Rect area;
            var rows = BeginPanel("News", out area);
            for (int i = T.Log.Count - 1; i >= 0 && i >= T.Log.Count - 150; i--)
            {
                var e = T.Log[i];
                Color c = e.Kind == LogKind.Bad ? Ui.Bad : e.Kind == LogKind.Good ? Ui.Good : e.Kind == LogKind.Birth ? new Color(0.6f, 0.8f, 1f) : e.Kind == LogKind.Death ? Ui.Dim : e.Kind == LogKind.Money ? Ui.Warn : Ui.Text;
                Ui.Small(rows.Next(16, 0), Town.DateOf(e.Day));
                var r = rows.Peek(10);
                float h = Ui.WrapHeight(e.Text, r.width);
                var tr = new Rect(r.x, r.y, r.width, h);
                bool over = tr.Contains(Event.current.mousePosition) && e.Subject >= 0;
                Ui.Wrap(tr, Ui.Col(e.Text, over ? Ui.Accent : c));
                if (over && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (e.AboutPerson) { var p = T.GetCitizen(e.Subject); if (p != null) gc.FocusCitizen(p); }
                    else { var b = T.GetBuilding(e.Subject); if (b != null) gc.FocusBuilding(b); }
                    Event.current.Use();
                }
                rows.Space(h / Ui.S + 8);
            }
            EndPanel(rows);
        }
    }
}
