using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// Homes and the bills that come with them. Renters pay their landlord (a property firm in town, a family here who owns a second
    /// place, or the bank for what it has repossessed); owners pay their mortgage and property tax. Landlords move rents with how many flats stand empty, and house prices
    /// follow the whole town's market. Renters with savings and a steady job buy; families who miss their payments are evicted
    /// or foreclosed on. Also the month's other bills and incomes: utilities, benefits, pensions and tuition.
    /// </summary>
    public partial class Town
    {
        public float HousingIndex = 1f;      // house prices and usual rents against normal

        public int FreeUnits(Building b)
        {
            if (!b.Built || b.Def.Homes == 0 || !Connected(b)) return 0;
            return Math.Max(0, b.Def.Homes - b.Households.Count);
        }

        public int VacantUnits() { int n = 0; foreach (var b in Buildings) if (!b.ForSale) n += FreeUnits(b); return n; }
        public int TotalUnits() { int n = 0; foreach (var b in Buildings) if (b.Built && b.Def.Homes > 0) n += b.Def.Homes; return n; }

        /// <summary>A home to rent for a household of this size, with rent under maxShare of its take-home pay. Null if none.</summary>
        public Building FindHome(int size, double takeHome, double maxShare)
        {
            Building best = null; double bs = double.MaxValue;
            foreach (var b in Buildings)
            {
                if (b.ForSale || FreeUnits(b) == 0) continue;
                if (b.Rent > takeHome * maxShare) continue;
                double s = b.Rent;
                if (size >= 3 && b.Type == BuildingType.Apartments) s *= 1.35;
                if (size <= 2 && b.Type == BuildingType.House) s *= 1.1;
                s *= 0.9 + Rand() * 0.2;
                if (s < bs) { bs = s; best = b; }
            }
            return best;
        }

        public double MonthlyIncome(Household h)
        {
            double s = 0;
            foreach (var c in Members(h)) { s += c.Wage; if (c.Retired) s += Balance.Pension; }
            return s + h.LastIn[HouseholdBook.Business] * 0.7;
        }

        // ================================================================== every month
        void HousingMonthly()
        {
            foreach (var h in Households.ToList())
            {
                var home = HomeOf(h);
                var ms = Members(h);
                // utilities
                double util = Balance.UtilitiesHome * (0.7 + 0.15 * Math.Min(4, ms.Count));
                HouseholdPays(h, util, HouseholdBook.Utilities); Trade.Utilities += util;
                double ins = Balance.Insurance + Balance.InsuranceAdult * ms.Count(m => !m.IsChild);
                ins = Math.Min(ins, Math.Max(0, h.Cash));
                HouseholdPays(h, ins, HouseholdBook.Utilities); Trade.Utilities += ins;
                // benefits and pensions
                foreach (var c in ms)
                {
                    if (c.Retired) { HouseholdEarns(h, Balance.Pension, HouseholdBook.Pension); Trade.Pensions += Balance.Pension; }
                    else if (c.InLaborForce && c.WorkId < 0 && c.JoblessDays > 1 && c.JoblessDays <= 24 && Policy.Benefit > 0)
                    {
                        HouseholdEarns(h, Policy.Benefit, HouseholdBook.Benefits); Treasury -= Policy.Benefit; Book.Benefits += Policy.Benefit;
                    }
                }
                // tuition
                foreach (var c in ms)
                {
                    if (c.Stage < Stage.TradeSchool || c.SchoolId < 0) continue;
                    double fee = Tuition(c.Stage) * (1 - Policy.TuitionAid);
                    if (fee <= 0) continue;
                    if (h.Cash > fee + Essentials(h) * 1.5) { HouseholdPays(h, fee, HouseholdBook.School); Treasury += fee; Book.Fees += fee; }
                    else { StudentLoan(h, c, fee); Treasury += fee; Book.Fees += fee; }
                }
                var bank = BankBuilding;
                if (bank != null && h.Cash > Balance.BankFee) { HouseholdPays(h, Balance.BankFee, HouseholdBook.Utilities); bank.Cash += Balance.BankFee; bank.Revenue += Balance.BankFee; }
                if (home == null) continue;
                if (h.Owns)
                {
                    double tax = home.Value * Policy.PropertyTax / 12;
                    HouseholdPays(h, tax, HouseholdBook.Housing); Treasury += tax; Book.PropertyTax += tax;
                    double fix = home.Value * Balance.Maintenance / 12;
                    if (h.Cash > fix * 3) { HouseholdPays(h, fix, HouseholdBook.Housing); Repairs(fix); }
                    continue;
                }
                // rent
                double rent = home.Rent;
                if (h.Cash >= rent)
                {
                    HouseholdPays(h, rent, HouseholdBook.Housing);
                    PayLandlord(home, rent);
                    h.MissedRent = 0;
                }
                else
                {
                    double part = Math.Max(0, h.Cash);
                    HouseholdPays(h, part, HouseholdBook.Housing); PayLandlord(home, part);
                    h.MissedRent++;
                    var c = ms.FirstOrDefault(m => !m.IsChild);
                    if (c != null) Note(c, "Behind on the rent.");
                    if (h.MissedRent == 2) AddLog("The " + h.Name + " family is two months behind on the rent.", LogKind.Bad, c != null ? c.Id : -1, true);
                }
            }
            Landlords();
            HousePrices();
            HomeBuying();
        }

        /// <summary>Repair and upkeep work goes to the local builders, a share each; with none in town it goes to outside firms.
        /// About 40% of it is materials bought from outside.</summary>
        public void Repairs(double amount)
        {
            if (amount <= 0) return;
            var builders = Buildings.Where(b => b.Type == BuildingType.Construction && b.Open && b.Employees.Count > 0).ToList();
            if (builders.Count == 0) { Trade.Imports += amount; return; }
            double each = amount / builders.Count;
            foreach (var c in builders)
            {
                double mat = each * 0.4;
                c.Cash += each - mat; c.Revenue += each; c.Supplies += mat;
                Trade.Imports += mat;
            }
        }

        void PayLandlord(Building home, double amount) { PayOwner(home, amount, true); }

        /// <summary>Rental buildings: tax, upkeep, and next month's rent. Firms' handymen do their upkeep; families and the bank
        /// pay the local builder.</summary>
        void Landlords()
        {
            float vacancy = TotalUnits() > 0 ? VacantUnits() / (float)TotalUnits() : 0;
            var firmUpkeep = new Dictionary<int, double>();
            foreach (var b in Buildings)
            {
                if (!b.Built || b.Def.Homes == 0) continue;
                bool occupiedByOwner = b.Owner == Owner.Household && b.Households.Contains(b.OwnerId);
                if (occupiedByOwner) continue;
                double tax = b.Value * Policy.PropertyTax / 12, upkeep = b.Value * Balance.Maintenance / 12;
                Treasury += tax; Book.PropertyTax += tax;
                var firm = OwnerFirm(b);
                if (firm != null)
                {
                    firm.Cash -= tax; firm.TaxPaid += tax;
                    double u; firmUpkeep.TryGetValue(firm.Id, out u); firmUpkeep[firm.Id] = u + upkeep;
                }
                else if (b.Owner == Owner.Household && GetHousehold(b.OwnerId) != null)
                {
                    HouseholdPays(GetHousehold(b.OwnerId), tax + upkeep, HouseholdBook.Housing); Repairs(upkeep);
                }
                else { BankCapital -= tax + upkeep; Repairs(upkeep); }
                // the rent follows how many homes stand empty
                int free = FreeUnits(b);
                if (b.ForSale) continue;
                if (free > 0) { b.VacantDays += Balance.DaysPerMonth; if (b.VacantDays >= 4) b.Rent *= 0.97; }
                else { b.VacantDays = 0; if (vacancy < 0.05f) b.Rent *= 1.025; else if (vacancy < 0.09f) b.Rent *= 1.008; }
                b.Rent = Math.Round(Math.Max(b.Def.Rent * 0.55, Math.Min(b.Def.Rent * 2.4, b.Rent)) / 5) * 5;
            }
            foreach (var kv in firmUpkeep) { var f = GetBuilding(kv.Key); if (f != null) FirmUpkeep(f, kv.Value); }
            BankSales();
        }

        void HousePrices()
        {
            float vacancy = TotalUnits() > 0 ? VacantUnits() / (float)TotalUnits() : 0;
            int forSale = Buildings.Count(b => b.ForSale && b.Households.Count == 0);
            if (vacancy < 0.03f && forSale < 2) HousingIndex *= 1.01f;
            else if (vacancy < 0.06f) HousingIndex *= 1.002f;
            else if (vacancy > 0.09f || forSale > 4) HousingIndex *= 0.99f;
            // and prices are pulled back toward what a new house costs to build and sell: they don't ratchet up for ever
            HousingIndex += (1 - HousingIndex) * 0.012f;
            HousingIndex = Math.Max(0.6f, Math.Min(2.5f, HousingIndex));
            foreach (var b in Buildings) if (b.Def.Homes > 0 && b.Built) b.Value = Math.Round(b.Def.Value * HousingIndex * (0.92 + 0.16 * (b.Id % 7) / 6.0));
        }

        /// <summary>Renters with savings and steady pay buy the houses on the market; well-off families buy one to let.</summary>
        void HomeBuying()
        {
            var market = Buildings.Where(b => b.ForSale && b.Built && b.Households.Count == 0 && Connected(b)).ToList();
            // unsold for four months: the property firm that built it lets it instead
            foreach (var b in market.ToList())
            {
                b.VacantDays += Balance.DaysPerMonth;
                if (b.VacantDays >= 16 && (OwnerFirm(b) != null || BankHeld(b))) { b.ForSale = false; b.VacantDays = 0; b.Rent = b.Def.Rent * HousingIndex; market.Remove(b); }
            }
            if (market.Count == 0) return;
            foreach (var h in Households.OrderBy(x => Rand()).ToList())
            {
                if (market.Count == 0) break;
                if (h.Owns || h.HomeId < 0 || h.MissedRent > 0) continue;
                var ms = Members(h);
                if (!ms.Any(m => m.WorkId >= 0 && m.Experience > 1) || !Chance(0.3f)) continue;
                double income = MonthlyIncome(h);
                Building pick = null;
                foreach (var b in market.OrderBy(b => b.Value))
                {
                    double price = b.Value;
                    double cash = price * 0.13 + Essentials(h);
                    if (h.Cash < cash) break;
                    double pay = Annuity(price * 0.9, Balance.MortgageRate, Balance.MortgageMonths);
                    if (!Approve(h, pay + price * Policy.PropertyTax / 12, 0.36)) continue;
                    pick = b; break;
                }
                if (pick == null) continue;
                BuyHome(h, pick);
                market.Remove(pick);
            }
            // a family with plenty put by buys a house to let
            foreach (var h in Households.Where(x => x.Cash > 160000).OrderBy(x => Rand()).ToList())
            {
                if (market.Count == 0 || !Chance(0.25f)) break;
                var b = market.OrderBy(x => x.Value).First();
                if (h.Cash < b.Value * 1.05 + 30000) continue;
                SellerGets(b, b.Value, false);
                HouseholdPays(h, b.Value * 1.03, HouseholdBook.Housing); Trade.Imports += b.Value * 0.03;
                FamilyOwns(b, h); b.ForSale = false; b.Rent = b.Def.Rent * HousingIndex; b.VacantDays = 0; b.Cost = b.Value;
                market.Remove(b);
                AddLog("The " + h.Name + " family bought a house to rent out.", LogKind.Money);
            }
        }

        void BuyHome(Household h, Building b, bool quiet = false)
        {
            double price = b.Value, down = price * 0.1, fees = price * 0.03;
            SellerGets(b, price, false);
            b.Cost = price;
            HouseholdPays(h, down + fees, HouseholdBook.Housing); Trade.Imports += fees;
            NewLoan(LoanKind.Mortgage, h, b, price - down, Balance.MortgageRate, Balance.MortgageMonths);
            MoveIn(h, b, true);
            h.Furniture = Math.Max(0, h.Furniture - 0.15f);
            var c = Members(h).FirstOrDefault(m => !m.IsChild);
            if (c != null) Note(c, "Bought a house for " + Money(price) + ".");
            if (!quiet) AddLog("The " + h.Name + " family bought a house for " + Money(price) + " with a mortgage.", LogKind.Money, c != null ? c.Id : -1, c != null);
        }

        void Foreclose(Loan l)
        {
            var h = GetHousehold(l.HouseholdId);
            var b = GetBuilding(l.BuildingId);
            Loans.Remove(l);
            if (b == null) return;
            if (h != null)
            {
                AddLog("The bank foreclosed on the " + h.Name + " family's house.", LogKind.Bad);
                var c = Members(h).FirstOrDefault(m => !m.IsChild); if (c != null) Note(c, "Lost the house to foreclosure.");
                if (h.HomeId == b.Id)
                {
                    MoveOut(h);
                    var rental = FindHome(h.Members.Count, MonthlyIncome(h) * 0.85, 0.5);
                    if (rental != null) MoveIn(h, rental, false);
                }
            }
            // the bank holds it and sells it on (BankSales); what it gets back comes in then
            BankTakes(b);
        }

        // ================================================================== loans
        public static double Annuity(double principal, double rate, int months)
        {
            double r = rate / 12;
            if (r <= 0) return principal / months;
            return principal * r / (1 - Math.Pow(1 + r, -months));
        }

        public double DebtPayments(Household h)
        {
            double s = 0;
            foreach (var l in Loans) if (l.HouseholdId == h.Id && !l.Deferred && l.Kind != LoanKind.Business) s += l.Payment;
            return s;
        }

        public double DebtOwed(Household h)
        {
            double s = 0;
            foreach (var l in Loans) if (l.HouseholdId == h.Id && l.Kind != LoanKind.Business) s += l.Principal;
            return s;
        }

        /// <summary>Would the bank lend: all debt payments, this one included, under maxShare of monthly income.</summary>
        public bool Approve(Household h, double newPayment, double maxShare)
        {
            double income = MonthlyIncome(h);
            if (income <= 0) return false;
            if (Loans.Any(l => l.HouseholdId == h.Id && l.Missed > 0)) return false;
            return newPayment <= income * maxShare && DebtPayments(h) + newPayment <= income * 0.45;
        }

        public Loan NewLoan(LoanKind kind, Household h, Building b, double principal, double rate, int months)
        {
            var l = new Loan { Id = NextLoan++, Kind = kind, HouseholdId = h != null ? h.Id : -1, BuildingId = b != null ? b.Id : -1, Principal = principal, Rate = rate, Payment = Annuity(principal, rate, months), Started = Day };
            Loans.Add(l);
            BankCapital -= principal;
            if (BankCapital < 0) { BankFunding += -BankCapital; BankCapital = 0; }
            return l;
        }

        void StudentLoan(Household h, Citizen c, double amount)
        {
            var l = Loans.FirstOrDefault(x => x.Kind == LoanKind.Student && x.CitizenId == c.Id && x.Deferred);
            if (l == null) { l = NewLoan(LoanKind.Student, h, null, 0, Balance.StudentRate, Balance.StudentMonths); l.CitizenId = c.Id; l.Deferred = true; }
            l.Principal += amount; BankCapital -= amount;
            if (BankCapital < 0) { BankFunding += -BankCapital; BankCapital = 0; }
        }

        /// <summary>Lends a business what it needs to keep going, if it looks like a going concern and it has the security.</summary>
        bool BusinessLoan(Building b, double amount)
        {
            // a farm is lent against next season's harvest through the winter
            // and a builder or property firm against the work it has on: their money comes in lumps
            // (or a new business in its first half year, still stocking up and finding its customers)
            // (and a property firm with homes going up, on the market, or just finished and filling with tenants)
            bool pipeline = Day - b.BuiltDay < Balance.DaysPerYear / 2 || (b.Type == BuildingType.Realty && Portfolio(b).Any(o => !o.Built || o.ForSale || Day - o.BuiltDay < Balance.DaysPerYear / 2)) ||
                            (b.Type == BuildingType.Construction && Buildings.Any(o => !o.Built && o.ContractorId == b.Id));
            if (!pipeline && b.LossMonths >= (b.Type == BuildingType.Farm ? 7 : 3) && b.LastRevenue < MonthlyCosts(b) * 0.5) return false;
            // a new business gets the benefit of the doubt in its first year: lent up to what it's worth
            bool young = Day - b.BuiltDay < Balance.DaysPerYear;
            if (Debt(b) + amount > Collateral(b) * (young ? 1.0 : 0.8)) return false;
            NewLoan(LoanKind.Business, b.Owner == Owner.Household ? GetHousehold(b.OwnerId) : null, b, amount, Balance.BusinessRate, Balance.BusinessMonths);
            b.Cash += amount;
            return true;
        }

        public Building BankBuilding { get { return Buildings.FirstOrDefault(b => b.Type == BuildingType.Bank && b.Open); } }

        void LoansMonthly()
        {
            var bank = BankBuilding;
            double interestIn = 0;
            foreach (var l in Loans.ToList())
            {
                double r = l.Rate / 12;
                if (l.Deferred) { l.Principal += l.Principal * r; continue; }
                double interest = l.Principal * r;
                double due = Math.Min(l.Payment, l.Principal + interest);
                var h = GetHousehold(l.HouseholdId);
                var biz = LoanPayer(l);
                bool paid = false;
                if (biz != null) { if (biz.Cash >= due) { biz.Cash -= due; biz.OtherCosts += interest; paid = true; } }
                else if (h != null && l.Kind != LoanKind.Business) { if (h.Cash >= due) { HouseholdPays(h, due, HouseholdBook.Debt); paid = true; } }
                else { Loans.Remove(l); continue; }
                if (paid)
                {
                    l.Missed = 0; l.Principal -= due - interest; BankCapital += due - interest; interestIn += interest;
                    if (l.Principal < 1)
                    {
                        Loans.Remove(l);
                        if (l.Kind == LoanKind.Mortgage && h != null) AddLog("The " + h.Name + " family paid off their mortgage.", LogKind.Good);
                    }
                    continue;
                }
                l.Missed++; l.Principal += interest;
                if (l.Missed < 4) continue;
                switch (l.Kind)
                {
                    case LoanKind.Mortgage: Foreclose(l); break;
                    case LoanKind.Car:
                        Loans.Remove(l);
                        BankCapital += h != null ? Math.Min(l.Principal, Repossess(h)) : l.Principal * 0.4;
                        break;
                    case LoanKind.Business:
                        var asset = GetBuilding(l.BuildingId);
                        Loans.Remove(l);
                        if (asset != null && biz != null && asset != biz)
                        {
                            // a firm behind on the loan for one of its buildings: the bank repossesses that building
                            BankTakes(asset);
                            AddLog(Title(biz) + " fell behind on its loan for " + (asset.Def.Homes > 0 ? "an " + asset.Def.Name.ToLower() : Ref(asset)) + ": the bank took it.", LogKind.Bad, asset.Id);
                        }
                        else if (biz != null) CloseBusiness(biz, "it couldn't repay its loan");
                        break;
                    default:
                        if (l.Missed >= 12) Loans.Remove(l);
                        break;
                }
            }
            double funding = BankFunding * Balance.FundingRate / 12;
            if (bank != null)
            {
                bank.Cash += interestIn; bank.Revenue += interestIn;
                bank.Cash -= funding; bank.OtherCosts += funding;
            }
            else Trade.Profits += interestIn - funding;
            Trade.Profits += funding;
            if (BankFunding > 0 && BankCapital > 500000) { double back = Math.Min(BankFunding, BankCapital - 500000); BankFunding -= back; BankCapital -= back; }
        }
    }
}
