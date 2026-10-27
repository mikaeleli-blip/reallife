using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>
    /// The region beyond the highway. People who can't find work in town drive to jobs in the towns around, for less than a
    /// job here would pay once you count the drive; they take a job in town when one comes up. Their pay comes in from outside.
    /// While there's room for more commuters, families move here for the homes: that's how an empty town gets its first people.
    /// </summary>
    public partial class Town
    {
        /// <summary>How many people here the jobs around can take: the region only has so much work for one small town.</summary>
        public int CommuteCap() { return 40 + Population / 3; }

        public int Commuters() { var t = Terminal; return t != null ? t.Employees.Count : 0; }

        public bool Commutes(Citizen c) { return c.WorkId >= 0 && c.WorkId == TerminalId; }

        /// <summary>What a job out of town pays, by schooling, plus a little for experience.</summary>
        public static double CommuteWage(Edu edu, float experience)
        {
            double w;
            switch (edu)
            {
                case Edu.HighSchool: w = 2800; break;
                case Edu.Trade: w = 3400; break;
                case Edu.Degree: w = 4400; break;
                case Edu.Medical: w = 7000; break;
                default: w = 2300; break;
            }
            return Math.Round(w * (1 + Math.Min(0.35, experience * 0.012)) / 10) * 10;
        }

        /// <summary>A car, a home on a street that leads to the highway.</summary>
        public bool CanCommute(Citizen c)
        {
            var t = Terminal;
            if (t == null || c.IsChild || c.Retired) return false;
            var home = HomeOf(c);
            return home != null && Connected(home) && CarCount(HouseholdOf(c)) > 0;   // in the garage, they get a lift
        }

        void StartCommuting(Citizen c, bool silent)
        {
            Hire(Terminal, c, Role.Commuter, true);
            if (!silent) Note(c, "Took a job out of town.");
        }

        /// <summary>People who've looked here a couple of weeks and found nothing take a job out of town, if there's one.</summary>
        void CommuteDaily()
        {
            var t = Terminal;
            if (t == null) return;
            int room = CommuteCap() - t.Employees.Count;
            if (room <= 0) return;
            foreach (var c in People)
            {
                if (room <= 0) break;
                if (!c.InLaborForce || c.WorkId >= 0 || c.JoblessDays < 2 || !CanCommute(c)) continue;
                StartCommuting(c, false);
                room--;
            }
        }

        /// <summary>Pay day for commuters: their pay comes in from outside, and the town taxes it like any other.</summary>
        void PayCommuters(Building t)
        {
            foreach (var c in t.Employees.Select(GetCitizen).Where(c => c != null).ToList())
            {
                c.Wage = CommuteWage(c.Edu, c.Experience);
                double pay = c.Wage * Math.Min(1.0, c.DaysWorked / (double)Balance.DaysPerMonth);
                c.DaysWorked = 0;
                if (pay <= 0) continue;
                double tax = pay * Policy.IncomeTax;
                var h = HouseholdOf(c);
                if (h != null) { HouseholdEarns(h, pay, HouseholdBook.Wages); HouseholdPays(h, tax, HouseholdBook.Taxes); }
                Treasury += tax; Book.IncomeTax += tax;
                Trade.Commuting += pay; t.WageBill += pay;
                // and someone whose family no longer has a car can't get there any more
                if (h != null && CarCount(h) == 0) { LeaveJob(c, null); Note(c, "Lost the job out of town: no way to get there."); }
            }
        }

        // ================================================================== settlers
        [NonSerialized] public int SettlersShort;   // families who wanted to buy a house here last month but found none
        [NonSerialized] public int SettlersRentShort;   // and those who'd have rented a flat

        /// <summary>While the jobs around have room, families move here for a home of their own and drive to work: a house for
        /// sale if they can get a mortgage, a place to rent if not. Those who find nothing are the demand for new homes.</summary>
        void Settlers()
        {
            SettlersShort = 0; SettlersRentShort = 0;
            if (!Policy.Immigration || Terminal == null || mainComp < 0) return;
            int looking = People.Count(c => c.InLaborForce && c.WorkId < 0);
            int room = CommuteCap() - Commuters() - looking;
            if (room < 2) return;
            int n = Math.Min(Math.Min(room / 2, 5 + Population / 30), 10);
            // dear housing puts people off moving here
            n = (int)Math.Round(n * Math.Max(0, Math.Min(1, (2.1 - HousingIndex) / 0.8)));
            var market = Buildings.Where(b => b.ForSale && b.Built && b.Households.Count == 0 && Connected(b)).OrderBy(b => b.Value).ToList();
            for (int i = 0; i < n; i++)
            {
                if (!Chance(0.8f)) continue;
                bool family = Chance(0.7f);
                float r = Rand();
                var edu = r < 0.18f ? Edu.None : r < 0.58f ? Edu.HighSchool : r < 0.8f ? Edu.Trade : Edu.Degree;
                // what they'd earn: one commuter, or two in a couple
                double income = CommuteWage(edu, 8) * (family ? 1.8 : 1);
                Building buy = null;
                if (family)
                    foreach (var b in market)
                    {
                        double pay = Annuity(b.Value * 0.9, Balance.MortgageRate, Balance.MortgageMonths) + b.Value * Policy.PropertyTax / 12;
                        if (pay <= income * 0.36) { buy = b; break; }
                    }
                Building rent = buy == null ? FindHome(family ? 3 : 1, income * (1 - Policy.IncomeTax), 0.42) : null;
                if (buy == null && rent == null)
                {
                    // nothing now: they'd come for a new one at today's prices, if they could afford it
                    var house = Defs.Get(BuildingType.House);
                    double price = house.Value * HousingIndex;
                    bool could = family ? Annuity(price * 0.9, Balance.MortgageRate, Balance.MortgageMonths) + price * Policy.PropertyTax / 12 <= income * 0.36
                                        : Defs.Get(BuildingType.Apartments).Rent * HousingIndex <= income * (1 - Policy.IncomeTax) * 0.42;
                    if (could) { if (family) SettlersShort++; else SettlersRentShort++; }
                    continue;
                }
                var h = Newcomers(edu, family);
                if (CarCount(h) == 0) GiveCar(h, edu);
                h.Fuel = Math.Max(h.Fuel, 1);
                if (buy != null)
                {
                    h.Cash += buy.Value * 0.14 + Rand() * 30000;   // what they got for the place they sold
                    market.Remove(buy);
                    BuyHome(h, buy, true);
                }
                else MoveIn(h, rent, false);
                // they came with work lined up
                foreach (var c in Members(h))
                    if (c.InLaborForce && c.WorkId < 0 && Commuters() < CommuteCap() && CanCommute(c)) StartCommuting(c, true);
                ArrivalsMonth += h.Members.Count;
                var a = Members(h).First();
                AddLog("The " + h.Name + " " + (family ? "family" : "household") + " moved to town" + (buy != null ? " and bought a house" : "") +
                       ", driving to work out of town.", LogKind.Good, a.Id, true);
                Note(a, "Moved to " + Name + (buy != null ? " and bought a house." : "."));
            }
            Wants[(int)BuildingType.House] += SettlersShort;
            Wants[(int)BuildingType.Apartments] += SettlersRentShort;
            DevNote("settlers: room " + room + " tried " + n + " short " + SettlersShort + "/" + SettlersRentShort + " market " + market.Count + " vacant " + VacantUnits() +
                    " building " + Buildings.Count(b => !b.Built && b.Def.Homes > 0) + " idx " + HousingIndex.ToString("0.00"));
        }

        /// <summary>Houses for the families who want to move here: built by a property firm in town if one can take them on,
        /// otherwise by the bank, which sells them on (unsold, it lets them).</summary>
        void HousesForSettlers(ref int building, int buildCap)
        {
            if (SettlersShort <= 0) return;
            int going = Buildings.Count(b => !b.Built && b.Type == BuildingType.House && b.ForSale);
            int cap = 8 + Population / 8;
            for (int i = 0; i < SettlersShort && going < cap && building < buildCap; i++)
            {
                var firm = HousingFirm(BuildingType.House);
                Building b;
                if (firm != null) b = Develop(BuildingType.House, null, firm);
                else
                {
                    int x, y;
                    if (!FindLot(BuildingType.House, out x, out y)) { LotShort[(int)Zone.Residential] = true; break; }
                    b = AddBuilding(BuildingType.House, x, y, FindFacing(x, y, 1, 1), false);
                    BankTakes(b);
                    b.ForSale = true;
                    StartConstruction(b, null);
                    WorkStarted += b.Def.Work;
                }
                if (b == null) break;
                going++; building++;
            }
        }
    }
}
