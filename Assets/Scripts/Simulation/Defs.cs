using System;
using System.Collections.Generic;

namespace RealLife.Simulation
{
    /// <summary>Things that are made, moved and sold. One sim day is about a real week, so a unit of groceries is a week's
    /// food for one person and a month is four days.</summary>
    public enum Good { Crops, Groceries, Clothes, Electronics, Furniture, Fuel, Parts, Materials, Cars }

    /// <summary>Things people pay for that can't be stored: a meal out, a haircut, a repair.</summary>
    public enum Service { None, Meals, Coffee, Drinks, Haircut, Fitness, CarRepair }

    public enum Zone : byte { None, Residential, Commercial, Industrial, Farmland, Apartments }   // Residential: houses

    public enum Category { Residential, Commercial, Industrial, Public, Special }

    /// <summary>Credentials, lowest to highest. A trade certificate and a degree are different tracks: a degree doesn't make you an electrician.</summary>
    public enum Edu { None, HighSchool, Trade, Degree, Medical }

    /// <summary>Where someone is in school right now.</summary>
    public enum Stage { None, Elementary, HighSchool, TradeSchool, College, MedSchool }

    /// <summary>Who owns a building. Firm: a local company (OwnerId is the company's own building; -1 is the bank).
    /// Outside is only the state highway now (and old saves, converted on load).</summary>
    public enum Owner { Town, Household, Outside, Firm }

    public enum LoanKind { Mortgage, Car, Student, Business }

    public enum LogKind { Info, Good, Bad, Birth, Death, Money }

    public enum BuildingType
    {
        // Residential
        House, Apartments,
        // Commercial
        GroceryStore, Restaurant, Cafe, Bar, ClothingStore, ElectronicsStore, FurnitureStore, GasStation, CarDealer,
        AutoRepair, HairSalon, Gym, Office, Bank,
        // Industrial
        Farm, FoodPlant, GarmentFactory, ElectronicsPlant, FurnitureWorkshop, Construction, Trucking,
        // Public
        TownHall, Elementary, HighSchool, College, Clinic, Hospital, Police, FireStation, Park, BusDepot,
        // Special
        FreightTerminal,
        // Commercial (added later; kept at the end so saved games keep their numbers)
        Realty,
        // Public (added later)
        ParkingLot,
        // Commercial: the big grocery chains (added later)
        DiscountStore, Supermarket, WarehouseClub
    }

    public enum Role
    {
        None,
        Farmer, PlantWorker, Assembler, Technician, Engineer, WarehouseWorker, TruckDriver,
        ConstructionWorker, Electrician, Plumber, SiteManager,
        Cashier, StockClerk, StoreManager, Cook, Chef, Waiter, Barista, Bartender, SalesClerk, Attendant, CarSales,
        Mechanic, Hairdresser, Trainer, OfficeClerk, Accountant, Developer, Teller, LoanOfficer, BranchManager,
        Teacher, Professor, Janitor, Nurse, Doctor, Receptionist, PoliceOfficer, Firefighter, BusDriver, TownClerk, ParkKeeper,
        PropertyManager, Handyman, Commuter
    }

    public sealed class RoleDef
    {
        public Role Role;
        public string Name;
        public Edu Needs;
        public double Wage;        // monthly, before tax
        public double Billing;     // an office worker's billing to outside clients per day worked
    }

    public struct JobSlot
    {
        public Role Role; public int Count;
        public JobSlot(Role r, int n) { Role = r; Count = n; }
    }

    public sealed class BuildingDef
    {
        public BuildingType Type;
        public string Name, Blurb;
        public int W = 1, H = 1;
        public Category Cat;
        public Zone Zone;                 // which zone a developer builds it on (None: placed by the player)
        public double Materials;          // building materials bought for construction, $
        public float Work;                // construction labour-days
        public JobSlot[] Jobs = new JobSlot[0];
        public int Homes;                 // household units
        public double Rent;               // usual monthly rent per unit
        public double Value;              // usual property value when new
        // Trade
        public Good? Sells;               // sold to households over the counter
        public Good? Makes;               // produced
        public Good? Input; public float InputPer;   // what each unit made (or each service) uses up
        public float Output;              // units made, or customers served, per worker-day
        public Service Serves;
        public double ServicePrice;
        public float StockCap;            // how much it keeps of what it sells or makes
        // Public
        public int Seats;                 // school places, or patients a day
        public int Radius;                // tiles it serves
        public double Upkeep;             // monthly running costs besides wages
        public int MinPop;                // population before anyone opens one
        public bool PlayerBuilds;
        public int Floors = 1;            // for the 3D view
        // A grocery chain's stores: its name, its prices against the going rate, what it pays its suppliers for buying in bulk,
        // how much better (or worse) a place to shop it is for those who can afford to care, the people it takes to fill
        // another store, and whether you need a car to shop there (a warehouse club)
        public string Chain;
        public float PriceFactor = 1, BulkBuy = 1, Appeal;
        public int PopPerStore;
        public bool NeedsCar;
        public int TotalJobs { get { int n = 0; foreach (var j in Jobs) n += j.Count; return n; } }
        public double StartupCost { get { return Materials + Work * Balance.LaborRate; } }
    }

    /// <summary>Tuning numbers. Money is in dollars at realistic monthly rates; one day ~ one real week.</summary>
    public static class Balance
    {
        public const int DaysPerMonth = 4;
        public const int MonthsPerYear = 12;
        public const int DaysPerYear = DaysPerMonth * MonthsPerYear;
        public const float YearPerDay = 1f / DaysPerYear;

        public const float AdultAge = 18f, RetireAge = 67f, SchoolAge = 6f, HighSchoolAge = 12f;
        public const float ChildFood = 0.7f;                // a child eats this much of an adult's groceries

        public const double LaborRate = 1300;               // what a construction company bills per labour-day (a worker-week)
        public const double FreightLocal = 0.04, FreightOutside = 0.06;   // share of a shipment's value paid for trucking
        public const double TruckCapacity = 60000;          // shipment value a driver can move in a day

        public const double Pension = 1700;                 // monthly state pension from outside
        public const double UtilitiesHome = 190;            // monthly power, water and internet per household
        public const double Insurance = 380, InsuranceAdult = 140;   // monthly health, car and home insurance, phones: paid out of town
        public const double UtilitiesJob = 60;              // monthly utilities per job slot for a business
        public const double OfficeOverhead = 0.25;
        public const double Maintenance = 0.01;             // a year, of a building's value: repairs done by the local builder
        public const double BankFee = 9;                    // monthly account fee per household          // share of an office's billing spent on software, travel, leases
        public const double CarPrice = 29000;

        public const double MortgageRate = 0.065, CarRate = 0.079, StudentRate = 0.05, BusinessRate = 0.09, FundingRate = 0.042;
        public const int MortgageMonths = 360, CarMonths = 60, StudentMonths = 120, BusinessMonths = 84;
        public const double PropertyRate = 0.068;           // a loan secured on a building a business or a property firm puts up
        public const int PropertyMonths = 300;
        public const double Equity = 0.12;                  // the share of a new business's cost its founders put in themselves
        public const double HandymanUpkeep = 3600;          // a month of upkeep labour one handyman covers
        public const int UnitsPerManager = 36;

        public const double TuitionTrade = 650, TuitionDegree = 950, TuitionMedical = 1600;   // monthly
        public const float YearsTrade = 2, YearsDegree = 4, YearsMedical = 4, YearsElementary = 6, YearsHighSchool = 6;
        public const int KidsPerTeacher = 24, StudentsPerProfessor = 22;
        public const int PatientsPerDoctor = 18, PatientsPerNurse = 9;

        public const float PriceStep = 0.04f, PriceLow = 0.7f, PriceHigh = 1.8f;
        public const float WageStep = 0.04f, WageHigh = 1.6f;

        public const float WalkTiles = 28;                  // furthest someone without a car or a bus will walk to work
        public const int BusRadius = 30;
    }

    public static class Defs
    {
        static readonly Dictionary<BuildingType, BuildingDef> defs = new Dictionary<BuildingType, BuildingDef>();
        static readonly Dictionary<Role, RoleDef> roles = new Dictionary<Role, RoleDef>();
        public static readonly BuildingType[] All = (BuildingType[])Enum.GetValues(typeof(BuildingType));
        public static readonly Role[] AllRoles = (Role[])Enum.GetValues(typeof(Role));
        public static readonly int GoodCount = Enum.GetValues(typeof(Good)).Length;

        public static BuildingDef Get(BuildingType t) { return defs[t]; }
        public static RoleDef Get(Role r) { return roles[r]; }

        // Prices: what households pay in a shop (Retail), what a maker gets selling to a shop in town (Wholesale),
        // what the outside world pays for exports (World) and charges for imports (Import).
        public static readonly double[] Retail    = { 0,  78,  68, 880, 690, 58,   0,    0, 29000 };
        public static readonly double[] Wholesale = { 23, 56,  40, 640, 450, 46,  40, 1000, 24900 };
        public static readonly double[] World     = { 19, 50,  35, 600, 420, 42,  40, 1000, 25000 };
        public static readonly double[] Import    = { 27, 64,  46, 700, 500, 46,  42, 1050, 25500 };
        public static readonly string[] GoodNames = { "Crops", "Groceries", "Clothes", "Electronics", "Furniture", "Fuel", "Parts", "Materials", "Cars" };
        public static readonly string[] GoodUnits = { "t", "wk", "pcs", "pcs", "pcs", "tank", "kits", "lots", "cars" };
        public static string Name(Good g) { return GoodNames[(int)g]; }

        public static string ServiceName(Service s)
        {
            switch (s)
            {
                case Service.Meals: return "Meals";
                case Service.Coffee: return "Coffee";
                case Service.Drinks: return "Drinks";
                case Service.Haircut: return "Haircuts";
                case Service.Fitness: return "Workouts";
                case Service.CarRepair: return "Repairs";
                default: return "";
            }
        }

        public static string EduName(Edu e)
        {
            switch (e)
            {
                case Edu.None: return "No diploma";
                case Edu.HighSchool: return "High school";
                case Edu.Trade: return "Trade certificate";
                case Edu.Degree: return "College degree";
                default: return "Medical degree";
            }
        }

        public static string StageName(Stage s)
        {
            switch (s)
            {
                case Stage.Elementary: return "Elementary school";
                case Stage.HighSchool: return "High school";
                case Stage.TradeSchool: return "Trade school";
                case Stage.College: return "College";
                case Stage.MedSchool: return "Medical school";
                default: return "";
            }
        }

        /// <summary>Can someone with this education do a job that needs that one?</summary>
        public static bool Qualifies(Edu has, Edu needs)
        {
            switch (needs)
            {
                case Edu.None: return true;
                case Edu.HighSchool: return has >= Edu.HighSchool;
                case Edu.Trade: return has == Edu.Trade;
                case Edu.Degree: return has == Edu.Degree || has == Edu.Medical;
                default: return has == Edu.Medical;
            }
        }

        static void R(Role r, string name, Edu e, double wage, double billing = 0)
        {
            roles[r] = new RoleDef { Role = r, Name = name, Needs = e, Wage = wage, Billing = billing };
        }

        static BuildingDef D(BuildingType t, string name, int w, int h, Category c, Zone z, double materials, float work, string blurb)
        {
            var d = new BuildingDef { Type = t, Name = name, W = w, H = h, Cat = c, Zone = z, Materials = materials, Work = work, Blurb = blurb };
            defs[t] = d; return d;
        }

        static JobSlot[] J(params object[] a)
        {
            var l = new JobSlot[a.Length / 2];
            for (int i = 0; i < l.Length; i++) l[i] = new JobSlot((Role)a[i * 2], (int)a[i * 2 + 1]);
            return l;
        }

        static Defs()
        {
            R(Role.None, "", Edu.None, 0);
            R(Role.Farmer, "Farmer", Edu.None, 2500);
            R(Role.PlantWorker, "Plant worker", Edu.None, 2700);
            R(Role.Assembler, "Assembler", Edu.HighSchool, 3000);
            R(Role.Technician, "Technician", Edu.Trade, 4300);
            R(Role.Engineer, "Engineer", Edu.Degree, 6600);
            R(Role.WarehouseWorker, "Warehouse worker", Edu.None, 2700);
            R(Role.TruckDriver, "Truck driver", Edu.HighSchool, 3600);
            R(Role.ConstructionWorker, "Construction worker", Edu.None, 3100);
            R(Role.Electrician, "Electrician", Edu.Trade, 4700);
            R(Role.Plumber, "Plumber", Edu.Trade, 4500);
            R(Role.SiteManager, "Site manager", Edu.Degree, 6200);
            R(Role.Cashier, "Cashier", Edu.None, 2350);
            R(Role.StockClerk, "Stock clerk", Edu.None, 2300);
            R(Role.StoreManager, "Store manager", Edu.HighSchool, 4200);
            R(Role.Cook, "Cook", Edu.None, 2600);
            R(Role.Chef, "Chef", Edu.Trade, 4000);
            R(Role.Waiter, "Waiter", Edu.None, 2300);
            R(Role.Barista, "Barista", Edu.None, 2250);
            R(Role.Bartender, "Bartender", Edu.None, 2400);
            R(Role.SalesClerk, "Sales clerk", Edu.HighSchool, 2600);
            R(Role.Attendant, "Attendant", Edu.None, 2250);
            R(Role.CarSales, "Car salesperson", Edu.HighSchool, 3800);
            R(Role.Mechanic, "Mechanic", Edu.Trade, 4000);
            R(Role.Hairdresser, "Hairdresser", Edu.Trade, 3000);
            R(Role.Trainer, "Fitness trainer", Edu.HighSchool, 2900);
            R(Role.OfficeClerk, "Office clerk", Edu.HighSchool, 3400, 1500);
            R(Role.Accountant, "Accountant", Edu.Degree, 5800, 2700);
            R(Role.Developer, "Software developer", Edu.Degree, 8000, 3700);
            R(Role.Teller, "Bank teller", Edu.HighSchool, 3000);
            R(Role.LoanOfficer, "Loan officer", Edu.Degree, 5600);
            R(Role.BranchManager, "Branch manager", Edu.Degree, 7200);
            R(Role.Teacher, "Teacher", Edu.Degree, 4600);
            R(Role.Professor, "Professor", Edu.Degree, 6000);
            R(Role.Janitor, "Janitor", Edu.None, 2400);
            R(Role.Nurse, "Nurse", Edu.Degree, 5300);
            R(Role.Doctor, "Doctor", Edu.Medical, 14500);
            R(Role.Receptionist, "Receptionist", Edu.HighSchool, 2800);
            R(Role.PoliceOfficer, "Police officer", Edu.HighSchool, 4700);
            R(Role.Firefighter, "Firefighter", Edu.HighSchool, 4500);
            R(Role.BusDriver, "Bus driver", Edu.HighSchool, 3500);
            R(Role.TownClerk, "Town clerk", Edu.HighSchool, 3700);
            R(Role.ParkKeeper, "Park keeper", Edu.None, 2600);
            R(Role.PropertyManager, "Property manager", Edu.HighSchool, 3900);
            R(Role.Handyman, "Handyman", Edu.None, 3000);
            R(Role.Commuter, "Job out of town", Edu.None, 2300);   // pay depends on their schooling: see Town.CommuteWage

            BuildingDef d;
            // ---------------------------------------------------------------- Residential
            d = D(BuildingType.House, "House", 1, 1, Category.Residential, Zone.Residential, 135000, 24, "A family home. Sold to whoever can get a mortgage, or rented out.");
            d.Homes = 1; d.Rent = 1650; d.Value = 230000; d.Floors = 2;
            d = D(BuildingType.Apartments, "Apartment block", 2, 2, Category.Residential, Zone.Apartments, 1106000, 200, "Twelve flats owned by a landlord who sets the rent.");
            d.Homes = 12; d.Rent = 1150; d.Value = 1900000; d.Floors = 5;

            // ---------------------------------------------------------------- Commercial
            d = D(BuildingType.GroceryStore, "Grocery store", 2, 2, Category.Commercial, Zone.Commercial, 100000, 120, "Buys groceries from food plants (or imports them) and sells them to families.");
            d.Jobs = J(Role.StoreManager, 1, Role.Cashier, 4, Role.StockClerk, 2); d.Sells = Good.Groceries; d.StockCap = 900; d.Output = 70; d.MinPop = 0; d.Value = 300000;
            d = D(BuildingType.Restaurant, "Restaurant", 2, 1, Category.Commercial, Zone.Commercial, 150000, 70, "Cooks and waiters serve meals. Uses groceries.");
            d.Jobs = J(Role.Chef, 1, Role.Cook, 2, Role.Waiter, 3); d.Serves = Service.Meals; d.ServicePrice = 25; d.Output = 80; d.Input = Good.Groceries; d.InputPer = 0.13f; d.StockCap = 80; d.MinPop = 0; d.Value = 300000;
            d = D(BuildingType.Cafe, "Café", 1, 1, Category.Commercial, Zone.Commercial, 70000, 35, "Coffee and pastries.");
            d.Jobs = J(Role.Barista, 3); d.Serves = Service.Coffee; d.ServicePrice = 6; d.Output = 220; d.Input = Good.Groceries; d.InputPer = 0.02f; d.StockCap = 30; d.MinPop = 120; d.Value = 160000; d.Floors = 2;
            d = D(BuildingType.Bar, "Bar", 1, 1, Category.Commercial, Zone.Commercial, 75000, 35, "Where people go after work.");
            d.Jobs = J(Role.Bartender, 3); d.Serves = Service.Drinks; d.ServicePrice = 16; d.Output = 90; d.Input = Good.Groceries; d.InputPer = 0.06f; d.StockCap = 30; d.MinPop = 100; d.Value = 170000; d.Floors = 2;
            d = D(BuildingType.ClothingStore, "Clothing store", 1, 1, Category.Commercial, Zone.Commercial, 80000, 40, "Clothes wear out; people come here to replace them.");
            d.Jobs = J(Role.SalesClerk, 3); d.Sells = Good.Clothes; d.StockCap = 400; d.Output = 60; d.MinPop = 90; d.Value = 180000; d.Floors = 2;
            d = D(BuildingType.ElectronicsStore, "Electronics store", 1, 1, Category.Commercial, Zone.Commercial, 90000, 40, "Phones, TVs and laptops.");
            d.Jobs = J(Role.SalesClerk, 3); d.Sells = Good.Electronics; d.StockCap = 50; d.Output = 10; d.MinPop = 350; d.Value = 200000; d.Floors = 2;
            d = D(BuildingType.FurnitureStore, "Furniture store", 2, 1, Category.Commercial, Zone.Commercial, 120000, 55, "Sofas, beds and kitchens.");
            d.Jobs = J(Role.SalesClerk, 2, Role.StockClerk, 1); d.Sells = Good.Furniture; d.StockCap = 50; d.Output = 10; d.MinPop = 450; d.Value = 260000;
            d = D(BuildingType.GasStation, "Gas station", 2, 1, Category.Commercial, Zone.Commercial, 140000, 50, "Fuel for cars, trucks and farm machines.");
            d.Jobs = J(Role.Attendant, 3); d.Sells = Good.Fuel; d.StockCap = 700; d.Output = 120; d.MinPop = 0; d.Value = 300000;
            d = D(BuildingType.CarDealer, "Car dealership", 2, 2, Category.Commercial, Zone.Commercial, 230000, 90, "Sells new cars it orders in and used ones it takes in trade, usually on a car loan.");
            d.Jobs = J(Role.CarSales, 2, Role.Mechanic, 1); d.Sells = Good.Cars; d.StockCap = 12; d.Output = 2; d.MinPop = 450; d.Value = 520000;
            d = D(BuildingType.AutoRepair, "Auto repair", 1, 1, Category.Commercial, Zone.Commercial, 85000, 40, "Services cars and fixes them when they break down, by the hour plus parts. Cars wait their turn.");
            d.Jobs = J(Role.Mechanic, 3); d.Serves = Service.CarRepair; d.ServicePrice = 95; d.Output = 35; d.Input = Good.Parts; d.InputPer = 0.6f; d.StockCap = 300; d.MinPop = 200; d.Value = 190000;
            d = D(BuildingType.HairSalon, "Hair salon", 1, 1, Category.Commercial, Zone.Commercial, 55000, 30, "Haircuts.");
            d.Jobs = J(Role.Hairdresser, 3); d.Serves = Service.Haircut; d.ServicePrice = 48; d.Output = 28; d.MinPop = 200; d.Value = 140000; d.Floors = 2;
            d = D(BuildingType.Gym, "Gym", 2, 1, Category.Commercial, Zone.Commercial, 130000, 55, "Keeps people healthy and happier.");
            d.Jobs = J(Role.Trainer, 3, Role.Receptionist, 1); d.Serves = Service.Fitness; d.ServicePrice = 15; d.Output = 110; d.MinPop = 260; d.Value = 260000;
            d = D(BuildingType.Office, "Office building", 2, 2, Category.Commercial, Zone.Commercial, 900000, 360, "Accountants, clerks and software developers billing clients outside town. Needs educated people.");
            d.Jobs = J(Role.OfficeClerk, 6, Role.Accountant, 4, Role.Developer, 6); d.MinPop = 0; d.Value = 2000000; d.Floors = 6;
            d = D(BuildingType.Bank, "Bank", 2, 1, Category.Commercial, Zone.Commercial, 300000, 110, "Lends for homes, cars, college and businesses.");
            d.Jobs = J(Role.BranchManager, 1, Role.LoanOfficer, 2, Role.Teller, 2); d.MinPop = 150; d.Value = 600000; d.Floors = 2;

            // ---------------------------------------------------------------- Industrial
            d = D(BuildingType.Farm, "Farm", 3, 3, Category.Industrial, Zone.Farmland, 180000, 80, "Grows crops for the food plant. Surplus is sold out of town. Farms go on farmland.");
            d.Jobs = J(Role.Farmer, 4); d.Makes = Good.Crops; d.Output = 52; d.Input = Good.Fuel; d.InputPer = 0.03f; d.StockCap = 1200; d.Value = 420000;
            d = D(BuildingType.FoodPlant, "Food processing plant", 2, 2, Category.Industrial, Zone.Industrial, 520000, 200, "Turns crops into groceries for the stores.");
            d.Jobs = J(Role.Technician, 1, Role.PlantWorker, 6); d.Makes = Good.Groceries; d.Input = Good.Crops; d.InputPer = 1f; d.Output = 46; d.StockCap = 1200; d.Value = 900000;
            d = D(BuildingType.GarmentFactory, "Garment factory", 2, 2, Category.Industrial, Zone.Industrial, 480000, 190, "Sews clothes from imported parts.");
            d.Jobs = J(Role.Technician, 1, Role.Assembler, 8); d.Makes = Good.Clothes; d.Input = Good.Parts; d.InputPer = 0.35f; d.Output = 75; d.StockCap = 1400; d.Value = 850000;
            d = D(BuildingType.ElectronicsPlant, "Electronics plant", 3, 2, Category.Industrial, Zone.Industrial, 900000, 320, "Assembles devices. Needs engineers and technicians.");
            d.Jobs = J(Role.Engineer, 2, Role.Technician, 2, Role.Assembler, 8); d.Makes = Good.Electronics; d.Input = Good.Parts; d.InputPer = 6f; d.Output = 5.6f; d.StockCap = 300; d.Value = 1600000;
            d = D(BuildingType.FurnitureWorkshop, "Furniture workshop", 2, 2, Category.Industrial, Zone.Industrial, 380000, 160, "Builds furniture from timber and fittings.");
            d.Jobs = J(Role.Technician, 1, Role.Assembler, 5); d.Makes = Good.Furniture; d.Input = Good.Parts; d.InputPer = 4.5f; d.Output = 7f; d.StockCap = 300; d.Value = 700000;
            d = D(BuildingType.Construction, "Construction company", 2, 2, Category.Industrial, Zone.Industrial, 220000, 90, "Builds everything in town. Without one, contractors come from outside and charge more.");
            d.Jobs = J(Role.SiteManager, 1, Role.Electrician, 2, Role.Plumber, 2, Role.ConstructionWorker, 7); d.Value = 450000;
            d = D(BuildingType.Trucking, "Trucking depot", 2, 2, Category.Industrial, Zone.Industrial, 250000, 100, "Moves goods between businesses and to the highway. Without one, outside haulers charge more.");
            d.Jobs = J(Role.WarehouseWorker, 2, Role.TruckDriver, 5); d.Value = 480000; d.MinPop = 300;

            // ---------------------------------------------------------------- Public
            d = D(BuildingType.TownHall, "Town hall", 2, 2, Category.Public, Zone.None, 600000, 220, "Runs the town's books. One per town; the town grows out from it.");
            d.Jobs = J(Role.TownClerk, 3); d.Upkeep = 2500; d.Floors = 3; d.PlayerBuilds = true;
            d = D(BuildingType.Elementary, "Elementary school", 2, 2, Category.Public, Zone.None, 700000, 260, "Ages 6 to 12. A good start makes high school stick.");
            d.Jobs = J(Role.Teacher, 6, Role.Janitor, 1); d.Seats = 150; d.Radius = 22; d.Upkeep = 3000; d.PlayerBuilds = true; d.Floors = 2;
            d = D(BuildingType.HighSchool, "High school", 3, 2, Category.Public, Zone.None, 1100000, 380, "Ages 12 to 18. Graduates qualify for most jobs and for college.");
            d.Jobs = J(Role.Teacher, 9, Role.Janitor, 1); d.Seats = 220; d.Radius = 30; d.Upkeep = 4500; d.PlayerBuilds = true; d.Floors = 3;
            d = D(BuildingType.College, "Community college", 3, 3, Category.Public, Zone.None, 1600000, 520, "Trade certificates (2 years), degrees (4 years), and medical school if there's a hospital.");
            d.Jobs = J(Role.Professor, 9, Role.Receptionist, 1, Role.Janitor, 1); d.Seats = 220; d.Radius = 999; d.Upkeep = 6000; d.PlayerBuilds = true; d.Floors = 3;
            d = D(BuildingType.Clinic, "Clinic", 2, 1, Category.Public, Zone.None, 380000, 150, "Doctors and nurses treat the sick for free.");
            d.Jobs = J(Role.Doctor, 1, Role.Nurse, 2, Role.Receptionist, 1); d.Radius = 26; d.Upkeep = 3500; d.PlayerBuilds = true; d.Floors = 2;
            d = D(BuildingType.Hospital, "Hospital", 3, 3, Category.Public, Zone.None, 2600000, 800, "Treats far more patients and lets the college train doctors.");
            d.Jobs = J(Role.Doctor, 4, Role.Nurse, 10, Role.Receptionist, 2, Role.Janitor, 2); d.Radius = 60; d.Upkeep = 15000; d.PlayerBuilds = true; d.Floors = 5;
            d = D(BuildingType.Police, "Police station", 2, 1, Category.Public, Zone.None, 420000, 160, "People feel safer near it.");
            d.Jobs = J(Role.PoliceOfficer, 5); d.Radius = 24; d.Upkeep = 2500; d.PlayerBuilds = true; d.Floors = 2;
            d = D(BuildingType.FireStation, "Fire station", 2, 1, Category.Public, Zone.None, 420000, 160, "People feel safer near it.");
            d.Jobs = J(Role.Firefighter, 5); d.Radius = 24; d.Upkeep = 2500; d.PlayerBuilds = true; d.Floors = 2;
            d = D(BuildingType.Park, "Park", 2, 2, Category.Public, Zone.None, 60000, 30, "Somewhere to go for free. Makes the neighbourhood nicer.");
            d.Jobs = J(Role.ParkKeeper, 1); d.Radius = 12; d.Upkeep = 500; d.PlayerBuilds = true;
            // the grocery chains: a local company opens the first store, and another as the town grows
            d = D(BuildingType.DiscountStore, "Discount supermarket", 3, 3, Category.Commercial, Zone.Commercial, 300000, 220, "A big no-frills grocery store: the lowest prices in town, bought in bulk. The first Valley Value opens when there's a town big enough to fill it; another for every few hundred more households.");
            d.Jobs = J(Role.StoreManager, 1, Role.Cashier, 6, Role.StockClerk, 4); d.Sells = Good.Groceries; d.StockCap = 3000; d.Output = 120; d.MinPop = 500; d.Value = 800000;
            d.Chain = "Valley Value"; d.PriceFactor = 1.0f; d.BulkBuy = 0.88f; d.Appeal = -0.03f; d.PopPerStore = 1100;
            d = D(BuildingType.Supermarket, "Supermarket", 3, 3, Category.Commercial, Zone.Commercial, 360000, 240, "A bright, well-stocked grocery store: dearer than the discounter, but the families who can afford to care prefer it. Redwood Market comes to bigger towns.");
            d.Jobs = J(Role.StoreManager, 1, Role.Cashier, 5, Role.StockClerk, 4); d.Sells = Good.Groceries; d.StockCap = 2500; d.Output = 100; d.MinPop = 950; d.Value = 900000;
            d.Chain = "Redwood Market"; d.PriceFactor = 1.1f; d.BulkBuy = 0.9f; d.Appeal = 0.12f; d.PopPerStore = 1600;
            d = D(BuildingType.WarehouseClub, "Warehouse club", 4, 4, Category.Commercial, Zone.Commercial, 500000, 320, "Groceries by the pallet at the lowest prices of all, for those who drive: you can't carry a month's shopping home on the bus. Bulkworks Club comes to big towns.");
            d.Jobs = J(Role.StoreManager, 1, Role.Cashier, 4, Role.StockClerk, 4, Role.WarehouseWorker, 3); d.Sells = Good.Groceries; d.StockCap = 6000; d.Output = 160; d.MinPop = 1800; d.Value = 1250000;
            d.Chain = "Bulkworks Club"; d.PriceFactor = 0.9f; d.BulkBuy = 0.8f; d.Appeal = 0f; d.PopPerStore = 3000; d.NeedsCar = true; d.Floors = 2;
            d = D(BuildingType.ParkingLot, "Parking lot", 2, 2, Category.Public, Zone.None, 45000, 25, "Thirteen spaces. Drivers park here and walk to the shops, work and places nearby instead of pulling up at the door, and they'd rather go where they can park.");
            d.Radius = Town.ParkWalk; d.Upkeep = 250; d.PlayerBuilds = true;
            d = D(BuildingType.BusDepot, "Bus depot", 2, 2, Category.Public, Zone.None, 450000, 170, "Sets up bus stops across town and runs buses round them: to work for those without a car, to school for kids too far to walk, to the highway for jobs out of town. Riders pay for a monthly pass.");
            d.Jobs = J(Role.BusDriver, 6); d.Radius = Balance.BusRadius; d.Upkeep = 4000; d.PlayerBuilds = true;

            d = D(BuildingType.Realty, "Property company", 1, 1, Category.Commercial, Zone.Commercial, 90000, 40, "A local firm that builds homes, lets them and looks after them. Its handymen do the repairs; its owners keep what's left of the rent.");
            d.Jobs = J(Role.PropertyManager, 2, Role.Handyman, 3); d.MinPop = 0; d.Value = 220000; d.Floors = 2;

            d = D(BuildingType.FreightTerminal, "Highway & freight terminal", 2, 2, Category.Special, Zone.None, 0, 0, "Where the highway enters town. Imports, exports and newcomers come through here.");
        }
    }
}
