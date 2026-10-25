using System;
using System.Linq;
using RealLife.Simulation;

static class Program
{
    static int Main(string[] args)
    {
        int years = args.Length > 0 ? int.Parse(args[0]) : 5;
        int seed = args.Length > 1 ? int.Parse(args[1]) : 12345;
        bool verbose = args.Length > 2 && args[2] == "v";
        string watch = args.Length > 3 ? args[3] : null;
        var t = Town.CreateNew(seed);
        Console.WriteLine($"{t.Name}: pop {t.Population}, households {t.Households.Count}, buildings {t.Buildings.Count}, jobs {t.JobSlots()}, employed {t.Employed()}, unemployed {t.Unemployed()}");
        if (args.Length > 4 && args[4] == "schools")
        {
            foreach (var bt in new[] { BuildingType.HighSchool, BuildingType.College, BuildingType.Park, BuildingType.BusDepot })
            {
                bool done = false;
                for (int y = 30; y < 70 && !done; y++) for (int x = 30; x < 75 && !done; x++) if (t.CanPlace(bt, x, y) == null) { t.PlacePublic(bt, x, y); done = true; }
                Console.WriteLine("placed " + bt + ": " + done);
            }
            for (int y = 64; y < 75; y++) for (int x = 38; x < 56; x++) t.SetZone(x, y, Zone.Residential);
            for (int x = 38; x <= 56; x++) t.PlaceRoad(x, 70);
        }
        Header();
        Row(t);
        int months = years * 12;
        for (int m = 0; m < months; m++)
        {
            for (int d = 0; d < Balance.DaysPerMonth; d++) t.Advance(1f);
            Row(t);
            if (verbose) foreach (var e in t.Log.Where(e => e.Day > t.Day - 4)) Console.WriteLine("      " + e.Text);
            if (watch != null) foreach (var b in t.Buildings.Where(b => b.Type.ToString() == watch)) Console.WriteLine($"      [{b.Type} {b.Id}] {(b.Closed ? "CLOSED" : "")} staff {b.Employees.Count} st {b.Staffing:0.00} cash {Town.Money(b.Cash)} rev {Town.Money(b.LastRevenue)} wages {Town.Money(b.LastWages)} supplies {Town.Money(b.LastSupplies)} other {Town.Money(b.LastOther)} tax {Town.Money(b.LastTax)} profit {Town.Money(b.LastProfit)} use {b.LastUse:0.00} lost {b.LastLost} cust {b.LastCustomers} price {b.Price:0.00}");
        }
        Console.WriteLine();
        Console.WriteLine("Businesses:");
        foreach (var b in t.Buildings.Where(Town.IsBusiness).OrderBy(b => b.Type))
            Console.WriteLine($"  {b.Def.Name,-24} {(!b.Built ? "building" : b.Closed ? "CLOSED" : ""),-8} staff {b.Employees.Count,2}/{b.Def.TotalJobs,-2} st {b.Staffing:0.00} wage x{b.WageMult:0.00} price {b.Price,8:0.00} cash {Town.Money(b.Cash),8} rev {Town.Money(b.LastRevenue),8} profit {Town.Money(b.LastProfit),8} use {b.LastUse:0.00} lost {b.LastLost} owner {b.Owner} {b.OwnerName}");
        Console.WriteLine("Trade last month: in " + Town.Money(t.LastTrade.In) + " (exports " + Town.Money(t.LastTrade.Exports) + ", services " + Town.Money(t.LastTrade.Services) + ", pensions " + Town.Money(t.LastTrade.Pensions) + ", invest " + Town.Money(t.LastTrade.Investment) +
            ")  out " + Town.Money(t.LastTrade.Outflow) + " (imports " + Town.Money(t.LastTrade.Imports) + ", utilities " + Town.Money(t.LastTrade.Utilities) + ", profits " + Town.Money(t.LastTrade.Profits) + ", freight " + Town.Money(t.LastTrade.Freight) + ")");
        var b2 = t.LastBook;
        Console.WriteLine($"Town book: in {Town.Money(b2.TotalIn)} (inc {Town.Money(b2.IncomeTax)} sales {Town.Money(b2.SalesTax)} prop {Town.Money(b2.PropertyTax)} biz {Town.Money(b2.BusinessTax)} fees {Town.Money(b2.Fees)}) out {Town.Money(b2.TotalOut)} (wages {Town.Money(b2.PublicWages)} upkeep {Town.Money(b2.Upkeep)} benefits {Town.Money(b2.Benefits)} build {Town.Money(b2.Construction)} interest {Town.Money(b2.Interest)})");
        Console.WriteLine("Edu: " + string.Join(", ", Enum.GetValues(typeof(Edu)).Cast<Edu>().Select(e => e + " " + t.People.Count(c => !c.IsChild && c.Edu == e))));
        Console.WriteLine("Advice: " + string.Join(" | ", t.Advice()));
        Console.WriteLine("Wants: " + string.Join(", ", Defs.All.Where(x => t.LastWants[(int)x] > 0).Select(x => x + " " + t.LastWants[(int)x].ToString("0"))));
        int nans = t.Households.Count(h => double.IsNaN(h.Cash) || double.IsInfinity(h.Cash) || double.IsNaN(h.FunMoney)) + t.Buildings.Count(b => double.IsNaN(b.Cash) || double.IsNaN(b.Price) || double.IsNaN(b.Value) || b.Stock.Any(x => float.IsNaN(x))) +
                   t.People.Count(c => float.IsNaN(c.Age) || float.IsNaN(c.Happiness) || float.IsNaN(c.Health) || double.IsNaN(c.Wage)) + t.Loans.Count(l => double.IsNaN(l.Principal)) + (double.IsNaN(t.Treasury) ? 1 : 0);
        Console.WriteLine(nans == 0 ? "No NaNs (save-safe)." : "NaN values: " + nans);
        Console.WriteLine("Loans: " + t.Loans.Count + " owed " + Town.Money(t.Loans.Sum(l => l.Principal)) + " bank capital " + Town.Money(t.BankCapital) + " funding " + Town.Money(t.BankFunding));
        return 0;
    }

    static void Header() => Console.WriteLine("date          pop  hh  emp unemp  un%  jobs open vac  hap  treasury   avgwage  medcash  prices  rent   houseIdx biz  trade-in trade-out  b/d/a/l");
    static void Row(Town t)
    {
        int open = t.Openings().Sum(o => o.Count);
        Console.WriteLine($"{t.MonthName} {t.Year} w{t.Week} {t.Population,5} {t.Households.Count,3} {t.Employed(),4} {t.Unemployed(),4} {t.UnemploymentRate()*100,5:0.0} {t.JobSlots(),4} {open,4} {t.VacantUnits(),3} {t.AverageHappiness(),4:0} {Town.Money(t.Treasury),9} {Town.Money(t.AverageWage()),8} {Town.Money(t.MedianCash()),8} {t.PriceLevel(),6:0.00} {Town.Money(t.AverageRent()),6} {t.HousingIndex,6:0.00} {t.Buildings.Count(b => Town.IsBusiness(b) && b.Open),4} {Town.Money(t.LastTrade.In),9} {Town.Money(t.LastTrade.Outflow),9}  {t.LastBirths}/{t.LastDeaths}/{t.LastArrivals}/{t.LastLeft}");
    }
}
