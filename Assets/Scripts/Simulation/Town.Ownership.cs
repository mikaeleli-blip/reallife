using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// Who owns what. Everything in town is owned in town: families own their homes and their businesses; property firms (local
    /// companies with an office, managers and handymen) build the rental homes, let them and look after them; the town's bank holds
    /// whatever is repossessed until somebody here buys it. Founders put some of their savings in and borrow the rest from the
    /// bank, secured on the building. When a family leaves, what they owned is sold to people who stay.
    /// </summary>
    public partial class Town
    {
        // ================================================================== who owns it
        /// <summary>The company that owns a building (null for a family, the town or the bank).</summary>
        public Building OwnerFirm(Building b) { return b != null && b.Owner == Owner.Firm ? GetBuilding(b.OwnerId) : null; }
        public bool BankHeld(Building b) { return b.Owner == Owner.Firm && b.OwnerId < 0; }
        public IEnumerable<Building> Portfolio(Building firm) { foreach (var b in Buildings) if (b.Owner == Owner.Firm && b.OwnerId == firm.Id) yield return b; }
        public List<Building> PropertyFirms() { return Buildings.Where(b => b.Type == BuildingType.Realty && b.Open).ToList(); }

        void FamilyOwns(Building b, Household h) { b.Owner = Owner.Household; b.OwnerId = h.Id; b.OwnerName = h.Name; }
        void FirmOwns(Building b, Building firm) { b.Owner = Owner.Firm; b.OwnerId = firm.Id; b.OwnerName = Title(firm); }

        /// <summary>The bank takes a building back: a shut business, a repossessed home, a failed firm's flats.</summary>
        void BankTakes(Building b)
        {
            b.Owner = Owner.Firm; b.OwnerId = -1; b.OwnerName = BankName;
            if (b.Type == BuildingType.House && b.Households.Count == 0) { b.ForSale = true; b.VacantDays = 0; }
        }

        /// <summary>Money to a building's owner: rent to a landlord family or firm, or to the bank for what it holds.</summary>
        void PayOwner(Building b, double amount, bool revenue)
        {
            if (amount <= 0) return;
            if (b.Owner == Owner.Household)
            {
                var h = GetHousehold(b.OwnerId);
                if (h != null) { HouseholdEarns(h, amount, HouseholdBook.Business); return; }
            }
            else if (b.Owner == Owner.Firm)
            {
                var f = GetBuilding(b.OwnerId);
                if (f != null) { f.Cash += amount; if (revenue) { f.Revenue += amount; f.Customers++; } return; }
                BankCapital += amount; return;
            }
            else if (b.Owner == Owner.Town) { Treasury += amount; return; }
            BankCapital += amount;
        }

        /// <summary>Do the bigger firms of this kind go by a brand rather than the family's name.</summary>
        static bool Branded(BuildingType t)
        {
            switch (t)
            {
                case BuildingType.GroceryStore: case BuildingType.Office: case BuildingType.FoodPlant: case BuildingType.GarmentFactory:
                case BuildingType.ElectronicsPlant: case BuildingType.CarDealer: case BuildingType.Trucking: case BuildingType.GasStation:
                case BuildingType.Realty: case BuildingType.Bank:
                    return true;
                default: return false;
            }
        }

        /// <summary>A name for a business once it has its owner: a brand for the bigger firms, the family's name for small places,
        /// unless a namesake already runs one of the same kind here, when it takes a name of its own.</summary>
        string BrandFor(Building b)
        {
            var t = b.Type;
            if (t == BuildingType.Bank) return BankName;
            if (b.Def.Chain != null) return b.Def.Chain;   // every store of a chain goes by its name
            if (!Branded(t))
            {
                bool namesake = b.Owner == Owner.Household && Buildings.Any(o => o != b && o.Type == t && !(o.Closed && o.Built) && string.IsNullOrEmpty(o.Brand)
                                                                                   && o.Owner == Owner.Household && o.OwnerName == b.OwnerName);
                if (!namesake) return null;
            }
            string n = FirmName(t);
            for (int i = 0; i < 40 && Buildings.Any(o => o != b && o.Brand == n); i++) n = FirmName(t);
            return n;
        }

        // ================================================================== a business's debts
        /// <summary>The business whose account pays a loan (null: a family pays it).</summary>
        public Building LoanPayer(Loan l)
        {
            if (l.FirmId >= 0) return GetBuilding(l.FirmId);
            return l.Kind == LoanKind.Business ? GetBuilding(l.BuildingId) : null;
        }

        bool PaidBy(Loan l, Building b) { return l.FirmId == b.Id || (l.FirmId < 0 && l.Kind == LoanKind.Business && l.BuildingId == b.Id); }
        public double LoanPayments(Building b) { double s = 0; foreach (var l in Loans) if (!l.Deferred && PaidBy(l, b)) s += l.Payment; return s; }
        public double Debt(Building b) { double s = 0; foreach (var l in Loans) if (PaidBy(l, b)) s += l.Principal; return s; }

        /// <summary>What a business can borrow against: its building, and a property firm's homes as well.</summary>
        public double Collateral(Building b)
        {
            double v = b.Value;
            if (b.Type == BuildingType.Realty) foreach (var o in Portfolio(b)) v += o.Value;
            // and what's on the shelves or in the yard
            if (b.Stock != null) for (int g = 0; g < b.Stock.Length; g++) v += b.Stock[g] * Defs.Wholesale[g];
            return v;
        }

        /// <summary>What a business keeps in its account before the owners take anything out.</summary>
        double Cushion(Building b)
        {
            double c = (MonthlyCosts(b) + LoanPayments(b)) * 2;
            if (b.Type == BuildingType.Realty)
            {
                foreach (var o in Portfolio(b)) c += o.Value * (Policy.PropertyTax + Balance.Maintenance) / 12 * 2;
                c += Defs.Get(BuildingType.Apartments).StartupCost * 0.12;   // put by toward the next block
            }
            if (b.Type == BuildingType.CarDealer) c += Balance.CarPrice * 0.9 * 2;   // to order in the next cars
            return c;
        }

        /// <summary>A new loan secured on a building. While it's going up nothing is paid back; payments start when it's done.</summary>
        Loan PropertyLoan(Building asset, Household h, Building payer, double amount, bool building)
        {
            var l = NewLoan(LoanKind.Business, h, asset, amount, Balance.PropertyRate, Balance.PropertyMonths);
            l.FirmId = payer != null ? payer.Id : -1;
            if (building) l.Deferred = true;
            return l;
        }

        /// <summary>Who pays for a building as it goes up: the town, or its owners from their savings and then a loan.</summary>
        void ClientPays(Building b, double amount)
        {
            if (amount <= 0) return;
            b.Cost += amount;
            if (b.Owner == Owner.Town) { Treasury -= amount; Book.Construction += amount; return; }
            var firm = OwnerFirm(b);
            var family = b.Owner == Owner.Household ? GetHousehold(b.OwnerId) : firm != null ? GetHousehold(firm.OwnerId) : null;
            double left = amount;
            // the owners' own money first, up to what they meant to put in
            if (b.EquityLeft > 0)
            {
                if (firm != null)
                {
                    double take = Math.Min(Math.Min(left, b.EquityLeft), Math.Max(0, firm.Cash - Cushion(firm) * 0.5));
                    firm.Cash -= take; left -= take; b.EquityLeft -= take;
                }
                if (family != null && b.EquityLeft > 0 && left > 0)
                {
                    double take = Math.Min(Math.Min(left, b.EquityLeft), Math.Max(0, family.Cash - 4000));
                    HouseholdPays(family, take, HouseholdBook.Goods); left -= take; b.EquityLeft -= take;
                }
            }
            if (left <= 0.01) return;
            if (firm == null && b.Owner != Owner.Household) { BankCapital -= left; return; }   // the bank's own building
            var l = Loans.FirstOrDefault(x => x.Kind == LoanKind.Business && x.BuildingId == b.Id && x.Deferred && x.FirmId == (firm != null ? firm.Id : -1));
            if (l == null) PropertyLoan(b, family, firm, left, true);
            else { l.Principal += left; BankCapital -= left; if (BankCapital < 0) { BankFunding += -BankCapital; BankCapital = 0; } }
        }

        /// <summary>A building is finished: the loans on it start being paid.</summary>
        void StartRepaying(Building b)
        {
            foreach (var l in Loans)
                if (l.BuildingId == b.Id && l.Kind == LoanKind.Business && l.Deferred) { l.Deferred = false; l.Payment = Annuity(l.Principal, l.Rate, Balance.PropertyMonths); }
        }

        // ================================================================== property firms
        /// <summary>Could this firm take on a new building: is there money for its share, and will the rent carry the loan?</summary>
        bool CanTakeOn(Building firm, BuildingType t)
        {
            var d = Defs.Get(t);
            double cost = d.StartupCost;
            bool forSale = t == BuildingType.House;
            double equity = cost * (forSale ? 0.05 : 0.15);
            var family = GetHousehold(firm.OwnerId);
            // a house to sell needs only a little of the firm's own money; flats to keep need a proper reserve behind them
            double reserve = forSale ? MonthlyCosts(firm) : Cushion(firm) * 0.5;
            double has = Math.Max(0, firm.Cash - reserve) + (family != null ? Math.Max(0, family.Cash - 20000) : 0);
            if (has < equity) return false;
            // what it would owe against what it has: the homes, the office, and the money in the bank (a new firm's working
            // capital is borrowed, and sitting there); the new building's loan is its cost less the owners' share
            if (Debt(firm) - Math.Max(0, firm.Cash) + (cost - equity) > Collateral(firm) * 0.8 + cost * 0.9) return false;
            if (forSale) return Portfolio(firm).Count(b => b.ForSale || (!b.Built && b.Type == BuildingType.House)) < 4 + Portfolio(firm).Count() / 8;
            double rentIn = 0; foreach (var b in Portfolio(firm)) if (b.Built) rentIn += b.Rent * b.Households.Count;
            double newRent = d.Homes * d.Rent * HousingIndex * 0.9;
            double pay = Annuity(cost - equity, Balance.PropertyRate, Balance.PropertyMonths);
            return LoanPayments(firm) + pay <= (rentIn + newRent) * 0.7;
        }

        /// <summary>A property firm's rent roll: what its tenants pay a month.</summary>
        public double RentIn(Building firm) { double r = 0; foreach (var o in Portfolio(firm)) if (o.Built) r += o.Rent * o.Households.Count; return r; }

        /// <summary>A property firm whose tenants' rent covers its loan payments is sound, whatever its last few months looked like
        /// (a firm starting out loses money until it has homes to let).</summary>
        public bool RentCarries(Building b) { if (b.Type != BuildingType.Realty) return false; double r = RentIn(b); return r > 0 && r >= LoanPayments(b); }

        /// <summary>The property firm that builds next, of those that can take it on: the one with the least already going up, and
        /// among those, more often the one with money to spare. (So a new firm gets work too, not just the biggest.)</summary>
        Building HousingFirm(BuildingType t)
        {
            Building best = null; double bs = double.MinValue;
            foreach (var f in PropertyFirms())
            {
                if (!Connected(f) || !CanTakeOn(f, t)) continue;
                double s = -Portfolio(f).Count(b => !b.Built) * 2 + Math.Max(-1, Math.Min(1, (f.Cash - Cushion(f)) / 200000)) + Rand() * 1.5;
                if (s > bs) { bs = s; best = f; }
            }
            return best;
        }

        /// <summary>Upkeep on a firm's homes: its handymen do what they can, the local builder the rest. Materials come from out of town.</summary>
        void FirmUpkeep(Building firm, double upkeep)
        {
            if (upkeep <= 0) return;
            int hands = 0; foreach (var id in firm.Employees) { var c = GetCitizen(id); if (c != null && c.Role == Role.Handyman) hands++; }
            double cap = hands * Balance.HandymanUpkeep, labour = upkeep * 0.6;
            double own = Math.Min(labour, cap), share = labour > 0 ? own / labour : 0;
            double materials = upkeep * 0.4 * share, contracted = upkeep * (1 - share);
            firm.Cash -= materials + contracted; firm.OtherCosts += materials + contracted;
            Trade.Imports += materials; Repairs(contracted);
            firm.CapUsed += (float)own; firm.CapTotal += (float)cap;
        }

        /// <summary>How many managers and handymen a firm wants for the homes it has.</summary>
        void PlanFirmStaff(Building firm)
        {
            int units = 0; double upkeep = 0;
            foreach (var b in Portfolio(firm)) { units += b.Def.Homes; upkeep += b.Value * Balance.Maintenance / 12; }
            firm.WantManagers = Math.Max(1, Math.Min(2, (int)Math.Ceiling(units / (double)Balance.UnitsPerManager)));
            firm.WantHands = Math.Max(0, Math.Min(3, (int)Math.Round(upkeep * 0.6 / Balance.HandymanUpkeep)));
        }

        // ================================================================== selling up
        /// <summary>The family that owns this is leaving or gone: it's sold to someone who stays.</summary>
        void SellLocally(Building b)
        {
            if (b.Def.Homes > 0) SellHome(b, b.Value * 0.95);
            else if (IsBusiness(b) && b.Built && !b.Closed) TransferBusiness(b);
            else { SellerGets(b, b.Value * 0.5, false); BankCapital -= b.Value * 0.5; BankTakes(b); }
        }

        /// <summary>A home sold whole: a property firm buys it to let if one can, otherwise the bank does and puts it on the market.
        /// The bank selling what it holds asks less down (equityShare), and lends the company the rest against the building.</summary>
        void SellHome(Building b, double price, double equityShare = 0.15)
        {
            Building buyer = null;
            foreach (var f in PropertyFirms().OrderBy(x => Portfolio(x).Count()))
            {
                double share = price * equityShare;
                var fam = GetHousehold(f.OwnerId);
                // the firm keeps enough for a month's wages and bills and its loans, the new one included; the owners make up the rest
                double keep = MonthlyCosts(f) + LoanPayments(f) + Annuity(price - share, Balance.PropertyRate, Balance.PropertyMonths);
                double has = Math.Max(0, f.Cash - keep) + (fam != null ? Math.Max(0, fam.Cash - 20000) : 0);
                // what it already owes against what it already has, and the building itself secures its own loan
                if (has < share || Debt(f) - Math.Max(0, f.Cash) + (price - share) > Collateral(f) * 0.8 + price * (1 - equityShare)) continue;
                double rentIn = 0; foreach (var o in Portfolio(f)) if (o.Built) rentIn += o.Rent * o.Households.Count;
                // the rent it would bring: what it's let at now, or the going rate, at nine in ten let
                double newRent = b.Def.Homes * Math.Max(b.Rent, b.Def.Rent * HousingIndex) * 0.9;
                double pay = Annuity(price - share, Balance.PropertyRate, Balance.PropertyMonths);
                if (LoanPayments(f) + pay > (rentIn + newRent) * 0.75) continue;
                buyer = f; break;
            }
            if (buyer != null)
            {
                SellerGets(b, price, false);
                double share = price * equityShare;
                var fam = GetHousehold(buyer.OwnerId);
                double keep = MonthlyCosts(buyer) + LoanPayments(buyer) + Annuity(price - share, Balance.PropertyRate, Balance.PropertyMonths);
                double fromFirm = Math.Min(share, Math.Max(0, buyer.Cash - keep));
                buyer.Cash -= fromFirm;
                if (fam != null && share > fromFirm) HouseholdPays(fam, share - fromFirm, HouseholdBook.Goods);
                PropertyLoan(b, fam, buyer, price - share, false);
                FirmOwns(b, buyer); b.ForSale = false; b.VacantDays = 0; b.Cost = price;
                buyer.LossMonths = 0;   // it has homes to let now: judged from here on
                if (b.Type == BuildingType.House || b.Households.Count == 0) b.Rent = b.Def.Rent * HousingIndex;
                AddLog(Title(buyer) + " bought " + (b.Def.Homes > 1 ? "an " + b.Def.Name.ToLower() : "a house") + " to let.", LogKind.Money, b.Id);
                return;
            }
            SellerGets(b, price, false);
            BankCapital -= price;
            BankTakes(b);
            b.Rent = b.Def.Rent * HousingIndex;
        }

        /// <summary>A going business whose owners are leaving: someone who works there buys it, or another family here does.
        /// If nobody will, it shuts.</summary>
        void TransferBusiness(Building b)
        {
            var seller = b.Owner == Owner.Household ? GetHousehold(b.OwnerId) : null;
            double debt = Debt(b);
            double price = Math.Max(b.Value * 0.15, b.Value * 0.75 - debt);
            Household buyer = null; Citizen runner = null;
            // the most experienced person working there who can raise a deposit
            foreach (var c in b.Employees.Select(GetCitizen).Where(c => c != null && !c.IsChild && c.HouseholdId != b.OwnerId && c.Experience >= 3 && c.Age < 63).OrderByDescending(c => c.Experience))
            {
                var h = HouseholdOf(c);
                if (h == null || h.Cash < Math.Max(8000, price * 0.1) + 3000) continue;
                if (Buildings.Any(o => o.Owner == Owner.Household && o.OwnerId == h.Id && IsBusiness(o))) continue;
                buyer = h; runner = c; break;
            }
            // or a family here with money to put to work
            if (buyer == null)
                buyer = Households.Where(h => h.Id != b.OwnerId && h.Cash > price * 0.25 + 30000 && Buildings.Count(o => o.Owner == Owner.Household && o.OwnerId == h.Id && IsBusiness(o)) < 3)
                                  .OrderByDescending(h => h.Cash).FirstOrDefault();
            if (buyer == null) { CloseBusiness(b, "nobody in town would take it on"); return; }
            // the leaving family's people stop being owners there
            if (seller != null) foreach (var m in Members(seller)) if (m.WorkId == b.Id) m.Owner = false;
            double down = Math.Min(price, Math.Max(0, buyer.Cash - 5000) * 0.6);
            HouseholdPays(buyer, down, HouseholdBook.Goods);
            if (price > down) PropertyLoan(b, buyer, b, price - down, false);
            SellerGets(b, price, true);
            string was = Title(b);
            FamilyOwns(b, buyer);
            if (string.IsNullOrEmpty(b.Brand)) b.Brand = BrandFor(b);
            if (runner != null) { runner.Owner = true; Note(runner, "Bought " + PlaceName(b) + "."); }
            foreach (var l in Loans) if (l.Kind == LoanKind.Business && l.BuildingId == b.Id) l.HouseholdId = buyer.Id;
            AddLog((runner != null ? runner.Name + " bought " : "The " + buyer.Name + " family bought ") + was + (seller != null ? " from the " + seller.Name + " family." : "."), LogKind.Money, b.Id);
        }

        /// <summary>The seller is paid: a family (who clear their mortgage on it), a firm (whose loan on it is repaid), or the bank.
        /// A going business is sold with its business loans, which the buyer takes over.</summary>
        void SellerGets(Building b, double price, bool keepBusinessLoans)
        {
            if (b.Owner == Owner.Household)
            {
                var seller = GetHousehold(b.OwnerId);
                if (seller != null)
                {
                    HouseholdEarns(seller, price, HouseholdBook.Business);
                    foreach (var l in Loans.ToList())
                        if (l.BuildingId == b.Id && l.FirmId < 0 && (l.Kind == LoanKind.Mortgage || (!keepBusinessLoans && l.HouseholdId == seller.Id))) { HouseholdPays(seller, l.Principal, HouseholdBook.Debt); BankCapital += l.Principal; Loans.Remove(l); }
                    return;
                }
            }
            var firm = OwnerFirm(b);
            if (firm != null)
            {
                // a sale is the firm's takings, less what the home cost it
                firm.Cash += price; firm.Revenue += price; firm.Supplies += b.Cost > 0 ? b.Cost : b.Def.StartupCost;
                foreach (var l in Loans.ToList())
                    if (l.BuildingId == b.Id && l.FirmId == firm.Id) { firm.Cash -= l.Principal; BankCapital += l.Principal; Loans.Remove(l); }
                return;
            }
            BankCapital += price;
        }

        /// <summary>Each month the bank offers what it holds: flats to the property firms, houses on the open market.</summary>
        void BankSales()
        {
            foreach (var b in Buildings.ToList())
            {
                if (!BankHeld(b) || !b.Built || b.Def.Homes == 0) continue;
                // a house goes up for sale to families when the bank takes it; one it ended up letting goes to the firms
                if (b.Type == BuildingType.House && (b.ForSale || b.Households.Count == 0)) continue;
                if (Chance(0.6f)) SellHome(b, b.Value * 0.8, 0.05);
            }
        }

        // ================================================================== old saves
        /// <summary>Saves from before flats had their own zone: the land under apartment blocks becomes apartment zoning.</summary>
        void MigrateFlats()
        {
            foreach (var b in Buildings)
                if (b.Type == BuildingType.Apartments)
                    for (int y = b.Y; y < b.Y + b.Def.H; y++) for (int x = b.X; x < b.X + b.Def.W; x++) SetZoneBits(x, y, Zone.Apartments);
            MapVersion++;
        }

        /// <summary>Brings a game saved before everything was locally owned up to date: firms out of town sell to families and
        /// property firms here, and farms get farmland.</summary>
        public void Migrate()
        {
            if (SaveVersion >= CurrentVersion) return;
            if (SaveVersion >= 2)
            {
                if (SaveVersion < 3) MigrateCars();
                MigrateFlats();
                SaveVersion = CurrentVersion; return;
            }
            foreach (var b in Buildings)
                if (b.Type == BuildingType.Farm)
                    for (int y = b.Y; y < b.Y + b.Def.H; y++) for (int x = b.X; x < b.X + b.Def.W; x++) SetZoneBits(x, y, Zone.Farmland);
            // property firms for the homes outside landlords had
            var outsideHomes = Buildings.Where(b => b.Owner == Owner.Outside && b.Def.Homes > 0 && b.OwnerName != BankName).ToList();
            int firms = outsideHomes.Sum(b => b.Def.Homes) > 40 ? 2 : outsideHomes.Count > 0 ? 1 : 0;
            var made = new List<Building>();
            for (int i = 0; i < firms; i++)
            {
                int x, y;
                if (!FindLot(BuildingType.Realty, out x, out y)) break;
                var f = AddBuilding(BuildingType.Realty, x, y, FindFacing(x, y, 1, 1), true);
                var fam = Households.Where(h => !Buildings.Any(o => o.Owner == Owner.Household && o.OwnerId == h.Id && IsBusiness(o))).OrderByDescending(h => h.Cash).FirstOrDefault()
                          ?? Households.OrderByDescending(h => h.Cash).FirstOrDefault();
                if (fam == null) break;
                FamilyOwns(f, fam);
                f.Brand = BrandFor(f);
                f.Value = f.Def.Value;
                OpenBusiness(f);
                made.Add(f);
            }
            int k = 0;
            foreach (var b in outsideHomes)
            {
                if (made.Count == 0) { BankTakes(b); continue; }
                var f = made[k++ % made.Count];
                FirmOwns(b, f);
                if (Chance(0.6f)) PropertyLoan(b, GetHousehold(f.OwnerId), f, b.Value * (0.08 + Rand() * 0.17), false);
            }
            foreach (var f in made) { PlanFirmStaff(f); f.Cash += 60000; }   // the rent the old landlords left in the account
            // businesses: to a family here
            foreach (var b in Buildings.ToList())
            {
                if (b.Owner != Owner.Outside || b.Type == BuildingType.FreightTerminal) continue;
                if (b.Def.Homes > 0 || b.Closed || !IsBusiness(b) || b.OwnerName == BankName) { BankTakes(b); continue; }
                var staff = b.Employees.Select(GetCitizen).Where(c => c != null && !c.IsChild).OrderByDescending(c => Defs.Get(c.Role).Wage).ThenByDescending(c => c.Experience).ToList();
                Household owner = null;
                foreach (var c in staff)
                {
                    var h = HouseholdOf(c);
                    if (h == null || Buildings.Any(o => o.Owner == Owner.Household && o.OwnerId == h.Id && IsBusiness(o))) continue;
                    owner = h; c.Owner = true; break;
                }
                if (owner == null) owner = Households.OrderByDescending(h => h.Cash).FirstOrDefault();
                if (owner == null) { BankTakes(b); continue; }
                string brand = b.Type == BuildingType.Bank ? BankName : b.OwnerName;
                FamilyOwns(b, owner);
                b.Brand = Branded(b.Type) ? brand : null;
            }
            MigrateCars();
            MigrateFlats();
            SaveVersion = CurrentVersion;
            MapVersion++;
        }
    }
}
