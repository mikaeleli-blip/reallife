using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>A truckload, for the view: from a building to another (-1 is the highway).</summary>
    public struct Shipment { public int From, To; public Good Good; public float Hour; }

    /// <summary>
    /// Businesses as businesses: each keeps its own bank account. Makers turn inputs into goods; shops buy stock from makers in town
    /// or import it, whichever is cheaper; what makers can't sell in town goes out on the highway at world prices. Every month a
    /// business pays its staff, its utilities and its taxes, its owner takes what's above a cushion, and it decides how many people
    /// it needs and what to charge. Run out of money and it borrows; can't borrow and it closes.
    /// </summary>
    public partial class Town
    {
        [NonSerialized] public List<Shipment> Shipments = new List<Shipment>();
        [NonSerialized] Dictionary<int, double> truckLeft = new Dictionary<int, double>();

        public static bool IsBusiness(Building b) { var c = b.Def.Cat; return c == Category.Commercial || c == Category.Industrial; }
        public static bool IsRental(Building b) { return b.Def.Homes > 0; }
        public double World(Good g) { return Defs.World[(int)g] * PriceDrift[(int)g]; }
        public double ImportPrice(Good g) { return Defs.Import[(int)g] * PriceDrift[(int)g]; }

        /// <summary>Customers (or units) the staff in today can handle.</summary>
        public float Capacity(Building b) { return PresentStaff(b) * b.Def.Output; }

        public double MonthlyCosts(Building b) { return PayAndBills(b) + SuppliesPerMonth(b); }

        /// <summary>A month's wages (or, before anyone's hired, the crew it's planning on) and utilities.</summary>
        double PayAndBills(Building b)
        {
            var d = b.Def; double w = 0;
            foreach (var j in d.Jobs) w += OfferWage(b, j.Role) * DesiredStaff(b, j);
            if (b.Employees.Count > 0) { w = 0; foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null) w += c.Wage; } }
            return w + d.TotalJobs * Balance.UtilitiesJob;
        }

        /// <summary>What it buys in a month working flat out: stock for a shop, materials for a plant.</summary>
        double SuppliesPerMonth(Building b)
        {
            var d = b.Def;
            int staff = b.Employees.Count; if (staff == 0) foreach (var j in d.Jobs) staff += DesiredStaff(b, j);
            staff = Math.Max(1, staff);
            if (d.Input.HasValue) return d.Output * d.InputPer * staff * Balance.DaysPerMonth * ImportPrice(d.Input.Value) * 0.8;
            if (d.Sells.HasValue) return d.Output * staff * Balance.DaysPerMonth * Defs.Wholesale[(int)d.Sells.Value] * 0.45;
            return 0;
        }

        void BusinessPays(Building b, double v, ref double line) { b.Cash -= v; line += v; }

        // ================================================================== every day
        void ResetDay()
        {
            if (Shipments == null) Shipments = new List<Shipment>();
            Shipments.Clear();
            if (truckLeft == null) truckLeft = new Dictionary<int, double>();
            truckLeft.Clear();
            qualityCache = new Dictionary<int, float>();
            foreach (var b in Buildings)
            {
                b.ServedToday = 0; b.TurnedAway = 0; b.SoldToday = 0; b.Produced = 0; b.Sold = 0;
                if (b.Type == BuildingType.Trucking && b.Open) truckLeft[b.Id] = 0;
            }
        }

        void ReadyTrucks()
        {
            foreach (var b in Buildings)
                if (b.Type == BuildingType.Trucking && b.Open)
                {
                    int drivers = 0;
                    foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null && c.AtWork && c.Role == Role.TruckDriver) drivers++; }
                    truckLeft[b.Id] = drivers * Balance.TruckCapacity;
                }
        }

        /// <summary>Hauling a shipment: a trucking depot in town if one has drivers free, otherwise an outside haulier.</summary>
        double Freight(double value, int from, int to, Good g)
        {
            double fee;
            int depot = -1;
            foreach (var kv in truckLeft) if (kv.Value >= value) { depot = kv.Key; break; }
            if (depot >= 0)
            {
                truckLeft[depot] -= value;
                fee = value * Balance.FreightLocal;
                var t = GetBuilding(depot);
                t.Cash += fee; t.Revenue += fee; t.Customers++;
            }
            else { fee = value * Balance.FreightOutside; Trade.Freight += fee; FreightOutsideMonth += fee; }
            if (Shipments.Count < 200 && value > 300) Shipments.Add(new Shipment { From = from, To = to, Good = g, Hour = 7 + Rand() * 10 });
            return fee;
        }
        [NonSerialized] public double FreightOutsideMonth;

        void Produce()
        {
            float season = new[] { 0.35f, 1.0f, 1.3f, 1.25f }[Season];
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (!b.Open || !IsBusiness(b) || !Connected(b)) continue;
                int present = PresentStaff(b);
                if (b.Type == BuildingType.Office)
                {
                    double bill = 0;
                    foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null && c.AtWork) bill += Defs.Get(c.Role).Billing * (1 + Math.Min(0.4, c.Experience * 0.015)); }
                    double overhead = bill * Balance.OfficeOverhead;
                    b.Cash += bill - overhead; b.Revenue += bill; b.OtherCosts += overhead;
                    Trade.Services += bill; Trade.Imports += overhead;
                    b.CapUsed += present; b.CapTotal += present;
                    continue;
                }
                if (!d.Makes.HasValue) continue;
                var g = d.Makes.Value;
                float potential = present * d.Output * (b.Type == BuildingType.Farm ? season : 1f);
                b.CapTotal += potential;
                float room = Math.Max(0, d.StockCap - b.Stock[(int)g]);
                float made = Math.Min(potential, room);
                if (d.Input.HasValue && d.InputPer > 0) made = Math.Min(made, b.Stock[(int)d.Input.Value] / d.InputPer);
                if (made <= 0) continue;
                if (d.Input.HasValue) b.Stock[(int)d.Input.Value] -= made * d.InputPer;
                b.Stock[(int)g] += made;
                b.Produced = made; b.CapUsed += made;
            }
        }

        /// <summary>What a business wants to buy today, and of what.</summary>
        bool Needs(Building b, out Good g, out float qty)
        {
            var d = b.Def; g = Good.Crops; qty = 0;
            if (d.Sells.HasValue)
            {
                g = d.Sells.Value;
                float s = b.Stock[(int)g];
                if (g == Good.Cars) return false;   // dealers order cars themselves (DealersDaily)
                else if (s < d.StockCap * 0.5f) qty = d.StockCap * 0.85f - s;
            }
            else if (d.Input.HasValue)
            {
                g = d.Input.Value;
                int staff = 0; foreach (var j in d.Jobs) staff += DesiredStaff(b, j);
                float target = Math.Max(1, staff) * d.Output * d.InputPer * 5f;
                if (d.Makes.HasValue) target = Math.Min(target, d.StockCap * d.InputPer * 0.5f + 1);
                if (b.Stock[(int)g] < target * 0.5f) qty = target - b.Stock[(int)g];
            }
            return qty > 0.01f;
        }

        void Restock()
        {
            foreach (var b in Buildings.OrderBy(x => Rand()).ToList())
            {
                if (!b.Open || !IsBusiness(b) || !Connected(b)) continue;
                Good g; float qty;
                if (!Needs(b, out g, out qty)) continue;
                Buy(b, g, qty);
            }
        }

        /// <summary>What a business can spend on stock: a shop keeps back this month's pay and loan payments (a plant buys what
        /// it needs to keep working, and borrows for payroll if it must).</summary>
        double Spendable(Building b)
        {
            if (!b.Def.Sells.HasValue) return Math.Max(0, b.Cash);
            double pay = 0; foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null) pay += c.Wage; }
            return Math.Max(0, b.Cash - pay * 0.5 - LoanPayments(b));
        }

        /// <summary>Buys stock from the cheapest makers in town that have it, importing the rest if that's cheaper or nobody local has it.</summary>
        float Buy(Building buyer, Good g, float qty)
        {
            float got = 0;
            double imp = ImportPrice(g) * (1 + Balance.FreightOutside);
            var sellers = Buildings.Where(s => s != buyer && s.Open && s.Def.Makes == g && s.Stock[(int)g] > 0.5f && Connected(s)).OrderBy(s => s.Price).ToList();
            foreach (var s in sellers)
            {
                if (got >= qty - 0.01f) break;
                if (s.Price * (1 + Balance.FreightLocal) > imp) break;
                float q = Math.Min(qty - got, s.Stock[(int)g]);
                double afford = Spendable(buyer) / (s.Price * (1 + Balance.FreightOutside));
                q = (float)Math.Min(q, afford);
                if (g == Good.Cars) q = (float)Math.Floor(q);
                if (q <= 0.01f) break;
                double value = q * s.Price;
                double fee = Freight(value, s.Id, buyer.Id, g);
                buyer.Cash -= value + fee; buyer.Supplies += value + fee;
                s.Cash += value; s.Revenue += value; s.Customers++; s.SoldToday += (int)Math.Ceiling(q);
                s.Stock[(int)g] -= q; buyer.Stock[(int)g] += q; got += q;
                buyer.UnitCost = (value + fee) / q;
            }
            if (got < qty - 0.01f)
            {
                double price = ImportPrice(g);
                float q = qty - got;
                double afford = Spendable(buyer) / (price * (1 + Balance.FreightOutside));
                q = (float)Math.Min(q, afford);
                if (g == Good.Cars) q = (float)Math.Floor(q);
                if (q > 0.01f)
                {
                    double value = q * price;
                    double fee = Freight(value, -1, buyer.Id, g);
                    buyer.Cash -= value + fee; buyer.Supplies += value + fee;
                    buyer.Stock[(int)g] += q; got += q;
                    Trade.Imports += value; ImportsByGood[(int)g] += value;
                    buyer.UnitCost = (value + fee) / q;
                }
            }
            return got;
        }
        [NonSerialized] public double[] ImportsByGood = new double[Defs.GoodCount];
        [NonSerialized] public double[] ExportsByGood = new double[Defs.GoodCount];

        /// <summary>Makers ship what they can't sell in town out on the highway.</summary>
        void Exports()
        {
            foreach (var b in Buildings)
            {
                var d = b.Def;
                if (!b.Open || !d.Makes.HasValue || !Connected(b)) continue;
                var g = d.Makes.Value;
                // what the shops here haven't bought goes out on the highway every few days, keeping a little back for them
                float s = b.Stock[(int)g];
                if (s < d.StockCap * 0.3f) continue;
                float q = s - d.StockCap * 0.2f;
                double value = q * World(g);
                double fee = Freight(value, b.Id, -1, g);
                b.Stock[(int)g] -= q;
                b.Cash += value - fee; b.Revenue += value; b.OtherCosts += fee;
                Trade.Exports += value; ExportsByGood[(int)g] += value;
            }
        }

        // ================================================================== every month
        /// <summary>Pay day: last month's wages, less income tax.</summary>
        void Payroll()
        {
            foreach (var b in Buildings.ToList())
            {
                if (b.Employees.Count == 0) continue;
                if (b.Id == TerminalId) { PayCommuters(b); continue; }
                bool pub = b.Def.Cat == Category.Public;
                double total = 0;
                var staff = b.Employees.Select(GetCitizen).Where(c => c != null).ToList();
                foreach (var c in staff) total += c.Wage * Math.Min(1.0, c.DaysWorked / (double)Balance.DaysPerMonth);
                if (!pub && b.Cash < total)
                {
                    double need = total - b.Cash + MonthlyCosts(b) * 0.5;
                    if (!BusinessLoan(b, need))
                    {
                        CloseBusiness(b, "it couldn't make payroll");
                        // what's in the account goes to the staff, a share each
                        double share = Math.Max(0, b.Cash) / Math.Max(1, staff.Count);
                        foreach (var c in staff) { var h = HouseholdOf(c); if (h != null) HouseholdEarns(h, share, HouseholdBook.Wages); }
                        b.Cash = Math.Min(0, b.Cash);
                        continue;
                    }
                }
                foreach (var c in staff)
                {
                    double pay = c.Wage * Math.Min(1.0, c.DaysWorked / (double)Balance.DaysPerMonth);
                    c.DaysWorked = 0;
                    if (pay <= 0) continue;
                    double tax = pay * Policy.IncomeTax;
                    var h = HouseholdOf(c);
                    if (h != null) { HouseholdEarns(h, pay, HouseholdBook.Wages); HouseholdPays(h, tax, HouseholdBook.Taxes); }
                    Treasury += tax; Book.IncomeTax += tax;
                    if (pub) { Treasury -= pay; Book.PublicWages += pay; b.WageBill += pay; }
                    else { b.Cash -= pay; b.WageBill += pay; }
                }
            }
        }

        void BusinessMonthly()
        {
            foreach (var b in Buildings.ToList())
            {
                if (!IsBusiness(b) || !b.Built) continue;
                var d = b.Def;
                if (b.Closed) { b.ClosedDays += Balance.DaysPerMonth; ShiftBooks(b); continue; }
                double util = d.TotalJobs * Balance.UtilitiesJob;
                b.Cash -= util; b.OtherCosts += util; Trade.Utilities += util;
                double fix = b.Value * Balance.Maintenance / 12;
                b.Cash -= fix; b.OtherCosts += fix; if (b.Type != BuildingType.Construction) Repairs(fix); else Trade.Imports += fix;
                double ptax = b.Value * Policy.PropertyTax / 12;
                b.Cash -= ptax; b.TaxPaid += ptax; Treasury += ptax; Book.PropertyTax += ptax;
                double profit = b.Revenue - b.WageBill - b.Supplies - b.TaxPaid - b.OtherCosts;
                if (profit > 0)
                {
                    double t = profit * Policy.BusinessTax;
                    b.Cash -= t; b.TaxPaid += t; Treasury += t; Book.BusinessTax += t; profit -= t;
                }
                b.LastUse = b.CapTotal > 0 ? Math.Min(1, b.CapUsed / b.CapTotal) : 0;
                ShiftBooks(b);
                b.LastProfit = profit;
                // a month counts as a loss when it really lost money, not when it came out about even
                if (profit >= -Math.Max(300, b.LastRevenue * 0.015)) b.LossMonths = 0; else b.LossMonths++;
                Succession(b);
                if (b.Closed) continue;
                // owner's draw: what's above a cushion of two months' costs and loan payments
                double cushion = Cushion(b);
                if (b.Cash > cushion)
                {
                    double draw = (b.Cash - cushion) * 0.75;
                    var oh = b.Owner == Owner.Household ? GetHousehold(b.OwnerId) : null;
                    if (oh != null) { b.Cash -= draw; HouseholdEarns(oh, draw, HouseholdBook.Business); }
                }
                if (b.Cash < 0 && !BusinessLoan(b, -b.Cash + MonthlyCosts(b))) { CloseBusiness(b, "it ran out of money"); continue; }
                // a farm loses money every winter; give it the year
                int patience = b.Type == BuildingType.Farm ? 9 : 5;
                // a property firm between sales isn't failing while it has houses going up or on the market
                bool selling = b.Type == BuildingType.Realty && Portfolio(b).Any(o => !o.Built || o.ForSale);
                if (b.LossMonths >= patience && b.Cash < MonthlyCosts(b) && !selling) { CloseBusiness(b, "it lost money " + (patience == 5 ? "five" : "nine") + " months running"); continue; }
                Staffing(b);
                Pricing(b);
            }
            FreightOutsideMonth = 0;
        }

        void ShiftBooks(Building b)
        {
            b.LastRevenue = b.Revenue; b.LastWages = b.WageBill; b.LastSupplies = b.Supplies; b.LastTax = b.TaxPaid; b.LastOther = b.OtherCosts;
            b.LastCustomers = b.Customers; b.LastLost = b.Lost;
            b.Revenue = b.WageBill = b.Supplies = b.TaxPaid = b.OtherCosts = 0; b.Customers = b.Lost = 0;
            b.CapUsed = b.CapTotal = 0;
        }

        /// <summary>When the owner of a family business dies, retires or moves away, family carries it on (or just keeps owning it
        /// with staff running it); with no family left it's sold to someone in town.</summary>
        void Succession(Building b)
        {
            if (b.Owner != Owner.Household) return;
            var oh = GetHousehold(b.OwnerId);
            if (oh == null) { TransferBusiness(b); return; }
            bool ownerWorks = b.Employees.Select(GetCitizen).Any(c => c != null && c.Owner && c.HouseholdId == b.OwnerId);
            if (ownerWorks) return;
            {
                var heir = Members(oh).FirstOrDefault(m => m.WorkId == b.Id && !m.IsChild) ??
                           Members(oh).FirstOrDefault(m => m.InLaborForce && m.WorkId < 0 && QualifiesFor(m, b) != Role.None);
                if (heir != null && QualifiesFor(heir, b) != Role.None || heir != null && heir.WorkId == b.Id)
                {
                    heir.Owner = true;
                    if (heir.WorkId != b.Id) Hire(b, heir, QualifiesFor(heir, b), false);
                    AddLog(heir.Name + " took over the family's " + d(b) + ".", LogKind.Info, heir.Id, true);
                }
                // otherwise the family still owns it and the staff run it
            }
        }
        static string d(Building b) { return b.Def.Name.ToLower(); }

        public void CloseBusiness(Building b, string why)
        {
            if (b.Closed) return;
            int n = b.Employees.Count;
            var who = Ref(b);
            AddLog(char.ToUpperInvariant(who[0]) + who.Substring(1) + " closed" + (why != null ? ": " + why : "") + ". " + (n == 0 ? "Nobody worked there." : n == 1 ? "One person lost their job." : n + " people lost their jobs."), LogKind.Bad, b.Id);
            foreach (var id in b.Employees.ToList())
            {
                var c = GetCitizen(id);
                if (c == null) continue;
                Note(c, "Lost their job when " + Ref(b) + " closed.");
                c.Owner = false; LeaveJob(c, null);
            }
            // the bank writes off what it lent the business and takes the building; a property firm's homes go with it
            foreach (var l in Loans.ToList()) if (PaidBy(l, b)) Loans.Remove(l);
            if (b.Type == BuildingType.Realty)
                foreach (var o in Portfolio(b).ToList()) BankTakes(o);
            if (b.Cash > 0)
            {
                var oh = b.Owner == Owner.Household ? GetHousehold(b.OwnerId) : null;
                if (oh != null) HouseholdEarns(oh, b.Cash, HouseholdBook.Business); else BankCapital += b.Cash;
            }
            b.Cash = 0; b.Closed = true; b.ClosedDays = 0; b.Staffing = 0.5f;
            if (b.Owner != Owner.Town) BankTakes(b);
            if (why != null && !why.Contains("left town") && !why.Contains("nobody in town")) { PrevFailDay[(int)b.Type] = LastFailDay[(int)b.Type]; LastFailDay[(int)b.Type] = Day; }
            MapVersion++;
        }

        /// <summary>Does each worker at a plant bring in more than they're paid: what the goods sold for, less the materials, per head.</summary>
        bool HandsPay(Building b)
        {
            int n = b.Employees.Count;
            if (n == 0) return false;
            double pay = 0; foreach (var id in b.Employees) { var c = GetCitizen(id); if (c != null) pay += c.Wage; }
            return (b.LastRevenue - b.LastSupplies) > pay * 1.05;
        }

        double LeastWage(Building b)
        {
            double w = double.MaxValue;
            foreach (var j in b.Def.Jobs) w = Math.Min(w, OfferWage(b, j.Role));
            return w == double.MaxValue ? 0 : w;
        }

        void Staffing(Building b)
        {
            var d = b.Def;
            float s = b.Staffing;
            float lostShare = b.LastLost / (float)Math.Max(1, b.LastCustomers + b.LastLost);
            if (b.Type == BuildingType.Office) s += b.LastProfit > 0 ? 0.15f : b.LossMonths >= 2 ? -0.15f : 0;
            else if (b.Type == BuildingType.Construction)
            {
                // a crew sized to the work on the books: what's left on its sites, over the next few months
                float backlog = 0; foreach (var o in Buildings) if (!o.Built && o.ContractorId == b.Id) backlog += o.Def.Work - o.Progress;
                float perMonth = Math.Max(1, b.Employees.Count) * Balance.DaysPerMonth;
                s += backlog > perMonth * 1.5f ? 0.2f : backlog < perMonth * 0.4f ? -0.25f : backlog < perMonth ? -0.1f : 0;
            }
            else if (b.Type == BuildingType.Realty) { PlanFirmStaff(b); s = 1f; }
            else if (b.Type == BuildingType.Trucking) s += FreightOutsideMonth > 500 ? 0.15f : b.LastUse < 0.4f ? -0.1f : 0;
            else if (b.Type == BuildingType.Bank) s += b.LastProfit > 3000 ? 0.15f : b.LastProfit < 0 ? -0.15f : 0;
            else if (d.Makes.HasValue)
            {
                float fill = b.Stock[(int)d.Makes.Value] / d.StockCap;
                bool inputs = !d.Input.HasValue || b.Stock[(int)d.Input.Value] > 0.5f;
                bool young = Day - b.BuiltDay < Balance.DaysPerYear / 2;
                if (fill > 0.9f) s -= 0.15f;
                else if (young && inputs && b.Cash > MonthlyCosts(b)) s += 0.15f;   // a new plant hires up to the size it was built for
                else if (b.LossMonths >= 2 && !HandsPay(b)) s -= 0.15f;   // cutting staff who earn more than they cost only deepens a loss
                else if (b.LastProfit >= 0 && inputs && b.LastUse > 0.8f) s += 0.15f;
                else if (!inputs) s -= 0.1f;
            }
            else
            {
                // turning customers away: more hands, if there's money to pay them and, for a service, enough custom turned away
                // to cover another wage
                bool worthIt = d.Serves == Service.None || b.LastLost * b.Price * TicketSize(b.Type) > LeastWage(b) * 0.8;
                // a shop turning people away with empty shelves needs stock, not staff
                if (d.Sells.HasValue && b.LastUse < 0.75f) worthIt = false;
                if (lostShare > 0.08f && worthIt && (b.LossMonths == 0 || (lostShare > 0.2f && b.Cash > MonthlyCosts(b)))) s += 0.17f;
                else if (b.LastUse < 0.45f || b.LossMonths >= 2) s -= 0.12f;
            }
            b.Staffing = Math.Max(0.25f, Math.Min(1f, s));
            TrimStaff(b, b.LossMonths >= 2 ? "business is losing money" : "there isn't enough work");
        }

        void Pricing(Building b)
        {
            var d = b.Def;
            float step = Balance.PriceStep;
            double baseP, floor;
            float lostShare = b.LastLost / (float)Math.Max(1, b.LastCustomers + b.LastLost);
            if (b.Type == BuildingType.CarDealer)
            {
                // about list price: up when customers are turned away or it's selling well, down when nobody's buying. A full
                // lot isn't a glut: it's what a dealer is for
                baseP = Defs.Retail[(int)Good.Cars];
                if (lostShare > 0.1f || b.LastCustomers >= 6) b.Price *= 1 + step * 0.5f;
                else if (b.LastCustomers <= 1) b.Price *= 1 - step * 0.5f;
                else if (b.LossMonths >= 2) b.Price *= 1 + step * 0.5f;
                b.Price = Math.Round(Math.Max(baseP * 0.98, Math.Min(baseP * 1.12, b.Price)), 2);
                return;
            }
            if (d.Sells.HasValue)
            {
                var g = d.Sells.Value;
                baseP = Defs.Retail[(int)g];
                floor = Math.Max(baseP * Balance.PriceLow, (b.UnitCost > 0 ? b.UnitCost : Defs.Wholesale[(int)g]) * 1.06);
                float fill = b.Stock[(int)g] / d.StockCap;
                if (lostShare > 0.12f) b.Price *= 1 + step;
                else if (fill > 0.7f && b.LastUse < 0.6f) b.Price *= 1 - step;
                else if (b.LossMonths >= 2) b.Price *= 1 + step * 0.5f;
                // a cheaper competitor pulls prices down
                var rival = Buildings.Where(o => o != b && o.Type == b.Type && o.Open).OrderBy(o => o.Price).FirstOrDefault();
                if (rival != null && rival.Price < b.Price * 0.95) b.Price *= 1 - step * 0.5f;
            }
            else if (d.Serves != Service.None)
            {
                baseP = d.ServicePrice;
                floor = baseP * Balance.PriceLow;
                if (lostShare > 0.12f) b.Price *= 1 + step;
                else if (b.LastUse < 0.45f) b.Price *= 1 - step;
                else if (b.LossMonths >= 2) b.Price *= 1 + step * 0.5f;
                else if (b.LastLost == 0 && b.LastUse < 0.75f && b.Price > baseP * 1.15) b.Price *= 1 - step * 0.5f;   // room to spare: ease back
            }
            else if (d.Makes.HasValue)
            {
                var g = d.Makes.Value;
                baseP = Defs.Wholesale[(int)g];
                floor = World(g) * 0.95;
                float fill = b.Stock[(int)g] / d.StockCap;
                if (fill > 0.6f) b.Price *= 1 - step;
                else if (fill < 0.2f) b.Price *= 1 + step;
                b.Price = Math.Min(b.Price, ImportPrice(g) * 0.99);
            }
            else return;
            b.Price = Math.Max(floor, Math.Min(baseP * Balance.PriceHigh, b.Price));
            b.Price = Math.Round(b.Price, 2);
        }
    }
}
