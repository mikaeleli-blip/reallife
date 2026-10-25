using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// A new game: empty land, a river and woods, and the state highway coming in from the west. Everything else is yours to lay
    /// out. (CreateDeveloped makes the old ready-built town of about 150 people, which the headless tests still use.)
    /// </summary>
    public partial class Town
    {
        public const int MapSize = 96;
        public int StartX = -1, StartY = -1;   // where the highway ends: where an empty town starts

        public static Town CreateNew(int seed)
        {
            var t = new Town { Seed = seed, Cols = MapSize, Rows = MapSize };
            t.RngState = unchecked((int)((uint)seed * 2654435761u | 1u));
            for (int i = 0; i < 10; i++) t.Rand();
            t.Tiles = new int[t.Cols * t.Rows];
            t.Name = t.Pick(Names.Towns);
            t.BankName = t.Pick(Names.Firms) + " Bank";
            t.Index();
            t.Terraform();
            t.Highway(16);
            t.Index();
            t.Treasury = 5000000;
            t.BankCapital = 9000000;
            t.Hour = 7f;
            t.AddLog("Welcome to " + t.Name + ". The land is empty: run streets off the end of the highway and zone land for homes. " +
                     "Families will move in and drive to work out of town until there are jobs here; zone shops, industry and farms, " +
                     "and build the town hall, schools and services as it grows.", LogKind.Info);
            t.RecordHistory();
            return t;
        }

        /// <summary>The state highway in from the west edge, ending where the town begins.</summary>
        void Highway(int length)
        {
            Road(0, 46, length, 46);
            var term = AddBuilding(BuildingType.FreightTerminal, 1, 47, 2, true);
            term.Facing = 2; term.Owner = Owner.Outside; term.OwnerName = "State highway";
            TerminalId = term.Id;
            StartX = length; StartY = 46;
            RebuildRoads();
        }

        /// <summary>The ready-built starting town of earlier versions: streets, homes, a downtown and industry, and the people
        /// living there, with a year of history behind them.</summary>
        public static Town CreateDeveloped(int seed)
        {
            var t = new Town { Seed = seed, Cols = MapSize, Rows = MapSize };
            t.RngState = unchecked((int)((uint)seed * 2654435761u | 1u));
            for (int i = 0; i < 10; i++) t.Rand();
            t.Tiles = new int[t.Cols * t.Rows];
            t.Name = t.Pick(Names.Towns);
            t.BankName = t.Pick(Names.Firms) + " Bank";
            t.Index();
            t.Terraform();
            t.LayOutTown();
            t.Index();
            t.Populate();
            t.Treasury = 3000000;
            t.BankCapital = 9000000;
            // a year of history before you arrive, so the town's economy has settled
            for (int d = 0; d < Balance.DaysPerYear; d++) t.Advance(1f);
            t.Log.Clear(); t.History.Clear();
            t.Hour = 7f;
            t.AddLog("Welcome to " + t.Name + ". " + t.People.Count + " people live here. You can't run their lives: lay out the streets, zone the land, build schools and services, and set the laws.", LogKind.Info);
            t.RecordHistory();
            return t;
        }

        // ================================================================== land
        float[] Noise(int cell)
        {
            int gw = Cols / cell + 2, gh = Rows / cell + 2;
            var g = new float[gw * gh];
            for (int i = 0; i < g.Length; i++) g[i] = Rand();
            var n = new float[Cols * Rows];
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    float fx = (float)x / cell, fy = (float)y / cell;
                    int ix = (int)fx, iy = (int)fy; float ux = fx - ix, uy = fy - iy;
                    ux = ux * ux * (3 - 2 * ux); uy = uy * uy * (3 - 2 * uy);
                    float a = g[iy * gw + ix], b = g[iy * gw + ix + 1], c = g[(iy + 1) * gw + ix], d = g[(iy + 1) * gw + ix + 1];
                    n[y * Cols + x] = (a * (1 - ux) + b * ux) * (1 - uy) + (c * (1 - ux) + d * ux) * uy;
                }
            return n;
        }

        void Terraform()
        {
            var big = Noise(14); var small = Noise(5);
            // a river down the east side
            float phase = Rand() * 6.28f;
            for (int y = 0; y < Rows; y++)
            {
                float cx = 84 + 4.5f * (float)Math.Sin(y * 0.09f + phase) + 1.5f * (float)Math.Sin(y * 0.23f);
                for (int x = 0; x < Cols; x++)
                {
                    float dist = Math.Abs(x - cx);
                    if (dist < 2.2f) SetTerrain(x, y, TileBits.Water);
                    else if (dist < 3.2f) SetTerrain(x, y, TileBits.Sand);
                }
            }
            // a pond in the north-west
            int px = 16 + RandInt(0, 8), py = 74 + RandInt(0, 8);
            for (int y = py - 6; y <= py + 6; y++)
                for (int x = px - 8; x <= px + 8; x++)
                {
                    if (!InBounds(x, y)) continue;
                    float dx = (x - px) / 7.5f, dy = (y - py) / 5f, r = dx * dx + dy * dy + small[y * Cols + x] * 0.35f;
                    if (r < 0.75f) SetTerrain(x, y, TileBits.Water);
                    else if (r < 1.0f && !IsWater(x, y)) SetTerrain(x, y, TileBits.Sand);
                }
            // woods
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Cols; x++)
                {
                    if (Terrain(x, y) != TileBits.Grass) continue;
                    float n = big[y * Cols + x] * 0.7f + small[y * Cols + x] * 0.3f;
                    float town = Math.Max(Math.Abs(x - 48), Math.Abs(y - 48));
                    float keepClear = town < 30 ? 0.2f : 0f;
                    if (n > 0.62f + keepClear || Chance(0.015f)) SetTerrain(x, y, TileBits.Trees);
                }
        }

        void Road(int x0, int y0, int x1, int y1)
        {
            int dx = Math.Sign(x1 - x0), dy = Math.Sign(y1 - y0);
            int x = x0, y = y0;
            while (true)
            {
                if (InBounds(x, y) && !IsWater(x, y)) { SetRoadBit(x, y, true); if (HasTrees(x, y)) SetTerrain(x, y, TileBits.Grass); }
                if (x == x1 && y == y1) break;
                if (x != x1) x += dx; else y += dy;
            }
        }

        void ZoneRect(int x0, int y0, int x1, int y1, Zone z)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (InBounds(x, y) && !IsWater(x, y) && !IsRoad(x, y)) SetZoneBits(x, y, z);
        }

        Building Starter(BuildingType t, int x, int y)
        {
            var d = Defs.Get(t);
            if (!FootprintFree(x, y, d.W, d.H)) return null;
            int f = FindFacing(x, y, d.W, d.H);
            if (f < 0) return null;
            var b = AddBuilding(t, x, y, f, true);
            b.BuiltDay = -RandInt(200, 2000);
            b.Value = d.Value * (0.85f + Rand() * 0.25f);
            return b;
        }

        /// <summary>Puts a starting building on the most central free lot of its zone.</summary>
        Building StarterLot(BuildingType t)
        {
            int x, y;
            if (!FindLot(t, out x, out y)) return null;
            return Starter(t, x, y);
        }

        void LayOutTown()
        {
            int[] vx = { 38, 44, 50, 56 }, hy = { 40, 46, 52, 58 };
            foreach (int x in vx) Road(x, hy[0], x, hy[3]);
            foreach (int y in hy) Road(vx[0], y, vx[3], y);
            // the highway in from the west
            Highway(38);
            // the industrial strip to the east, the farm road west, and a street of homes to the south
            Road(56, 40, 72, 40); Road(56, 52, 72, 52); Road(72, 40, 72, 64); Road(56, 64, 72, 64); Road(64, 52, 64, 64);
            Road(14, 58, 38, 58); Road(26, 58, 26, 70);
            Road(38, 58, 38, 64); Road(38, 64, 56, 64); Road(56, 58, 56, 64);
            RebuildRoads();

            ZoneRect(39, 41, 43, 45, Zone.Residential); ZoneRect(45, 41, 49, 45, Zone.Residential); ZoneRect(51, 41, 55, 45, Zone.Residential);
            ZoneRect(39, 47, 43, 51, Zone.Residential); ZoneRect(45, 47, 49, 51, Zone.Commercial); ZoneRect(51, 47, 55, 51, Zone.Commercial);
            ZoneRect(39, 53, 43, 57, Zone.Residential); ZoneRect(45, 53, 49, 57, Zone.Commercial); ZoneRect(51, 53, 55, 57, Zone.Residential);
            ZoneRect(39, 59, 55, 63, Zone.Residential);
            ZoneRect(57, 41, 71, 51, Zone.Industrial); ZoneRect(57, 53, 63, 63, Zone.Industrial); ZoneRect(65, 53, 71, 63, Zone.Industrial);
            // farmland along the farm road, well away from the factories
            ZoneRect(15, 59, 25, 63, Zone.Farmland); ZoneRect(27, 59, 37, 63, Zone.Farmland);
            ZoneRect(20, 64, 25, 70, Zone.Farmland); ZoneRect(27, 64, 32, 70, Zone.Farmland);

            // public buildings
            Starter(BuildingType.TownHall, 45, 47);
            Starter(BuildingType.Elementary, 39, 53);
            Starter(BuildingType.Clinic, 51, 41);
            Starter(BuildingType.Police, 45, 41);
            foreach (var p in Buildings) if (p.Def.Cat == Category.Public) { var d = p.Def; for (int y = p.Y; y < p.Y + d.H; y++) for (int x = p.X; x < p.X + d.W; x++) SetZoneBits(x, y, Zone.None); }
            // downtown
            foreach (var t in new[] { BuildingType.Office, BuildingType.Office, BuildingType.GroceryStore, BuildingType.Bank, BuildingType.Restaurant, BuildingType.Gym,
                                      BuildingType.GasStation, BuildingType.Cafe, BuildingType.Bar, BuildingType.ClothingStore, BuildingType.AutoRepair, BuildingType.HairSalon,
                                      BuildingType.Realty, BuildingType.Realty })
                StarterLot(t);
            // industry
            foreach (var t in new[] { BuildingType.FoodPlant, BuildingType.GarmentFactory, BuildingType.FurnitureWorkshop, BuildingType.Construction, BuildingType.Trucking })
                StarterLot(t);
            Starter(BuildingType.Farm, 15, 59);
            Starter(BuildingType.Farm, 27, 59);
            // homes
            for (int i = 0; i < 4; i++) StarterLot(BuildingType.Apartments);
            for (int y = 40; y <= 64; y++)
                for (int x = 38; x <= 56; x++)
                    if (ZoneAt(x, y) == Zone.Residential && FootprintFree(x, y, 1, 1) && FindFacing(x, y, 1, 1) >= 0 && Chance(0.85f)) Starter(BuildingType.House, x, y);
        }

        // ================================================================== people
        void Populate()
        {
            var homes = new List<Building>();
            foreach (var b in Buildings) if (b.Def.Homes > 0) for (int u = 0; u < b.Def.Homes; u++) homes.Add(b);
            // the jobs that need filling decide what the adults studied
            var needs = new List<Edu>();
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (d.Cat == Category.Special) continue;
                foreach (var j in d.Jobs)
                {
                    int n = StartStaff(b, j);
                    for (int k = 0; k < n; k++) needs.Add(Defs.Get(j.Role).Needs);
                }
            }
            int jobs = needs.Count;
            Shuffle(needs);
            int ni = 0;
            Func<Edu> nextEdu = () =>
            {
                if (ni < needs.Count) return needs[ni++];
                float r = Rand(); return r < 0.18f ? Edu.None : r < 0.62f ? Edu.HighSchool : r < 0.78f ? Edu.Trade : Edu.Degree;
            };
            Shuffle(homes);
            int workers = 0;
            foreach (var home in homes)
            {
                // enough people for the jobs, and a few looking
                if (workers >= jobs * 1.06f) break;
                bool retired = Chance(0.15f);
                string last = Pick(Names.Last);
                var h = NewHousehold(last);
                if (retired)
                {
                    var a = NewPerson(last, Chance(0.5f), 67 + Rand() * 18, Chance(0.5f) ? Edu.HighSchool : Edu.None);
                    a.Retired = true; Join(h, a);
                    if (Chance(0.55f)) { var b = NewPerson(last, !a.Female, Math.Max(66, a.Age + Gauss(0, 3)), Edu.HighSchool); b.Retired = true; Join(h, b); Marry(a, b); }
                    h.Cash = 30000 + Rand() * 140000;
                }
                else
                {
                    bool single = home.Type == BuildingType.Apartments ? Chance(0.55f) : Chance(0.18f);
                    float age = 22 + Rand() * 40;
                    var a = NewPerson(last, Chance(0.5f), age, nextEdu());
                    Join(h, a); workers++;
                    Citizen b = null;
                    if (!single)
                    {
                        b = NewPerson(last, !a.Female, Math.Max(20, age + Gauss(0, 3)), nextEdu());
                        Join(h, b); Marry(a, b); workers++;
                        int kids = home.Type == BuildingType.Apartments ? RandInt(0, 2) : RandInt(0, 4);
                        for (int k = 0; k < kids; k++)
                        {
                            float mum = a.Female ? a.Age : b.Age;
                            float ka = Math.Min(mum - 20, Rand() * 17.9f);
                            if (ka < 0) continue;
                            var c = NewPerson(last, Chance(0.5f), ka, Edu.None);
                            c.MotherId = a.Female ? a.Id : b.Id; c.FatherId = a.Female ? b.Id : a.Id;
                            Join(h, c);
                            c.Elementary = Math.Min(1f, Math.Max(0, (ka - Balance.SchoolAge) / Balance.YearsElementary));
                            if (ka >= Balance.HighSchoolAge) c.StageYears = ka - Balance.HighSchoolAge;
                        }
                    }
                    h.Cash = 4000 + Rand() * 26000 + (a.Age - 20) * 700;
                }
                bool owns = home.Type == BuildingType.House && Chance(0.66f);
                MoveIn(h, home, owns);
                if (owns && (!retired || Chance(0.2f)))
                {
                    double left = home.Value * (0.35 + Rand() * 0.5);
                    NewLoan(LoanKind.Mortgage, h, home, left, Balance.MortgageRate, (int)(Balance.MortgageMonths * (0.4 + Rand() * 0.6)));
                }
                h.Pantry = h.Members.Count * 2;
                h.Cars = Chance(0.82f) ? (h.Members.Count(m => !GetCitizen(m).IsChild) > 1 && Chance(0.4f) ? 2 : 1) : 0;
                h.Fuel = h.Cars * 1.5f;
                h.Clothes = 0.4f + Rand() * 0.5f; h.Electronics = 0.3f + Rand() * 0.6f; h.Furniture = 0.3f + Rand() * 0.6f; h.CarWear = 0.3f + Rand() * 0.6f;
            }
            foreach (var c in People) { c.ArrivedDay = -RandInt(48, 1500); c.Experience = Math.Max(0, c.Age - 21) * (0.6f + Rand() * 0.4f); }
            // businesses: every one belongs to a family here. Someone who can work there runs it; the bigger firms go to families
            // with some money behind them
            foreach (var b in Buildings)
            {
                if (!IsBusiness(b)) continue;
                bool big = BigFirm(b.Type);
                var owner = People.Where(c => c.InLaborForce && c.WorkId < 0 && !c.Owner && c.Age > 26 && c.Age < 62 && QualifiesFor(c, b) != Role.None &&
                                              !Buildings.Any(o => o.Owner == Owner.Household && o.OwnerId == c.HouseholdId && IsBusiness(o)))
                                  .OrderByDescending(c => (big ? HouseholdOf(c).Cash : 0) + Defs.Get(QualifiesFor(c, b)).Wage * 10 + Rand() * 20000).FirstOrDefault();
                if (owner != null)
                {
                    var h = HouseholdOf(owner);
                    FamilyOwns(b, h);
                    owner.Owner = true;
                    Hire(b, owner, QualifiesFor(owner, b), true);
                    if (big) h.Cash += 40000 + Rand() * 80000;   // a family behind a bigger firm has done well out of it
                }
                else
                {
                    var h = Households.Where(x => !Buildings.Any(o => o.Owner == Owner.Household && o.OwnerId == x.Id && IsBusiness(o))).OrderByDescending(x => x.Cash).FirstOrDefault();
                    if (h != null) FamilyOwns(b, h); else BankTakes(b);
                }
                b.Brand = BrandFor(b);
            }
            // landlords: rented homes belong to the property firms here, mortgaged as such things are
            var firms = Buildings.Where(b => b.Type == BuildingType.Realty).ToList();
            int fi = 0;
            foreach (var b in Buildings)
            {
                if (b.Def.Homes == 0) continue;
                bool ownerOccupied = b.Households.Count > 0 && GetHousehold(b.Households[0]).Owns;
                if (ownerOccupied) { b.Owner = Owner.Household; b.OwnerId = b.Households[0]; b.OwnerName = GetHousehold(b.OwnerId).Name; continue; }
                if (firms.Count == 0) { BankTakes(b); continue; }
                var f = firms[fi++ % firms.Count];
                FirmOwns(b, f);
                if (Chance(0.6f)) PropertyLoan(b, GetHousehold(f.OwnerId), f, b.Value * (0.08 + Rand() * 0.17), false);   // bought years ago, mostly paid off
            }
            // staff everything from the people in town
            foreach (var b in Buildings)
            {
                if (b.Def.Cat == Category.Special || b.Def.Homes > 0) continue;
                foreach (var j in b.Def.Jobs)
                {
                    int want = StartStaff(b, j);
                    int have = StaffIn(b, j.Role);
                    for (int k = have; k < want; k++)
                    {
                        var need = Defs.Get(j.Role).Needs;
                        var c = People.Where(p => p.InLaborForce && p.WorkId < 0 && Defs.Qualifies(p.Edu, need))
                                      .OrderByDescending(p => p.Edu == need ? 1 : 0).ThenBy(p => Rand()).FirstOrDefault();
                        if (c == null) break;
                        Hire(b, c, j.Role, true);
                    }
                }
                b.Staffing = b.Def.Cat == Category.Public ? 1f : Math.Max(0.3f, (float)b.Employees.Count / Math.Max(1, b.Def.TotalJobs));
                if (IsBusiness(b)) b.Cash = MonthlyCosts(b) * 2.5 + LoanPayments(b) * 2;
            }
            // shops start with some stock
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (d.Sells.HasValue) b.Stock[(int)d.Sells.Value] = d.Sells == Good.Cars ? 8 : d.StockCap * 0.6f;
                if (d.Makes.HasValue) b.Stock[(int)d.Makes.Value] = d.StockCap * 0.15f;
                if (d.Input.HasValue) b.Stock[(int)d.Input.Value] = Math.Max(d.StockCap * 0.1f, d.InputPer * d.Output * b.Employees.Count * 3);
            }
            // children to school
            foreach (var c in People) if (c.IsChild) Enrol(c);
            // houses nobody lives in come down to empty lots, keeping a few for newcomers
            var empty = Buildings.Where(b => b.Type == BuildingType.House && b.Households.Count == 0).ToList();
            Shuffle(empty);
            for (int i = 6; i < empty.Count; i++) RemoveBuilding(empty[i]);
        }

        int StartStaff(Building b, JobSlot j)
        {
            var d = b.Def;
            if (b.Type == BuildingType.Realty) { PlanFirmStaff(b); return j.Role == Role.PropertyManager ? b.WantManagers : b.WantHands; }
            if (d.Cat == Category.Public)
            {
                if (b.Type == BuildingType.Elementary) return j.Role == Role.Teacher ? 3 : j.Count;
                return j.Count;
            }
            switch (b.Type)
            {
                case BuildingType.Office: return j.Role == Role.Developer ? 3 : j.Role == Role.Accountant ? 2 : 4;
                case BuildingType.GarmentFactory: return j.Role == Role.Assembler ? 6 : j.Count;
                case BuildingType.Construction: return j.Role == Role.ConstructionWorker ? 4 : 1;
                case BuildingType.Trucking: return j.Role == Role.TruckDriver ? 3 : 1;
                case BuildingType.FoodPlant: return j.Role == Role.PlantWorker ? 4 : j.Count;
                case BuildingType.FurnitureWorkshop: return j.Role == Role.Assembler ? 3 : 1;
            }
            return Math.Max(1, (int)Math.Ceiling(j.Count * 0.6));
        }

        void Shuffle<T>(IList<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--) { int k = RandInt(0, i + 1); var t = l[i]; l[i] = l[k]; l[k] = t; }
        }

        public string FirmName(BuildingType t)
        {
            string f = Pick(Names.Firms);
            switch (t)
            {
                case BuildingType.GroceryStore: return f + " Market";
                case BuildingType.GasStation: return f + " Fuel";
                case BuildingType.Office: return f + " Group";
                case BuildingType.FoodPlant: return f + " Foods";
                case BuildingType.GarmentFactory: return f + " Apparel";
                case BuildingType.ElectronicsPlant: return f + " Electronics";
                case BuildingType.FurnitureWorkshop: return f + " Furniture";
                case BuildingType.CarDealer: return f + " Motors";
                case BuildingType.Trucking: return f + " Logistics";
                case BuildingType.Construction: return f + " Builders";
                case BuildingType.Farm: return f + " Farms";
                case BuildingType.Restaurant: return f + " Grill";
                case BuildingType.Cafe: return f + " Coffee";
                case BuildingType.Bar: return f + " Tavern";
                case BuildingType.ClothingStore: return f + " Outfitters";
                case BuildingType.ElectronicsStore: return f + " Tech";
                case BuildingType.FurnitureStore: return f + " Home";
                case BuildingType.AutoRepair: return f + " Auto Service";
                case BuildingType.HairSalon: return f + " Salon";
                case BuildingType.Gym: return f + " Fitness";
                case BuildingType.Bank: return f + " Bank";
                case BuildingType.Realty: return f + " Properties";
                default: return f + " Co.";
            }
        }
    }
}
