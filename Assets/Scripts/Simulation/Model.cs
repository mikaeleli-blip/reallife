using System;
using System.Collections.Generic;

namespace RealLife.Simulation
{
    [Serializable]
    public class Citizen
    {
        public int Id;
        public string First, Last;
        public bool Female;
        public float Age;
        public int HouseholdId = -1;
        public int PartnerId = -1, MotherId = -1, FatherId = -1;
        // Education
        public Edu Edu;
        public Stage Stage;
        public int SchoolId = -1;
        public float StageYears;          // years done in the current stage
        public float Elementary;          // 0..1: how much of elementary school they got
        public bool Dropout;
        // Work
        public int WorkId = -1;
        public Role Role;
        public double Wage;               // monthly
        public float Experience;          // years in work
        public int DaysWorked;            // this month
        public int JoblessDays;
        public bool Retired;
        public bool Owner;                // runs their own business
        // Life
        public float Health = 90f;
        public float Happiness = 60f;
        public float Ambition = 0.5f;     // 0..1: college, starting a business, chasing better pay
        public float Thrift = 0.5f;       // 0..1: how much of their pay they put by
        public float Seed;                // looks
        public int Pregnant;              // days to go
        public int Sick;                  // days ill
        public int ArrivedDay;
        public int BornDay = -1;
        // Today (for the 3D view and the inspector)
        public int ErrandId = -1;         // a shop visited today
        public int LeisureId = -1;        // a restaurant, bar, café, gym or park visited today
        public bool AtWork;
        public int Outings, LastOutings;  // times they went out this month and last
        public string Note;               // the latest thing that happened to them

        public string Name { get { return First + " " + Last; } }
        public bool IsChild { get { return Age < Balance.AdultAge; } }
        public bool IsStudent { get { return Stage != Stage.None; } }
        public bool InLaborForce { get { return !IsChild && !Retired && !IsStudent && !(Pregnant > 0 && Pregnant < 6); } }
    }

    [Serializable]
    public class Household
    {
        public int Id;
        public string Name;
        public List<int> Members = new List<int>();
        public int HomeId = -1;
        public bool Owns;                 // owns the home (otherwise rents)
        public double Cash;
        public float Pantry;              // groceries in the house, person-weeks
        public float Clothes = 0.8f;      // 0..1: how worn the family's clothes are (1 new)
        public float Electronics = 0.7f, Furniture = 0.7f;
        public int Cars;
        public float CarWear = 0.8f;      // 0..1 condition of the car
        public float Fuel;                // tanks
        public bool CarBroken;
        public int MissedRent;
        public double FunMoney;           // left this month for going out and things they want rather than need
        public double FunDaily;           // what they like to spend on those in a day
        public int SadMonths;
        public int MovedIn;
        // This month and last: where the money went
        public double[] In = new double[HouseholdBook.Count], Out = new double[HouseholdBook.Count];
        public double[] LastIn = new double[HouseholdBook.Count], LastOut = new double[HouseholdBook.Count];
    }

    /// <summary>Lines in a household's monthly budget.</summary>
    public static class HouseholdBook
    {
        public const int Wages = 0, Benefits = 1, Pension = 2, Business = 3, Loans = 4;          // income lines
        public const int Housing = 0, Food = 1, Transport = 2, Goods = 3, Leisure = 4, Taxes = 5, Debt = 6, Utilities = 7, School = 8, Savings = 9;   // spending lines
        public const int Count = 10;
        public static readonly string[] InNames = { "Wages", "Benefits", "Pension", "Business & rent", "Loans", "", "", "", "", "" };
        public static readonly string[] OutNames = { "Housing", "Groceries", "Car & fuel", "Shopping", "Going out", "Income tax", "Loan payments", "Utilities", "Tuition", "Retirement saving" };
    }

    [Serializable]
    public class Building
    {
        public int Id;
        public BuildingType Type;
        public int X, Y;
        public int Facing;                // 0 north (+y), 1 east, 2 south, 3 west: the side the road is on
        public int BuiltDay;
        // Construction
        public bool Built = true;
        public float Progress;            // labour-days done
        public int ContractorId = -1;     // the construction company building it (-1: outside contractors)
        public double ContractLeft;       // what the client still owes the builder
        public bool Started;              // materials delivered
        // Ownership and money
        public Owner Owner;
        public int OwnerId = -1;          // the household that owns it, or (Owner.Firm) the company's building, -1 the bank
        public string OwnerName;          // the owner's name: a family, a company or the bank
        public string Brand;              // a business's trading name, if it has one ("Horizon Properties")
        public double EquityLeft;         // while it's going up: what the founders still mean to put in from their savings
        public int WantManagers, WantHands;   // a property firm's staff for the homes it has
        public double Cash;               // the business's account
        public double Value;
        public bool Closed;               // a business that has shut
        public bool ForSale;              // a house on the market
        public int ClosedDays;
        // Trade
        public float[] Stock = new float[Defs.GoodCount];
        public double Price;              // what it asks (per unit sold, or per customer served)
        public float WageMult = 1f;       // pay relative to the going rate
        public float Staffing = 1f;       // 0..1: share of its job slots it wants filled
        public List<int> Employees = new List<int>();
        public int Unfilled;              // days it had an opening nobody took
        public int ServedToday, TurnedAway, SoldToday;
        public float Produced;            // made today
        public float Sold;                // units sold over the counter today
        // This month
        public double Revenue, WageBill, Supplies, TaxPaid, OtherCosts;
        public int Customers, Lost;
        // Last month
        public double LastRevenue, LastWages, LastSupplies, LastTax, LastOther, LastProfit;
        public int LastCustomers, LastLost;
        public int LossMonths;
        public float CapUsed, CapTotal;   // this month: customers served or units made, against what the staff could have done
        public float LastUse;             // last month's share of capacity used
        public double UnitCost;           // what it last paid for a unit of what it sells or works
        // Homes
        public List<int> Households = new List<int>();
        public double Rent;               // per unit per month
        public int VacantDays;

        public BuildingDef Def { get { return Defs.Get(Type); } }
        public bool Contains(int tx, int ty) { var d = Def; return tx >= X && tx < X + d.W && ty >= Y && ty < Y + d.H; }
        public float BuildFraction { get { var d = Def; return d.Work <= 0 ? 1f : Math.Min(1f, Progress / d.Work); } }
        public bool Open { get { return Built && !Closed; } }
        public float CenterX { get { return X + Def.W * 0.5f; } }
        public float CenterY { get { return Y + Def.H * 0.5f; } }
    }

    [Serializable]
    public class Loan
    {
        public int Id;
        public LoanKind Kind;
        public int HouseholdId = -1;
        public int BuildingId = -1;       // a business loan, or the home a mortgage is on
        public int FirmId = -1;           // a company that pays it (a property firm's loan on one of its buildings)
        public int CitizenId = -1;        // the student a student loan is for
        public double Principal;          // still owed
        public double Rate;               // a year
        public double Payment;            // a month
        public int Missed;
        public int Started;
        public bool Deferred;             // a student loan while they're still studying
    }

    /// <summary>The laws: what the player sets.</summary>
    [Serializable]
    public class Policy
    {
        public float IncomeTax = 0.12f;
        public float SalesTax = 0.07f;
        public float PropertyTax = 0.014f;      // of value, a year
        public float BusinessTax = 0.12f;       // of monthly profit
        public double MinWage = 2100;           // monthly
        public double Benefit = 1200;           // monthly, for up to six months out of work
        public float TuitionAid = 0.25f;        // the share of tuition the town pays
        public float PublicPay = 1f;            // public-sector pay against the going rate
        public float SchoolStaffing = 1f;       // share of teacher posts funded
        public float HealthStaffing = 1f;
        public float SafetyStaffing = 1f;
        public bool Immigration = true;
    }

    /// <summary>The town's books, by month.</summary>
    [Serializable]
    public class TownBook
    {
        public double IncomeTax, SalesTax, PropertyTax, BusinessTax, Fees;
        public double PublicWages, Upkeep, Benefits, Construction, Interest;
        public double TotalIn { get { return IncomeTax + SalesTax + PropertyTax + BusinessTax + Fees; } }
        public double TotalOut { get { return PublicWages + Upkeep + Benefits + Construction + Interest; } }
        public void Clear() { IncomeTax = SalesTax = PropertyTax = BusinessTax = Fees = PublicWages = Upkeep = Benefits = Construction = Interest = 0; }
        public TownBook Copy() { return (TownBook)MemberwiseClone(); }
    }

    /// <summary>Money crossing the town line this month: what the town earns from outside and what it spends there.</summary>
    [Serializable]
    public class TradeBook
    {
        public double Exports, Services, Pensions, Investment, Imports, Utilities, Profits, Freight, Savings;
        public double In { get { return Exports + Services + Pensions + Investment; } }
        public double Outflow { get { return Imports + Utilities + Profits + Freight + Savings; } }
        public void Clear() { Exports = Services = Pensions = Investment = Imports = Utilities = Profits = Freight = Savings = 0; }
        public TradeBook Copy() { return (TradeBook)MemberwiseClone(); }
    }

    [Serializable]
    public class LogEntry
    {
        public int Day;
        public string Text;
        public LogKind Kind;
        public int Subject = -1;          // a building or citizen id to jump to
        public bool AboutPerson;
    }

    [Serializable]
    public class HistoryPoint
    {
        public int Day;
        public int Population, Workers, Jobs, Unemployed, Businesses, Homes, VacantHomes, Students;
        public double Treasury, AvgWage, MedianCash, Gdp, Prices, AvgRent;
        public float Happiness;
    }
}
