using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>A good: what it's called, what it costs, and how it's bought.</summary>
    public sealed class GoodDef
    {
        public Good Good;
        public string Name, Unit;
        public double Retail, Wholesale, World, Import;
        public bool Whole;                // bought and sold in whole pieces
        public bool MustHave;             // people fetch it from the next town when there's none here
        public float ExportDepth = 2.5f;  // how many plants' worth the buyers out of town take before the price they pay falls
        public BuildingType Store = BuildingType.CarDealer;   // the usual shop for it, where wanting one is counted
    }

    /// <summary>Who in a household something is for.</summary>
    public enum Who { Household, Everyone, Adults, Drinkers, Kids, Readers, Active, Older, Students, PetOwners, Gardeners, Cyclists, Musicians, Wealthy, Flat, Drivers, Toddlers, Carless }

    /// <summary>Something a household keeps a supply of and buys more of when it runs low: from a full supply (1) it wears
    /// out or is used up a little each day. Necessities come out of their cash; wants out of their spending money, if
    /// there's enough of it; luxuries only for the comfortable.</summary>
    public sealed class NeedDef
    {
        public Good Good;
        public Who Who;
        public float Wear;                // of a full supply, a day
        public float Low;                 // they buy when it's down to this
        public float Units;               // a full supply, for each one it's for
        public int Level;                 // 0 necessity, 1 want, 2 luxury
    }

    public enum ServiceKind { Leisure, Routine, Special }

    /// <summary>A service: going out (a share of the household's spending money), something people need now and then
    /// (a chance a day), or something special (a hotel's guests, a funeral).</summary>
    public sealed class ServiceDef
    {
        public Service Service;
        public string Name, Unit;
        public ServiceKind Kind;
        public float Share;               // leisure: the share of spending money it takes
        public int MaxPerDay = 1;
        public float MinAge = 14, MaxAge = 200;
        public float Chance;              // routine: a day, for each one it's for
        public Who Who = Who.Everyone;
        public bool FromFun;              // routine: paid from spending money (otherwise it's a bill they pay)
        public float Health;              // what a visit does for someone's health
        public BuildingType Venue;        // the usual place for it, where wanting one is counted
    }

    /// <summary>The trade one kind of shop or service is in: the good it sells or the service it gives, and the kinds of
    /// place that do it (a pizzeria, a diner and a sushi bar are all in meals).</summary>
    public sealed class Market
    {
        public Good? Good;
        public Service Service;
        public BuildingType Primary;
        public BuildingType[] Kinds;
        public string Name { get { return Good.HasValue ? Defs.Name(Good.Value) : Defs.ServiceName(Service); } }
    }

    public static partial class Defs
    {
        public static GoodDef[] Goods;
        public static NeedDef[] Needs;
        static readonly Dictionary<Service, ServiceDef> services = new Dictionary<Service, ServiceDef>();
        public static ServiceDef ServiceInfo(Service s) { return services[s]; }
        public static IEnumerable<ServiceDef> AllServices { get { return services.Values; } }
        public static GoodDef GoodInfo(Good g) { return Goods[(int)g]; }
        public static List<Market> Markets;

        // ================================================================== goods
        static void G(Good g, string name, string unit, double retail, double wholesale, double world, double import, BuildingType store = BuildingType.CarDealer, bool whole = false, bool must = false, float depth = 2.5f)
        {
            Goods[(int)g] = new GoodDef { Good = g, Name = name, Unit = unit, Retail = retail, Wholesale = wholesale, World = world, Import = import, Store = store, Whole = whole, MustHave = must, ExportDepth = depth };
        }

        static void BuildGoods()
        {
            int n = GoodCount;
            Goods = new GoodDef[n];
            G(Good.Crops, "Crops", "t", 0, 23, 19, 27, depth: 8);
            G(Good.Groceries, "Groceries", "wk", 78, 56, 50, 64, BuildingType.GroceryStore, must: true, depth: 4);
            G(Good.Clothes, "Clothes", "pcs", 68, 40, 35, 46, BuildingType.ClothingStore);
            G(Good.Electronics, "Electronics", "pcs", 880, 640, 600, 700, BuildingType.ElectronicsStore, whole: true);
            G(Good.Furniture, "Furniture", "pcs", 690, 450, 420, 500, BuildingType.FurnitureStore, whole: true);
            G(Good.Fuel, "Fuel", "tank", 58, 46, 42, 46, BuildingType.GasStation, must: true);
            G(Good.Parts, "Parts", "kits", 0, 40, 40, 42);
            G(Good.Materials, "Materials", "lots", 0, 1000, 1000, 1050);
            G(Good.Cars, "Cars", "cars", 29000, 24900, 25000, 25500, BuildingType.CarDealer, whole: true);
            // raw materials
            G(Good.Logs, "Logs", "t", 0, 34, 30, 38, depth: 4);
            G(Good.Ore, "Ore", "t", 0, 50, 45, 56);
            G(Good.Stone, "Stone", "t", 0, 17, 14, 19, depth: 4);
            G(Good.Milk, "Milk", "100 l", 0, 32, 28, 36, depth: 5);
            G(Good.Livestock, "Livestock", "head", 0, 135, 120, 150, depth: 5);
            // what the mills and plants make from them
            G(Good.Lumber, "Lumber", "m³", 0, 100, 90, 112);
            G(Good.Steel, "Steel", "t", 0, 178, 160, 200);
            G(Good.Chemicals, "Chemicals", "drums", 0, 78, 70, 88);
            G(Good.Plastics, "Plastics", "t", 0, 106, 95, 118);
            G(Good.Textiles, "Textiles", "rolls", 0, 25, 22, 28);
            G(Good.Leather, "Leather", "hides", 0, 66, 60, 75);
            G(Good.Paper, "Paper", "reams", 0, 44, 40, 50);
            G(Good.Glass, "Glass", "crates", 0, 39, 35, 44);
            G(Good.Flour, "Flour", "sacks", 0, 38, 34, 43);
            // more things people buy
            G(Good.Shoes, "Shoes", "pairs", 85, 52, 46, 58, BuildingType.ShoeStore);
            G(Good.Appliances, "Appliances", "pcs", 720, 470, 430, 520, BuildingType.ApplianceStore, whole: true);
            G(Good.Hardware, "Hardware", "pcs", 42, 26, 23, 29, BuildingType.HardwareStore);
            G(Good.Medicine, "Medicine", "packs", 38, 22, 20, 26, BuildingType.Pharmacy, must: true);
            G(Good.Toiletries, "Toiletries", "pcs", 9, 5.5, 5, 6.4, BuildingType.Drugstore, must: true);
            G(Good.Books, "Books", "pcs", 22, 12, 10.5, 13.5, BuildingType.Bookstore);
            G(Good.Toys, "Toys", "pcs", 32, 18, 16, 20.5, BuildingType.ToyStore);
            G(Good.SportingGoods, "Sporting goods", "pcs", 95, 56, 50, 64, BuildingType.SportsStore);
            G(Good.Jewelry, "Jewelry", "pcs", 420, 230, 210, 270, BuildingType.Jeweler, whole: true);
            G(Good.Beverages, "Beer, wine & spirits", "cases", 10, 6, 5.2, 6.8, BuildingType.LiquorStore);
            G(Good.PetSupplies, "Pet supplies", "bags", 18, 10.5, 9.5, 12, BuildingType.PetStore, must: true);
            G(Good.Plants, "Plants & flowers", "pots", 35, 19, 17, 22, BuildingType.GardenCenter);
            G(Good.Bicycles, "Bicycles", "bikes", 480, 300, 270, 345, BuildingType.BikeShop, whole: true);
            G(Good.Instruments, "Instruments", "pcs", 650, 400, 360, 460, BuildingType.MusicStore, whole: true);
            G(Good.Stationery, "Stationery", "packs", 14, 7.5, 6.5, 8.5, BuildingType.StationeryStore);
            G(Good.Homewares, "Homewares", "pcs", 55, 31, 28, 36, BuildingType.HomeGoodsStore);
            Retail = Goods.Select(x => x.Retail).ToArray();
            Wholesale = Goods.Select(x => x.Wholesale).ToArray();
            World = Goods.Select(x => x.World).ToArray();
            Import = Goods.Select(x => x.Import).ToArray();
            GoodNames = Goods.Select(x => x.Name).ToArray();
            GoodUnits = Goods.Select(x => x.Unit).ToArray();

            // what households keep a supply of (groceries, clothes, electronics, furniture, fuel and cars have their own rules)
            Needs = new[]
            {
                N(Good.Shoes, Who.Everyone, 0.03f, 0.4f, 1, 1),
                N(Good.Appliances, Who.Household, 0.01f, 0.35f, 1, 1),
                N(Good.Hardware, Who.Household, 0.12f, 0.4f, 2, 1),
                N(Good.Medicine, Who.Older, 0.2f, 0.4f, 1, 0),
                N(Good.Toiletries, Who.Everyone, 0.14f, 0.4f, 3, 0),
                N(Good.Books, Who.Readers, 0.09f, 0.3f, 1, 1),
                N(Good.Toys, Who.Kids, 0.2f, 0.4f, 1, 1),
                N(Good.SportingGoods, Who.Active, 0.063f, 0.3f, 0.5f, 1),
                N(Good.Jewelry, Who.Wealthy, 0.012f, 0.3f, 1, 2),
                N(Good.Beverages, Who.Drinkers, 0.75f, 0.4f, 1.5f, 1),
                N(Good.PetSupplies, Who.PetOwners, 0.28f, 0.4f, 4, 0),
                N(Good.Plants, Who.Gardeners, 0.11f, 0.4f, 2, 1),
                N(Good.Bicycles, Who.Cyclists, 0.004f, 0.3f, 1, 1),
                N(Good.Instruments, Who.Musicians, 0.006f, 0.3f, 1, 2),
                N(Good.Stationery, Who.Students, 0.09f, 0.4f, 2, 0),
                N(Good.Homewares, Who.Household, 0.04f, 0.4f, 3, 1),
            };
        }

        static NeedDef N(Good g, Who who, float wear, float low, float units, int level)
        {
            return new NeedDef { Good = g, Who = who, Wear = wear, Low = low, Units = units, Level = level };
        }

        // ================================================================== jobs
        static void Roles()
        {
            R(Role.Machinist, "Machinist", Edu.Trade, 4400);
            R(Role.Chemist, "Chemist", Edu.Degree, 6400);
            R(Role.LabTech, "Lab technician", Edu.Trade, 4000);
            R(Role.Craftsman, "Craftsman", Edu.Trade, 3800);
            R(Role.Butcher, "Butcher", Edu.Trade, 3300);
            R(Role.Baker, "Baker", Edu.Trade, 3200);
            R(Role.Brewer, "Brewer", Edu.Trade, 3700);
            R(Role.Printer, "Printer", Edu.Trade, 3600);
            R(Role.Pharmacist, "Pharmacist", Edu.Degree, 7800);
            R(Role.Dentist, "Dentist", Edu.Medical, 13000);
            R(Role.DentalHygienist, "Dental hygienist", Edu.Trade, 4300);
            R(Role.Optometrist, "Optometrist", Edu.Degree, 7000);
            R(Role.Veterinarian, "Veterinarian", Edu.Degree, 7200);
            R(Role.VetTech, "Vet technician", Edu.Trade, 3400);
            R(Role.Physiotherapist, "Physiotherapist", Edu.Degree, 5600);
            R(Role.Lawyer, "Lawyer", Edu.Degree, 9000, 4200);
            R(Role.Paralegal, "Paralegal", Edu.HighSchool, 3600, 1700);
            R(Role.Architect, "Architect", Edu.Degree, 7000, 3300);
            R(Role.Designer, "Designer", Edu.Degree, 5200, 2500);
            R(Role.Agent, "Agent", Edu.HighSchool, 3500, 1800);
            R(Role.CallAgent, "Call agent", Edu.None, 2500, 1100);
            R(Role.Instructor, "Instructor", Edu.Trade, 3300);
            R(Role.CareWorker, "Childcare worker", Edu.HighSchool, 2900);
            R(Role.Beautician, "Beautician", Edu.Trade, 3000);
            R(Role.TattooArtist, "Tattoo artist", Edu.Trade, 3400);
            R(Role.Usher, "Attendant", Edu.None, 2250);
            R(Role.Performer, "Performer", Edu.HighSchool, 3000);
            R(Role.Housekeeper, "Housekeeper", Edu.None, 2300);
            R(Role.Driver, "Driver", Edu.HighSchool, 3000);
            R(Role.Mortician, "Funeral director", Edu.Trade, 4200);
            R(Role.Tailor, "Tailor", Edu.Trade, 3100);
            R(Role.Florist, "Florist", Edu.None, 2400);
            R(Role.Executive, "Executive", Edu.Degree, 9500);
            // engineers in an engineering firm bill clients out of town
            roles[Role.Engineer].Billing = 3100;
        }

        // ================================================================== kinds of business
        /// <summary>A mill, plant, factory, workshop or farm.</summary>
        static BuildingDef Plant(BuildingType t, string name, int w, int h, Zone z, double materials, float work, double value, Good makes, float output, string suffix, string blurb, params object[] jobs)
        {
            var d = D(t, name, w, h, Category.Industrial, z, materials, work, blurb);
            d.Jobs = J(jobs); d.Makes = makes; d.Output = output; d.Value = value; d.Suffix = suffix;
            d.StockCap = (float)Math.Ceiling(output * d.TotalJobs * 8);
            return d;
        }

        /// <summary>A shop.</summary>
        static BuildingDef Store(BuildingType t, string name, int w, int h, double materials, float work, double value, Good sells, float output, float stock, int minPop, int popPer, string suffix, string blurb, params object[] jobs)
        {
            var d = D(t, name, w, h, Category.Commercial, Zone.Commercial, materials, work, blurb);
            d.Jobs = J(jobs); d.Sells = sells; d.Output = output; d.StockCap = stock; d.MinPop = minPop; d.PopPer = popPer; d.Value = value; d.Suffix = suffix;
            return d;
        }

        /// <summary>A place that serves people: a restaurant, a gym, a dentist.</summary>
        static BuildingDef Place(BuildingType t, string name, int w, int h, double materials, float work, double value, Service s, double price, float output, int minPop, int popPer, string suffix, string blurb, params object[] jobs)
        {
            var d = D(t, name, w, h, Category.Commercial, Zone.Commercial, materials, work, blurb);
            d.Jobs = J(jobs); d.Serves = s; d.ServicePrice = price; d.Output = output; d.MinPop = minPop; d.PopPer = popPer; d.Value = value; d.Suffix = suffix;
            d.Ticket = price;
            return d;
        }

        /// <summary>An office whose staff bill clients out of town.</summary>
        static BuildingDef Firm(BuildingType t, string name, int w, int h, double materials, float work, double value, int floors, string suffix, string blurb, params object[] jobs)
        {
            var d = D(t, name, w, h, Category.Commercial, Zone.Commercial, materials, work, blurb);
            d.Jobs = J(jobs); d.Value = value; d.Floors = floors; d.Suffix = suffix; d.Bills = true; d.Branded = true;
            return d;
        }

        static void Catalog()
        {
            BuildingDef d;
            // ---------------------------------------------------------------- farms
            d = Plant(BuildingType.DairyFarm, "Dairy farm", 3, 3, Zone.Farmland, 190000, 80, 440000, Good.Milk, 44, " Dairy", "Milks a herd for the dairy plant. Goes on farmland.", Role.Farmer, 4);
            d.In(Good.Fuel, 0.02f);
            d = Plant(BuildingType.CattleRanch, "Cattle ranch", 3, 3, Zone.Farmland, 160000, 70, 400000, Good.Livestock, 12, " Ranch", "Raises beef cattle for the meatpacker and the tannery, fed on crops. Goes on farmland.", Role.Farmer, 3);
            d.In(Good.Crops, 0.6f);
            d = Plant(BuildingType.PoultryFarm, "Poultry farm", 3, 3, Zone.Farmland, 170000, 70, 400000, Good.Livestock, 13, " Poultry", "Long barns of chickens for the meatpacker. Goes on farmland.", Role.Farmer, 3, Role.PlantWorker, 1);
            d.In(Good.Crops, 0.9f);
            d = Plant(BuildingType.TreeFarm, "Tree farm", 3, 3, Zone.Farmland, 120000, 50, 320000, Good.Logs, 40, " Timber", "Grows and fells timber for the sawmill and the paper mill. Goes on farmland.", Role.Farmer, 3);
            d.In(Good.Fuel, 0.02f);
            d = Plant(BuildingType.Orchard, "Orchard", 3, 3, Zone.Farmland, 150000, 60, 380000, Good.Crops, 64, " Orchards", "Apples, pears and cherries for the cannery, the cider press and the shops. Goes on farmland.", Role.Farmer, 4);
            d.In(Good.Fuel, 0.02f);
            d = Plant(BuildingType.Vineyard, "Vineyard", 3, 3, Zone.Farmland, 160000, 60, 400000, Good.Crops, 64, " Vineyards", "Grapes for the winery. Goes on farmland.", Role.Farmer, 4);
            d.In(Good.Fuel, 0.02f);
            d = Plant(BuildingType.PlantNursery, "Plant nursery", 3, 3, Zone.Farmland, 170000, 70, 380000, Good.Plants, 74, " Nurseries", "Greenhouses of flowers, shrubs and seedlings for the garden centers and florists. Goes on farmland.", Role.Farmer, 3, Role.PlantWorker, 1);
            d.In(Good.Fuel, 0.02f);

            // ---------------------------------------------------------------- mills and plants
            d = Plant(BuildingType.Sawmill, "Sawmill", 3, 2, Zone.Industrial, 420000, 170, 760000, Good.Lumber, 41, " Lumber", "Saws logs into lumber for the furniture makers and the builders' merchants.", Role.Technician, 1, Role.PlantWorker, 6);
            d.In(Good.Logs, 1.3f);
            d = Plant(BuildingType.PaperMill, "Paper mill", 3, 3, Zone.Industrial, 620000, 240, 1100000, Good.Paper, 90, " Paper", "Pulps logs into paper for the printers.", Role.Technician, 2, Role.PlantWorker, 7);
            d.In(Good.Logs, 0.45f).In(Good.Chemicals, 0.04f);
            d = Plant(BuildingType.SteelMill, "Steel mill", 3, 3, Zone.Industrial, 950000, 360, 1700000, Good.Steel, 25, " Steel", "Melts ore into steel for the machine shops and the appliance and bicycle makers.", Role.Engineer, 1, Role.Technician, 2, Role.PlantWorker, 9);
            d.In(Good.Ore, 1.2f).In(Good.Fuel, 0.35f);
            d = Plant(BuildingType.MetalShop, "Machine shop", 2, 2, Zone.Industrial, 360000, 150, 650000, Good.Parts, 91, " Machining", "Machinists turn steel into parts for garages and factories.", Role.Machinist, 4, Role.Assembler, 2);
            d.In(Good.Steel, 0.09f);
            d = Plant(BuildingType.AutoPartsPlant, "Auto parts plant", 3, 2, Zone.Industrial, 620000, 250, 1100000, Good.Parts, 93, " Components", "Stamps and molds parts for cars and machines.", Role.Engineer, 1, Role.Technician, 1, Role.Assembler, 8);
            d.In(Good.Steel, 0.07f).In(Good.Plastics, 0.05f);
            d = Plant(BuildingType.ChemicalPlant, "Chemical plant", 3, 2, Zone.Industrial, 800000, 300, 1400000, Good.Chemicals, 59, " Chemicals", "Cracks fuel into chemicals for the plastics, paint, soap and drug makers. Needs a chemist.", Role.Chemist, 1, Role.Technician, 2, Role.PlantWorker, 5);
            d.In(Good.Fuel, 0.75f);
            d = Plant(BuildingType.PlasticsPlant, "Plastics plant", 2, 2, Zone.Industrial, 650000, 250, 1150000, Good.Plastics, 39, " Polymers", "Turns chemicals into plastic pellets for the toy, shoe and parts makers.", Role.Technician, 2, Role.PlantWorker, 6);
            d.In(Good.Chemicals, 0.55f);
            d = Plant(BuildingType.TextileMill, "Textile mill", 3, 2, Zone.Industrial, 500000, 200, 900000, Good.Textiles, 144, " Mills", "Spins and weaves crops into cloth for the garment, furniture and sports factories.", Role.Technician, 1, Role.PlantWorker, 7);
            d.In(Good.Crops, 0.4f);
            d = Plant(BuildingType.Tannery, "Tannery", 2, 2, Zone.Industrial, 380000, 150, 680000, Good.Leather, 56, " Leather", "Tans hides into leather for the shoe factory.", Role.Technician, 1, Role.PlantWorker, 5);
            d.In(Good.Livestock, 0.18f).In(Good.Chemicals, 0.04f);
            d = Plant(BuildingType.Glassworks, "Glassworks", 2, 2, Zone.Industrial, 520000, 200, 920000, Good.Glass, 98, " Glass", "Furnaces melt stone and sand into glass for the potteries.", Role.Technician, 2, Role.PlantWorker, 5);
            d.In(Good.Stone, 0.7f).In(Good.Fuel, 0.08f);
            d = Plant(BuildingType.Quarry, "Quarry", 3, 3, Zone.Industrial, 300000, 120, 560000, Good.Stone, 120, " Quarry", "Cuts and crushes stone for the brickworks, the concrete plant and the glassworks.", Role.Technician, 1, Role.PlantWorker, 5);
            d.In(Good.Fuel, 0.05f);
            d = Plant(BuildingType.Brickworks, "Brickworks", 3, 2, Zone.Industrial, 480000, 190, 850000, Good.Materials, 2.5f, " Brick", "Fires bricks and blocks for the builders.", Role.Technician, 1, Role.PlantWorker, 6);
            d.In(Good.Stone, 12f).In(Good.Fuel, 3f);
            d = Plant(BuildingType.ConcretePlant, "Concrete plant", 2, 2, Zone.Industrial, 450000, 180, 800000, Good.Materials, 2.3f, " Concrete", "Mixes concrete and trucks it out to the building sites.", Role.Technician, 1, Role.PlantWorker, 4, Role.TruckDriver, 2);
            d.In(Good.Stone, 15f);
            d = Plant(BuildingType.FlourMill, "Flour mill", 2, 2, Zone.Industrial, 380000, 150, 700000, Good.Flour, 120, " Milling", "Mills grain into flour for the bakeries.", Role.Technician, 1, Role.PlantWorker, 4);
            d.In(Good.Crops, 0.7f);
            d = Plant(BuildingType.BreadFactory, "Bread factory", 2, 2, Zone.Industrial, 420000, 170, 760000, Good.Groceries, 59, " Bakeries", "Bakes bread and cakes by the truckload for the grocery stores.", Role.Baker, 2, Role.PlantWorker, 5);
            d.In(Good.Flour, 0.55f);
            d = Plant(BuildingType.DairyPlant, "Dairy plant", 2, 2, Zone.Industrial, 480000, 190, 860000, Good.Groceries, 74, " Creamery", "Bottles milk and makes cheese and yogurt for the stores.", Role.Technician, 1, Role.PlantWorker, 6);
            d.In(Good.Milk, 0.75f);
            d = Plant(BuildingType.Meatpacking, "Meatpacking plant", 2, 2, Zone.Industrial, 520000, 200, 920000, Good.Groceries, 67, " Meats", "Butchers cattle and poultry for the stores.", Role.Butcher, 3, Role.PlantWorker, 5);
            d.In(Good.Livestock, 0.17f);
            d = Plant(BuildingType.Cannery, "Cannery", 2, 2, Zone.Industrial, 460000, 180, 820000, Good.Groceries, 64, " Canning", "Cans and freezes fruit and vegetables for the stores.", Role.Technician, 1, Role.PlantWorker, 6);
            d.In(Good.Crops, 0.9f);
            d = Plant(BuildingType.Brewery, "Brewery", 2, 2, Zone.Industrial, 420000, 170, 780000, Good.Beverages, 545, " Brewing", "Brews beer from grain for the bars and the liquor stores.", Role.Brewer, 2, Role.PlantWorker, 4);
            d.In(Good.Crops, 0.08f);
            d = Plant(BuildingType.Winery, "Winery", 2, 2, Zone.Industrial, 380000, 150, 720000, Good.Beverages, 635, " Cellars", "Presses and bottles wine.", Role.Brewer, 2, Role.PlantWorker, 3);
            d.In(Good.Crops, 0.09f);
            d = Plant(BuildingType.Distillery, "Distillery", 2, 2, Zone.Industrial, 400000, 160, 760000, Good.Beverages, 516, " Distilling", "Copper stills turn grain into whiskey and gin.", Role.Brewer, 2, Role.PlantWorker, 3);
            d.In(Good.Crops, 0.07f);
            d = Plant(BuildingType.BottlingPlant, "Bottling plant", 2, 2, Zone.Industrial, 450000, 180, 820000, Good.Beverages, 447, " Bottling", "Mixes and bottles soft drinks.", Role.Technician, 1, Role.PlantWorker, 6);
            d.In(Good.Chemicals, 0.02f);
            d = Plant(BuildingType.PetFoodPlant, "Pet food plant", 2, 2, Zone.Industrial, 400000, 160, 740000, Good.PetSupplies, 310, " Pet Foods", "Makes pet food from grain and meat for the pet stores.", Role.Technician, 1, Role.PlantWorker, 5);
            d.In(Good.Livestock, 0.012f).In(Good.Crops, 0.09f);

            // ---------------------------------------------------------------- factories and workshops
            d = Plant(BuildingType.ShoeFactory, "Shoe factory", 2, 2, Zone.Industrial, 460000, 180, 840000, Good.Shoes, 72, " Footwear", "Cuts and stitches shoes from leather and rubber.", Role.Technician, 1, Role.Assembler, 7);
            d.In(Good.Leather, 0.22f).In(Good.Plastics, 0.05f);
            d = Plant(BuildingType.AppliancePlant, "Appliance plant", 3, 2, Zone.Industrial, 850000, 330, 1500000, Good.Appliances, 8.8f, " Appliances", "Builds fridges, washers and stoves from steel and parts. Needs an engineer.", Role.Engineer, 1, Role.Technician, 2, Role.Assembler, 8);
            d.In(Good.Steel, 0.7f).In(Good.Parts, 1.6f);
            d = Plant(BuildingType.ToolWorks, "Tool works", 2, 2, Zone.Industrial, 420000, 170, 760000, Good.Hardware, 176, " Tools", "Forges hand tools, hinges and fixings for the hardware stores.", Role.Machinist, 3, Role.Assembler, 4);
            d.In(Good.Steel, 0.06f);
            d = Plant(BuildingType.PaintFactory, "Paint factory", 2, 2, Zone.Industrial, 440000, 170, 780000, Good.Hardware, 155, " Paints", "Mixes paint and varnish for the hardware stores. Needs a chemist.", Role.Chemist, 1, Role.Technician, 1, Role.PlantWorker, 4);
            d.In(Good.Chemicals, 0.12f);
            d = Plant(BuildingType.PharmaLab, "Pharmaceutical lab", 2, 2, Zone.Industrial, 850000, 320, 1500000, Good.Medicine, 178, " Pharma", "Chemists and lab technicians make medicines for the pharmacies.", Role.Chemist, 3, Role.LabTech, 4);
            d.In(Good.Chemicals, 0.06f);
            d = Plant(BuildingType.SoapWorks, "Soap works", 2, 2, Zone.Industrial, 380000, 150, 700000, Good.Toiletries, 575, " Soap", "Makes soap, shampoo and toothpaste for the drugstores.", Role.Technician, 1, Role.PlantWorker, 5);
            d.In(Good.Chemicals, 0.025f);
            d = Plant(BuildingType.PrintingPress, "Printing press", 2, 2, Zone.Industrial, 420000, 170, 760000, Good.Books, 415, " Press", "Prints and binds books for the bookstores.", Role.Printer, 3, Role.PlantWorker, 3);
            d.In(Good.Paper, 0.12f);
            d = Plant(BuildingType.PaperGoodsPlant, "Paper goods plant", 2, 2, Zone.Industrial, 360000, 140, 660000, Good.Stationery, 560, " Paper Goods", "Makes notebooks, envelopes and school supplies.", Role.Technician, 1, Role.PlantWorker, 5);
            d.In(Good.Paper, 0.07f);
            d = Plant(BuildingType.ToyFactory, "Toy factory", 2, 2, Zone.Industrial, 440000, 170, 800000, Good.Toys, 184, " Toys", "Molds and paints toys from plastic.", Role.Technician, 1, Role.Assembler, 7);
            d.In(Good.Plastics, 0.06f);
            d = Plant(BuildingType.SportsGoodsFactory, "Sporting goods factory", 2, 2, Zone.Industrial, 460000, 180, 840000, Good.SportingGoods, 72, " Athletics", "Makes balls, rackets, helmets and sportswear.", Role.Technician, 1, Role.Assembler, 7);
            d.In(Good.Plastics, 0.15f).In(Good.Textiles, 0.3f);
            d = Plant(BuildingType.BicycleWorks, "Bicycle works", 2, 2, Zone.Industrial, 440000, 170, 800000, Good.Bicycles, 16, " Cycles", "Welds frames and builds bicycles.", Role.Machinist, 2, Role.Assembler, 5);
            d.In(Good.Steel, 0.5f).In(Good.Parts, 1.2f);
            d = Plant(BuildingType.InstrumentWorkshop, "Instrument workshop", 2, 1, Zone.Industrial, 300000, 120, 560000, Good.Instruments, 6.4f, " Instruments", "Craftsmen build guitars, violins and pianos from fine lumber.", Role.Craftsman, 5);
            d.In(Good.Lumber, 0.7f);
            d = Plant(BuildingType.JewelryWorkshop, "Jewelry workshop", 1, 1, Zone.Industrial, 280000, 110, 540000, Good.Jewelry, 12, " Goldsmiths", "Goldsmiths make rings, chains and watches from precious metal.", Role.Craftsman, 4);
            d.In(Good.Ore, 1.0f);
            d = Plant(BuildingType.Pottery, "Pottery", 2, 2, Zone.Industrial, 340000, 140, 640000, Good.Homewares, 118, " Pottery", "Throws and fires dishes, vases and glassware.", Role.Craftsman, 2, Role.PlantWorker, 4);
            d.In(Good.Glass, 0.25f).In(Good.Stone, 0.15f);
            d = Plant(BuildingType.MattressFactory, "Mattress factory", 2, 2, Zone.Industrial, 420000, 170, 760000, Good.Furniture, 8, " Sleep Co.", "Springs and cloth into mattresses and sofas.", Role.Technician, 1, Role.Assembler, 6);
            d.In(Good.Textiles, 3f).In(Good.Steel, 0.6f);
            d = Plant(BuildingType.CabinetShop, "Cabinet shop", 2, 2, Zone.Industrial, 360000, 150, 660000, Good.Furniture, 6.8f, " Cabinetry", "Craftsmen build kitchens, cabinets and tables from lumber.", Role.Craftsman, 4, Role.Assembler, 2);
            d.In(Good.Lumber, 1.4f);
            foreach (var t in new[] { BuildingType.SteelMill, BuildingType.PaperMill, BuildingType.ChemicalPlant, BuildingType.PlasticsPlant, BuildingType.AutoPartsPlant,
                                      BuildingType.AppliancePlant, BuildingType.PharmaLab, BuildingType.TextileMill, BuildingType.Brewery, BuildingType.BottlingPlant, BuildingType.Meatpacking })
                Get(t).Branded = true;

            // ---------------------------------------------------------------- shops
            Store(BuildingType.ShoeStore, "Shoe store", 1, 1, 75000, 35, 170000, Good.Shoes, 60, 400, 350, 900, " Shoes", "Shoes for the family. People wear out a few pairs a year.", Role.SalesClerk, 2);
            Store(BuildingType.ApplianceStore, "Appliance store", 2, 1, 130000, 55, 280000, Good.Appliances, 8, 60, 700, 1600, " Appliances", "Fridges, washers and vacuums.", Role.SalesClerk, 2, Role.StockClerk, 1);
            Store(BuildingType.HardwareStore, "Hardware store", 2, 1, 140000, 60, 300000, Good.Hardware, 120, 1200, 300, 900, " Hardware", "Tools, paint and everything for fixing up a house.", Role.SalesClerk, 2, Role.StockClerk, 1);
            Store(BuildingType.Pharmacy, "Pharmacy", 1, 1, 110000, 45, 240000, Good.Medicine, 160, 1500, 250, 900, " Pharmacy", "Medicines, mostly for older people. Needs a pharmacist.", Role.Pharmacist, 1, Role.Cashier, 2);
            Store(BuildingType.Drugstore, "Drugstore", 1, 1, 90000, 40, 200000, Good.Toiletries, 600, 6000, 200, 800, " Drug", "Soap, shampoo, toothpaste: what every family runs through.", Role.Cashier, 2, Role.StockClerk, 1);
            Store(BuildingType.Bookstore, "Bookstore", 1, 1, 70000, 35, 160000, Good.Books, 220, 1500, 400, 1500, " Books", "Books for readers young and old.", Role.SalesClerk, 2);
            Store(BuildingType.ToyStore, "Toy store", 1, 1, 70000, 35, 160000, Good.Toys, 150, 1000, 400, 1300, " Toys", "Toys for the town's kids.", Role.SalesClerk, 2);
            Store(BuildingType.SportsStore, "Sporting goods store", 2, 1, 110000, 50, 240000, Good.SportingGoods, 55, 400, 450, 1500, " Sports", "Balls, bikes helmets, running shoes and gym kit.", Role.SalesClerk, 3);
            d = Store(BuildingType.Jeweler, "Jeweler", 1, 1, 95000, 40, 220000, Good.Jewelry, 12, 80, 500, 1800, " Jewelers", "Rings and watches for the families who can afford them.", Role.SalesClerk, 2);
            d.Appeal = 0.1f;
            Store(BuildingType.LiquorStore, "Liquor store", 1, 1, 70000, 30, 160000, Good.Beverages, 500, 4000, 250, 900, " Wine & Spirits", "Beer, wine and spirits. Only for adults.", Role.Cashier, 2);
            Store(BuildingType.PetStore, "Pet store", 1, 1, 70000, 30, 160000, Good.PetSupplies, 280, 2000, 300, 1100, " Pets", "Food and supplies for the town's dogs and cats.", Role.SalesClerk, 2);
            Store(BuildingType.GardenCenter, "Garden center", 2, 2, 120000, 50, 260000, Good.Plants, 150, 1500, 400, 1600, " Garden Center", "Plants, seeds and compost for people with gardens: busiest in spring.", Role.SalesClerk, 2, Role.StockClerk, 1);
            d = Store(BuildingType.Florist, "Florist", 1, 1, 55000, 25, 140000, Good.Plants, 120, 600, 300, 1200, " Flowers", "Cut flowers and potted plants.", Role.Florist, 2);
            d.Appeal = 0.1f; d.PriceFactor = 1.1f;
            Store(BuildingType.BikeShop, "Bike shop", 1, 1, 70000, 30, 170000, Good.Bicycles, 10, 60, 450, 1800, " Cycles", "Sells and fixes bicycles.", Role.SalesClerk, 1, Role.Mechanic, 1);
            Store(BuildingType.MusicStore, "Music store", 1, 1, 75000, 30, 170000, Good.Instruments, 8, 40, 600, 2500, " Music", "Guitars, keyboards, drums and sheet music.", Role.SalesClerk, 2);
            Store(BuildingType.StationeryStore, "Stationery store", 1, 1, 55000, 25, 130000, Good.Stationery, 350, 2500, 350, 1500, " Paper Co.", "Notebooks, pens and school supplies.", Role.SalesClerk, 1, Role.Cashier, 1);
            Store(BuildingType.HomeGoodsStore, "Home goods store", 2, 1, 110000, 50, 240000, Good.Homewares, 100, 900, 450, 1300, " Home", "Dishes, towels, lamps and decor.", Role.SalesClerk, 2, Role.StockClerk, 1);
            // other kinds of shop for clothes, electronics, furniture and food
            d = Store(BuildingType.Boutique, "Boutique", 1, 1, 80000, 35, 190000, Good.Clothes, 40, 250, 400, 900, " Boutique", "Fashion at a price, for those who can afford to care.", Role.SalesClerk, 2);
            d.Appeal = 0.22f; d.PriceFactor = 1.25f;
            d = Store(BuildingType.ThriftStore, "Thrift store", 1, 1, 50000, 25, 120000, Good.Clothes, 60, 400, 300, 1000, " Thrift", "Second-hand clothes for a lot less. It buys what people give away for next to nothing.", Role.SalesClerk, 1, Role.Cashier, 1);
            d.Appeal = -0.12f; d.PriceFactor = 0.6f; d.BulkBuy = 0.5f;
            d = Store(BuildingType.DepartmentStore, "Department store", 2, 2, 320000, 130, 720000, Good.Clothes, 80, 1500, 900, 2500, " Department Store", "Floors of clothes for the whole family.", Role.StoreManager, 1, Role.SalesClerk, 5, Role.Cashier, 2);
            d.Appeal = 0.06f; d.BulkBuy = 0.92f; d.Floors = 2; d.Branded = true;
            Store(BuildingType.PhoneStore, "Phone store", 1, 1, 80000, 35, 180000, Good.Electronics, 8, 50, 300, 900, " Mobile", "Phones, tablets and laptops.", Role.SalesClerk, 2);
            Store(BuildingType.MattressStore, "Mattress store", 1, 1, 80000, 35, 180000, Good.Furniture, 8, 40, 500, 1500, " Sleep", "Beds and mattresses.", Role.SalesClerk, 2);
            d = Store(BuildingType.ConvenienceStore, "Convenience store", 1, 1, 70000, 30, 160000, Good.Groceries, 50, 400, 150, 500, " Corner Store", "A small grocery round the corner: open late, a bit dearer.", Role.Cashier, 1);
            d.PriceFactor = 1.15f;
            d = Store(BuildingType.Butcher, "Butcher shop", 1, 1, 70000, 30, 160000, Good.Groceries, 40, 300, 300, 900, " Meats", "Fresh meat, cut to order.", Role.Butcher, 1);
            d.Appeal = 0.12f; d.PriceFactor = 1.1f;
            d = Store(BuildingType.Bakery, "Bakery", 1, 1, 70000, 30, 160000, Good.Groceries, 50, 200, 250, 800, " Bakery", "Fresh bread and pastries every morning.", Role.Baker, 2);
            d.Appeal = 0.1f; d.PriceFactor = 1.08f;
            d = Store(BuildingType.FarmersMarket, "Farmers market", 2, 2, 60000, 30, 140000, Good.Groceries, 60, 500, 400, 2000, " Market Hall", "Stalls of local produce under a roof.", Role.SalesClerk, 2, Role.Cashier, 1);
            d.Appeal = 0.15f; d.PriceFactor = 1.05f;
            d = Store(BuildingType.FishMarket, "Fish market", 1, 1, 70000, 30, 160000, Good.Groceries, 40, 250, 700, 1800, " Fish", "Fresh fish on ice.", Role.Butcher, 1);
            d.Appeal = 0.08f; d.PriceFactor = 1.1f;

            // ---------------------------------------------------------------- places to eat
            Eat(BuildingType.Pizzeria, "Pizzeria", 1, 1, 80000, 35, 180000, 18, 90, 150, 600, " Pizza", "Pizza by the slice and the pie.", 0.1f, 0, Role.Cook, 2, Role.Waiter, 1);
            Eat(BuildingType.BurgerJoint, "Burger joint", 1, 1, 80000, 35, 180000, 14, 110, 150, 600, " Burgers", "Burgers and fries, quick and cheap.", 0.09f, -0.05f, Role.Cook, 2, Role.Cashier, 1);
            Eat(BuildingType.Diner, "Diner", 1, 1, 85000, 35, 190000, 17, 80, 120, 600, " Diner", "Breakfast all day and pie at the counter.", 0.1f, 0, Role.Cook, 2, Role.Waiter, 2);
            Eat(BuildingType.SushiBar, "Sushi bar", 1, 1, 100000, 40, 210000, 30, 60, 450, 1200, " Sushi", "Fresh sushi at the counter.", 0.16f, 0.12f, Role.Chef, 1, Role.Cook, 1, Role.Waiter, 1);
            Eat(BuildingType.TacoShop, "Taco shop", 1, 1, 70000, 30, 160000, 13, 110, 200, 700, " Tacos", "Tacos, burritos and salsa.", 0.08f, 0, Role.Cook, 2, Role.Cashier, 1);
            Eat(BuildingType.Steakhouse, "Steakhouse", 2, 1, 160000, 65, 340000, 48, 50, 600, 1600, " Steakhouse", "Steaks, wine and white tablecloths.", 0.28f, 0.18f, Role.Chef, 1, Role.Cook, 2, Role.Waiter, 3);
            Eat(BuildingType.NoodleBar, "Noodle bar", 1, 1, 80000, 35, 180000, 16, 90, 300, 900, " Noodles", "Ramen and noodle bowls.", 0.09f, 0.04f, Role.Cook, 2, Role.Waiter, 1);
            Eat(BuildingType.ChickenShack, "Fried chicken", 1, 1, 75000, 30, 170000, 13, 110, 200, 700, " Chicken", "Fried chicken by the bucket.", 0.09f, -0.05f, Role.Cook, 2, Role.Cashier, 1);
            Eat(BuildingType.SandwichShop, "Sandwich shop", 1, 1, 60000, 25, 150000, 12, 120, 150, 600, " Sandwiches", "Subs and sandwiches made to order.", 0.07f, 0, Role.Cook, 1, Role.Cashier, 1);
            Eat(BuildingType.FineDining, "Fine dining", 2, 1, 220000, 90, 420000, 75, 30, 900, 2500, " Table", "A tasting menu for special nights out. For those who can afford it.", 0.4f, 0.32f, Role.Chef, 2, Role.Cook, 2, Role.Waiter, 3);
            Eat(BuildingType.BBQJoint, "Barbecue", 2, 1, 130000, 55, 280000, 24, 70, 400, 1100, " BBQ", "Smoked brisket and ribs.", 0.18f, 0.05f, Role.Chef, 1, Role.Cook, 2, Role.Waiter, 2);
            Eat(BuildingType.SeafoodRestaurant, "Seafood restaurant", 2, 1, 150000, 60, 320000, 34, 55, 550, 1500, " Seafood", "Fish, shrimp and oysters.", 0.22f, 0.12f, Role.Chef, 1, Role.Cook, 2, Role.Waiter, 2);
            Eat(BuildingType.CurryHouse, "Curry house", 1, 1, 90000, 35, 190000, 21, 80, 350, 1000, " Curry House", "Curries, naan and tandoori.", 0.12f, 0.05f, Role.Chef, 1, Role.Cook, 1, Role.Waiter, 1);
            Eat(BuildingType.ThaiRestaurant, "Thai restaurant", 1, 1, 90000, 35, 190000, 20, 80, 350, 1000, " Thai Kitchen", "Pad thai and green curry.", 0.12f, 0.05f, Role.Chef, 1, Role.Cook, 1, Role.Waiter, 1);
            // coffee, tea and treats
            d = Place(BuildingType.TeaHouse, "Tea house", 1, 1, 60000, 25, 150000, Service.Coffee, 6, 200, 300, 1200, " Tea House", "Pots of tea and quiet tables.", Role.Barista, 2);
            d.In(Good.Groceries, 0.02f); d.Appeal = 0.1f; d.Floors = 2;
            d = Place(BuildingType.JuiceBar, "Juice bar", 1, 1, 55000, 25, 140000, Service.Coffee, 7, 200, 300, 1200, " Juice", "Smoothies and fresh juice.", Role.Barista, 2);
            d.In(Good.Groceries, 0.04f);
            d = Place(BuildingType.DonutShop, "Donut shop", 1, 1, 60000, 25, 150000, Service.Treats, 6, 220, 200, 800, " Donuts", "Donuts and coffee to go.", Role.Baker, 1, Role.Cashier, 1);
            d.In(Good.Groceries, 0.03f);
            d = Place(BuildingType.IceCreamParlor, "Ice cream parlor", 1, 1, 60000, 25, 150000, Service.Treats, 6, 220, 200, 800, " Ice Cream", "Cones and sundaes. Families come in summer.", Role.Cashier, 2);
            d.In(Good.Groceries, 0.03f);
            d = Place(BuildingType.CandyShop, "Candy shop", 1, 1, 55000, 25, 140000, Service.Treats, 7, 200, 300, 1200, " Sweets", "Jars of sweets and fudge.", Role.SalesClerk, 1);
            d.In(Good.Groceries, 0.03f);
            d = Place(BuildingType.BakeryCafe, "Bakery café", 1, 1, 75000, 30, 170000, Service.Treats, 8, 180, 250, 900, " Bakehouse", "Pastries, cakes and good coffee.", Role.Baker, 1, Role.Barista, 1);
            d.In(Good.Groceries, 0.04f); d.Appeal = 0.1f;
            // drinks and nights out
            d = Place(BuildingType.SportsBar, "Sports bar", 2, 1, 120000, 50, 260000, Service.Drinks, 15, 90, 300, 900, " Sports Bar", "The game on every screen, wings and beer.", Role.Bartender, 3, Role.Cook, 1);
            d.In(Good.Beverages, 0.6f).In(Good.Groceries, 0.04f);
            d = Place(BuildingType.WineBar, "Wine bar", 1, 1, 90000, 35, 190000, Service.Drinks, 22, 70, 450, 1500, " Wine Bar", "Wine by the glass.", Role.Bartender, 2);
            d.In(Good.Beverages, 0.7f); d.Appeal = 0.15f; d.Floors = 2;
            d = Place(BuildingType.Brewpub, "Brewpub", 2, 1, 140000, 55, 300000, Service.Drinks, 14, 90, 400, 1300, " Brewing Co.", "Beer brewed in the back, burgers in the front.", Role.Brewer, 1, Role.Bartender, 2, Role.Cook, 1);
            d.In(Good.Crops, 0.1f).In(Good.Beverages, 0.2f).In(Good.Groceries, 0.05f); d.Appeal = 0.06f;
            d = Place(BuildingType.CocktailLounge, "Cocktail lounge", 1, 1, 100000, 40, 210000, Service.Drinks, 20, 70, 500, 1600, " Lounge", "Cocktails in low light.", Role.Bartender, 2);
            d.In(Good.Beverages, 0.6f); d.Appeal = 0.18f; d.Floors = 2;
            d = Place(BuildingType.Nightclub, "Nightclub", 2, 1, 160000, 60, 330000, Service.Nightlife, 25, 150, 700, 2500, " Club", "A DJ and a dance floor till late. For the young.", Role.Bartender, 3, Role.Usher, 2);
            d.In(Good.Beverages, 0.9f);
            d = Place(BuildingType.KaraokeBar, "Karaoke bar", 1, 1, 90000, 35, 190000, Service.Nightlife, 18, 120, 700, 2200, " Karaoke", "Private rooms and a mic.", Role.Bartender, 2);
            d.In(Good.Beverages, 0.7f); d.Floors = 2;

            // ---------------------------------------------------------------- fitness, looks and fun
            Fun(BuildingType.YogaStudio, "Yoga studio", 1, 1, 70000, 30, 160000, Service.Fitness, 18, 90, 350, 1300, " Yoga", "Classes morning and evening.", 0.1f, Role.Instructor, 2);
            Fun(BuildingType.BoxingGym, "Boxing gym", 1, 1, 75000, 30, 170000, Service.Fitness, 16, 90, 400, 1500, " Boxing Club", "Bags, a ring and hard training.", 0, Role.Trainer, 2);
            Fun(BuildingType.ClimbingGym, "Climbing gym", 2, 1, 180000, 70, 360000, Service.Fitness, 20, 120, 700, 2500, " Climbing", "Walls to climb, indoors.", 0.05f, Role.Instructor, 2, Role.Receptionist, 1);
            Fun(BuildingType.SwimClub, "Swim club", 2, 2, 350000, 140, 700000, Service.Fitness, 12, 160, 800, 3000, " Swim Club", "An indoor pool for laps and lessons.", 0.05f, Role.Instructor, 2, Role.Receptionist, 1, Role.Janitor, 1);
            Fun(BuildingType.MartialArtsDojo, "Martial arts dojo", 1, 1, 70000, 30, 160000, Service.Fitness, 15, 90, 400, 1500, " Dojo", "Karate and judo for kids and adults.", 0, Role.Instructor, 2);
            Fun(BuildingType.Barbershop, "Barbershop", 1, 1, 50000, 25, 130000, Service.Haircut, 28, 35, 150, 500, " Barbers", "Quick cuts and shaves.", -0.05f, Role.Hairdresser, 2);
            d = Fun(BuildingType.NailSalon, "Nail salon", 1, 1, 55000, 25, 140000, Service.Beauty, 40, 35, 300, 900, " Nails", "Manicures and pedicures.", 0, Role.Beautician, 2);
            d.In(Good.Toiletries, 0.3f);
            d = Fun(BuildingType.DaySpa, "Day spa", 2, 1, 150000, 60, 320000, Service.Beauty, 85, 20, 600, 2000, " Spa", "Massages, facials and a sauna. For those who can afford it.", 0.22f, Role.Beautician, 3, Role.Receptionist, 1);
            d.In(Good.Toiletries, 0.8f);
            Fun(BuildingType.TattooParlor, "Tattoo parlor", 1, 1, 55000, 25, 140000, Service.Beauty, 120, 12, 400, 1500, " Tattoo", "Ink, by appointment.", 0, Role.TattooArtist, 2);
            Fun(BuildingType.TanningSalon, "Tanning salon", 1, 1, 55000, 25, 140000, Service.Beauty, 25, 80, 350, 1300, " Tan", "A tan in midwinter.", 0, Role.Beautician, 1);
            d = Fun(BuildingType.Cinema, "Cinema", 2, 2, 450000, 170, 850000, Service.Movies, 13, 300, 600, 3000, " Cinema", "Four screens and popcorn.", 0.05f, Role.Usher, 3, Role.Cashier, 1, Role.Technician, 1);
            d.In(Good.Groceries, 0.03f); d.Branded = true;
            d = Fun(BuildingType.BowlingAlley, "Bowling alley", 2, 2, 380000, 150, 750000, Service.Bowling, 16, 130, 700, 3000, " Lanes", "Twelve lanes, a bar and a grill.", 0, Role.Usher, 2, Role.Cook, 1, Role.Bartender, 1);
            d.In(Good.Groceries, 0.03f).In(Good.Beverages, 0.2f);
            Fun(BuildingType.Arcade, "Arcade", 1, 1, 90000, 35, 190000, Service.Games, 12, 200, 400, 1500, " Arcade", "Video games, pinball and air hockey.", 0, Role.Usher, 2);
            d = Fun(BuildingType.PoolHall, "Pool hall", 1, 1, 80000, 35, 180000, Service.Games, 10, 160, 400, 1500, " Billiards", "Pool tables and cold beer.", 0, Role.Bartender, 1, Role.Usher, 1);
            d.In(Good.Beverages, 0.3f);
            Fun(BuildingType.EscapeRoom, "Escape room", 1, 1, 70000, 30, 160000, Service.Games, 28, 60, 500, 2000, " Escape", "An hour to solve your way out.", 0.05f, Role.Usher, 1, Role.Performer, 1);
            Fun(BuildingType.MiniGolf, "Mini golf", 2, 2, 120000, 50, 220000, Service.Games, 12, 200, 500, 2500, " Mini Golf", "Eighteen holes of windmills and loops.", 0, Role.Usher, 2);
            Fun(BuildingType.SkatingRink, "Skating rink", 2, 2, 300000, 120, 600000, Service.Games, 11, 220, 700, 3000, " Rink", "Ice in winter, roller skates the rest of the year.", 0, Role.Usher, 2, Role.Janitor, 1);
            d = Fun(BuildingType.Theater, "Theater", 2, 2, 600000, 220, 1100000, Service.Shows, 40, 120, 900, 4000, " Playhouse", "Plays and musicals on a real stage.", 0.15f, Role.Performer, 4, Role.Usher, 2);
            d.Floors = 2;
            d = Fun(BuildingType.ComedyClub, "Comedy club", 1, 1, 90000, 35, 190000, Service.Shows, 28, 120, 600, 2500, " Comedy", "Stand-up every weekend.", 0, Role.Performer, 2, Role.Bartender, 1);
            d.In(Good.Beverages, 0.4f);

            // ---------------------------------------------------------------- health, household and family
            Fun(BuildingType.Dentist, "Dentist", 1, 1, 150000, 60, 300000, Service.Dental, 140, 30, 400, 1500, " Dental", "Checkups and fillings. Needs a dentist.", 0, Role.Dentist, 1, Role.DentalHygienist, 2, Role.Receptionist, 1);
            Fun(BuildingType.Optician, "Optician", 1, 1, 90000, 35, 190000, Service.EyeCare, 110, 30, 500, 2000, " Eyecare", "Eye tests and glasses.", 0, Role.Optometrist, 1, Role.SalesClerk, 1);
            d = Fun(BuildingType.VetClinic, "Vet clinic", 1, 1, 140000, 55, 280000, Service.VetCare, 90, 30, 400, 1500, " Animal Hospital", "Care for the town's pets.", 0, Role.Veterinarian, 1, Role.VetTech, 2);
            d.In(Good.Medicine, 0.2f);
            Fun(BuildingType.PhysioClinic, "Physiotherapy clinic", 1, 1, 100000, 40, 210000, Service.Physio, 80, 35, 500, 2000, " Physio", "Bad backs and bad knees, mostly older people.", 0, Role.Physiotherapist, 2, Role.Receptionist, 1);
            d = Fun(BuildingType.Laundromat, "Laundromat", 1, 1, 90000, 35, 190000, Service.Laundry, 12, 150, 200, 800, " Laundromat", "Rows of washers for those without their own: mostly people in flats.", 0, Role.Attendant, 1);
            d.In(Good.Chemicals, 0.02f);
            d = Fun(BuildingType.DryCleaner, "Dry cleaner", 1, 1, 80000, 35, 180000, Service.Laundry, 18, 80, 350, 1300, " Cleaners", "Suits and shirts, cleaned and pressed.", 0.08f, Role.Tailor, 1, Role.Attendant, 1);
            d.In(Good.Chemicals, 0.03f);
            d = Fun(BuildingType.Tailor, "Tailor", 1, 1, 45000, 20, 120000, Service.Repairs, 35, 40, 600, 1800, " Alterations", "Hems, zips and alterations.", 0, Role.Tailor, 1);
            d.In(Good.Textiles, 0.1f);
            d = Fun(BuildingType.PhoneRepair, "Phone repair", 1, 1, 45000, 20, 120000, Service.Repairs, 60, 30, 300, 1200, " Repair", "Cracked screens and dead batteries.", 0, Role.Technician, 1);
            d.In(Good.Parts, 0.3f);
            d = Fun(BuildingType.CarWash, "Car wash", 2, 1, 160000, 60, 300000, Service.CarWash, 14, 150, 300, 1200, " Car Wash", "Drive through, brushes and wax.", 0, Role.Attendant, 3);
            d.In(Good.Chemicals, 0.02f);
            d = Fun(BuildingType.Daycare, "Daycare", 2, 1, 160000, 60, 320000, Service.Childcare, 180, 5, 300, 900, " Daycare", "Looks after small children while their parents work.", 0, Role.CareWorker, 6, Role.Receptionist, 1);
            d.In(Good.Groceries, 0.3f);
            Fun(BuildingType.MusicSchool, "Music school", 1, 1, 80000, 35, 180000, Service.Lessons, 35, 40, 450, 1800, " Music School", "Piano, guitar and violin lessons for kids.", 0.05f, Role.Instructor, 2);
            Fun(BuildingType.DanceStudio, "Dance studio", 1, 1, 70000, 30, 160000, Service.Lessons, 30, 50, 400, 1500, " Dance", "Ballet, tap and hip hop.", 0, Role.Instructor, 1);
            Fun(BuildingType.TutoringCenter, "Tutoring center", 1, 1, 60000, 25, 150000, Service.Lessons, 40, 40, 400, 1500, " Learning", "Extra help with math and reading.", 0, Role.Instructor, 1);
            d = Fun(BuildingType.DrivingSchool, "Driving school", 1, 1, 60000, 25, 150000, Service.Lessons, 60, 30, 450, 2000, " Driving School", "Teaches teenagers to drive.", 0, Role.Instructor, 2);
            d.In(Good.Fuel, 0.2f);

            // ---------------------------------------------------------------- offices
            Firm(BuildingType.LawFirm, "Law firm", 1, 1, 180000, 70, 400000, 2, " Law", "Lawyers for the town: house sales, new businesses, wills and disputes, contracts for the bigger firms. Needs graduates.", Role.Lawyer, 2, Role.Paralegal, 2, Role.Receptionist, 1);
            Firm(BuildingType.AccountingFirm, "Accounting firm", 1, 1, 160000, 60, 360000, 2, " & Partners", "Does the books for the town's businesses and families' tax returns. Needs graduates.", Role.Accountant, 3, Role.OfficeClerk, 2);
            Firm(BuildingType.InsuranceAgency, "Insurance agency", 1, 1, 120000, 50, 260000, 2, " Insurance", "Sells the town's families and businesses their insurance, on commission.", Role.Agent, 3, Role.OfficeClerk, 1);
            Firm(BuildingType.RealEstateAgency, "Real estate agency", 1, 1, 120000, 50, 260000, 2, " Realtors", "Agents taking a cut of every house sold in town, and letting homes for families and the property firms.", Role.Agent, 3, Role.Receptionist, 1);
            Firm(BuildingType.ArchitectureFirm, "Architecture firm", 1, 1, 170000, 65, 380000, 2, " Architects", "Designs what gets built in town, for a share of each job: busy in a building boom, quiet after. Needs graduates.", Role.Architect, 3, Role.Designer, 1);
            Firm(BuildingType.MarketingAgency, "Marketing agency", 1, 1, 160000, 60, 360000, 2, " Media", "Ads for the town's shops, restaurants and firms.", Role.Designer, 3, Role.OfficeClerk, 1, Role.Developer, 1);
            Firm(BuildingType.SoftwareStudio, "Software studio", 2, 1, 260000, 100, 560000, 3, " Labs", "IT and software for the town's businesses and the town hall, and a few contracts from out of town. Needs graduates.", Role.Developer, 5, Role.Designer, 1);
            Firm(BuildingType.CallCenter, "Call center", 2, 1, 240000, 90, 500000, 2, " Contact Center", "Answers the phones for the bigger businesses here, and for some companies out of town. Work for people without a diploma.", Role.CallAgent, 10, Role.OfficeClerk, 1);
            Firm(BuildingType.EngineeringFirm, "Engineering firm", 2, 1, 260000, 100, 560000, 3, " Engineering", "Engineering for the plants, the roads and the public buildings.", Role.Engineer, 4, Role.Technician, 2, Role.Designer, 1);
            Firm(BuildingType.DesignStudio, "Design studio", 1, 1, 120000, 50, 260000, 2, " Design", "Signs and logos for new businesses, packaging for the plants.", Role.Designer, 3);

            // ---------------------------------------------------------------- visitors and the rest
            d = Fun(BuildingType.Motel, "Motel", 2, 1, 220000, 90, 420000, Service.Lodging, 75, 25, 250, 1500, " Motel", "Rooms off the highway for people passing through and visiting the town's businesses.", -0.05f, Role.Housekeeper, 2, Role.Receptionist, 1);
            d.In(Good.Toiletries, 0.5f);
            d = Fun(BuildingType.Hotel, "Hotel", 2, 2, 1100000, 400, 2000000, Service.Lodging, 140, 25, 900, 4000, " Hotel", "Six floors of rooms, a restaurant and a lobby bar.", 0.2f, Role.StoreManager, 1, Role.Housekeeper, 4, Role.Receptionist, 2, Role.Cook, 1);
            d.In(Good.Toiletries, 0.5f).In(Good.Groceries, 0.15f); d.Floors = 6; d.Branded = true;
            d = Fun(BuildingType.BedAndBreakfast, "Bed and breakfast", 1, 1, 120000, 45, 240000, Service.Lodging, 110, 20, 600, 2000, " Inn", "A few cosy rooms and breakfast.", 0.15f, Role.Housekeeper, 1, Role.Cook, 1);
            d.In(Good.Toiletries, 0.4f).In(Good.Groceries, 0.12f); d.Floors = 2;
            Fun(BuildingType.FuneralHome, "Funeral home", 1, 1, 150000, 60, 300000, Service.Funeral, 8500, 2, 1500, 4000, " Funeral Home", "Funerals for the town's dead. Families pay, here or in the next town.", 0, Role.Mortician, 1);
            // ---------------------------------------------------------------- the grocery chains' head offices
            HeadOffice(BuildingType.ValleyValueOffice, BuildingType.DiscountStore);
            HeadOffice(BuildingType.RedwoodOffice, BuildingType.Supermarket);
            HeadOffice(BuildingType.BulkworksOffice, BuildingType.WarehouseClub);

            d = Fun(BuildingType.TaxiCompany, "Taxi company", 1, 1, 80000, 30, 170000, Service.Rides, 20, 60, 400, 1500, " Taxi", "Rides for people without a car.", 0, Role.Driver, 3);
            d.In(Good.Fuel, 0.25f);
        }

        static BuildingDef Eat(BuildingType t, string name, int w, int h, double materials, float work, double value, double price, float output, int minPop, int popPer, string suffix, string blurb, float food, float appeal, params object[] jobs)
        {
            var d = Place(t, name, w, h, materials, work, value, Service.Meals, price, output, minPop, popPer, suffix, blurb, jobs);
            d.In(Good.Groceries, food); d.Appeal = appeal;
            return d;
        }

        /// <summary>A grocery chain's head office: the company that owns its stores.</summary>
        static void HeadOffice(BuildingType t, BuildingType chain)
        {
            var c = defs[chain];
            var d = D(t, c.Chain + " head office", 1, 1, Category.Commercial, Zone.Commercial, 220000, 80,
                      "The company behind " + c.Chain + ": it owns the stores and takes their profits, does their books and their ads, and decides when the town needs another one and where it should go.");
            d.Jobs = J(Role.Executive, 1, Role.Accountant, 2, Role.OfficeClerk, 2, Role.Designer, 1);
            d.Value = 480000; d.Floors = 3; d.HeadOf = chain; d.Branded = true;
            c.Office = t;
        }

        static BuildingDef Fun(BuildingType t, string name, int w, int h, double materials, float work, double value, Service s, double price, float output, int minPop, int popPer, string suffix, string blurb, float appeal, params object[] jobs)
        {
            var d = Place(t, name, w, h, materials, work, value, s, price, output, minPop, popPer, suffix, blurb, jobs);
            d.Appeal = appeal;
            return d;
        }

        // ================================================================== services
        static ServiceDef S(Service s, string name, string unit, ServiceKind kind, BuildingType venue)
        {
            var d = new ServiceDef { Service = s, Name = name, Unit = unit, Kind = kind, Venue = venue };
            services[s] = d;
            return d;
        }

        static void Services()
        {
            ServiceDef s;
            // going out: shares of the household's spending money (normalised over what the town has)
            s = S(Service.Meals, "Meals", "meal", ServiceKind.Leisure, BuildingType.Restaurant); s.Share = 0.30f; s.MaxPerDay = 2; s.MinAge = 6;
            s = S(Service.Coffee, "Coffee", "coffee", ServiceKind.Leisure, BuildingType.Cafe); s.Share = 0.08f; s.MaxPerDay = 3;
            s = S(Service.Treats, "Treats", "treat", ServiceKind.Leisure, BuildingType.DonutShop); s.Share = 0.05f; s.MaxPerDay = 2; s.MinAge = 4;
            s = S(Service.Drinks, "Drinks", "drink", ServiceKind.Leisure, BuildingType.Bar); s.Share = 0.10f; s.MaxPerDay = 2; s.MinAge = 21;
            s = S(Service.Fitness, "Workouts", "visit", ServiceKind.Leisure, BuildingType.Gym); s.Share = 0.07f; s.MaxAge = 75; s.Health = 1.5f;
            s = S(Service.Movies, "Movies", "ticket", ServiceKind.Leisure, BuildingType.Cinema); s.Share = 0.05f; s.MinAge = 6;
            s = S(Service.Bowling, "Bowling", "game", ServiceKind.Leisure, BuildingType.BowlingAlley); s.Share = 0.03f; s.MinAge = 8;
            s = S(Service.Games, "Games", "visit", ServiceKind.Leisure, BuildingType.Arcade); s.Share = 0.04f; s.MinAge = 8; s.MaxAge = 55;
            s = S(Service.Shows, "Shows", "ticket", ServiceKind.Leisure, BuildingType.Theater); s.Share = 0.03f; s.MinAge = 12;
            s = S(Service.Nightlife, "Nights out", "entry", ServiceKind.Leisure, BuildingType.Nightclub); s.Share = 0.04f; s.MinAge = 21; s.MaxAge = 45;
            s = S(Service.Beauty, "Beauty", "visit", ServiceKind.Leisure, BuildingType.NailSalon); s.Share = 0.03f; s.MinAge = 16;
            // now and then: a chance a day (a sim day is about a week)
            s = S(Service.Haircut, "Haircuts", "haircut", ServiceKind.Routine, BuildingType.HairSalon); s.Chance = 0.16f; s.FromFun = true;
            s = S(Service.CarRepair, "Repairs", "job", ServiceKind.Special, BuildingType.AutoRepair);
            s = S(Service.Dental, "Dental care", "visit", ServiceKind.Routine, BuildingType.Dentist); s.Chance = 0.02f; s.MinAge = 3; s.Health = 2;
            s = S(Service.EyeCare, "Eye care", "visit", ServiceKind.Routine, BuildingType.Optician); s.Chance = 0.01f; s.MinAge = 8;
            s = S(Service.VetCare, "Vet care", "visit", ServiceKind.Routine, BuildingType.VetClinic); s.Chance = 0.06f; s.Who = Who.PetOwners;
            s = S(Service.Laundry, "Laundry", "load", ServiceKind.Routine, BuildingType.Laundromat); s.Chance = 0.5f; s.Who = Who.Flat;
            s = S(Service.Repairs, "Fix-ups", "job", ServiceKind.Routine, BuildingType.PhoneRepair); s.Chance = 0.05f; s.Who = Who.Household; s.FromFun = true;
            s = S(Service.CarWash, "Car washes", "wash", ServiceKind.Routine, BuildingType.CarWash); s.Chance = 0.15f; s.Who = Who.Drivers; s.FromFun = true;
            s = S(Service.Physio, "Physiotherapy", "session", ServiceKind.Routine, BuildingType.PhysioClinic); s.Chance = 0.015f; s.MinAge = 40; s.Health = 3;
            s = S(Service.Lessons, "Lessons", "lesson", ServiceKind.Routine, BuildingType.MusicSchool); s.Chance = 0.25f; s.Who = Who.Students; s.MinAge = 6; s.MaxAge = 17; s.FromFun = true;
            s = S(Service.Childcare, "Childcare", "week", ServiceKind.Routine, BuildingType.Daycare); s.Chance = 1f; s.Who = Who.Toddlers;
            s = S(Service.Rides, "Taxi rides", "ride", ServiceKind.Routine, BuildingType.TaxiCompany); s.Chance = 0.06f; s.Who = Who.Carless;
            s = S(Service.Lodging, "Hotel stays", "night", ServiceKind.Special, BuildingType.Motel);
            s = S(Service.Funeral, "Funerals", "funeral", ServiceKind.Special, BuildingType.FuneralHome);

            // the markets: each good sold over the counter, each service, and the kinds of place in them (the chains have their own rules)
            Markets = new List<Market>();
            foreach (var g in Goods)
            {
                if (g.Retail <= 0 || g.Good == Good.Cars) continue;
                var kinds = All.Where(t => defs.ContainsKey(t) && defs[t].Sells == g.Good && defs[t].Chain == null).ToArray();
                if (kinds.Length > 0) Markets.Add(new Market { Good = g.Good, Primary = g.Store, Kinds = kinds });
            }
            foreach (var sd in services.Values)
            {
                if (sd.Service == Service.CarRepair) continue;
                var kinds = All.Where(t => defs.ContainsKey(t) && defs[t].Serves == sd.Service).ToArray();
                if (kinds.Length > 0) Markets.Add(new Market { Service = sd.Service, Primary = sd.Venue, Kinds = kinds });
            }
        }

        /// <summary>The market a kind of shop or service is in (null for anything else).</summary>
        public static Market MarketOf(BuildingType t)
        {
            foreach (var m in Markets) if (Array.IndexOf(m.Kinds, t) >= 0) return m;
            return null;
        }
    }
}
