using System;
using System.Collections.Generic;
using System.Linq;

namespace RealLife.Simulation
{
    /// <summary>A bus stop: a road tile, and the kerb it stands on (towards the place it was put there for).</summary>
    public class BusStop
    {
        public int X, Y;
        public int SideX, SideY;
        public int Depot;
    }

    /// <summary>A bus route: a loop from the depot's own stop round its share of the stops and back.</summary>
    public class BusRoute
    {
        public int Depot;
        public List<int> Stops = new List<int>();
        public int Buses;
    }

    /// <summary>
    /// Buses. A depot with drivers sets up stops along the streets within its reach, one within a short walk of every home,
    /// shop, workplace and school, and one at the highway for those who work out of town; and splits them into a few loops
    /// out from the depot and back, a bus or two on each. People without a car of their own today take the bus to work, kids
    /// take it to school when it's beyond a walk, and anyone takes it to the shops. A family that can get everywhere it needs by
    /// bus, and hasn't much money, may do without a car. Riders buy a monthly pass.
    /// </summary>
    public partial class Town
    {
        public const int StopWalk = 7;           // the furthest people walk to a stop, in tiles
        public const int RideMin = 9;            // nearer than this they walk
        public const int RidersPerBus = 110;     // regular riders a bus can carry: a few dozen seats, several loops a day
        public const double FareAdult = 55, FareChild = 20;   // a month's pass

        [NonSerialized] public List<BusStop> BusStops = new List<BusStop>();
        [NonSerialized] public List<BusRoute> BusRoutes = new List<BusRoute>();
        [NonSerialized] public int BusNetVersion;          // goes up whenever the stops or routes change
        [NonSerialized] string busKey;
        [NonSerialized] int busDay = -1, busMap = -1;
        [NonSerialized] string busShape;
        [NonSerialized] Dictionary<int, int> stopOf;
        [NonSerialized] public int BusRiders;              // regular riders today: to work, school or college
        [NonSerialized] public double LastFares;

        /// <summary>The stops and routes, rebuilt when the streets, the buildings or the depots' drivers change.</summary>
        public void EnsureBusNet()
        {
            // looked at once a day, or when the map changes: plenty, and it's asked a lot
            if (BusStops != null && busDay == Day && busMap == MapVersion) return;
            busDay = Day; busMap = MapVersion;
            var sb = new System.Text.StringBuilder();
            sb.Append(MapVersion);
            foreach (var b in Buildings)
                if (b.Type == BuildingType.BusDepot && b.Built && b.Open && b.Employees.Count > 0) sb.Append('|').Append(b.Id).Append(':').Append(b.Employees.Count);
            string key = sb.ToString();
            if (key == busKey && BusStops != null) return;
            busKey = key;
            BuildBusNet();
            // only a real change to the stops or routes counts (most map changes don't touch them)
            string shape = NetShape();
            if (shape != busShape) { busShape = shape; BusNetVersion++; }
        }

        string NetShape()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var s in BusStops) sb.Append(s.X).Append(',').Append(s.Y).Append(',').Append(s.Depot).Append(';');
            foreach (var r in BusRoutes) { sb.Append('|').Append(r.Buses).Append(':'); foreach (var i in r.Stops) sb.Append(i).Append(','); }
            return sb.ToString();
        }

        void BuildBusNet()
        {
            if (BusStops == null) BusStops = new List<BusStop>();
            if (BusRoutes == null) BusRoutes = new List<BusRoute>();
            if (stopOf == null) stopOf = new Dictionary<int, int>();
            BusStops.Clear(); BusRoutes.Clear(); stopOf.Clear();
            bool highway = false;
            foreach (var d in Buildings.Where(b => b.Type == BuildingType.BusDepot && b.Built && b.Open && b.Employees.Count > 0 && Connected(b)).OrderBy(b => b.Id).ToList())
            {
                int ax, ay;
                if (!AccessTile(d, out ax, out ay)) continue;
                int first = BusStops.Count;
                int hub = AddStop(d, ax, ay, d.Id);
                // a stop in front of every place in reach that has none within a walk, nearest the depot first
                var places = Buildings.Where(b => b != d && b.Built && (b.Def.Homes > 0 || IsBusiness(b) || b.Def.Cat == Category.Public) && Connected(b) && Distance(b, d) <= d.Def.Radius)
                                      .OrderBy(b => Distance(b, d)).ThenBy(b => b.Id);
                foreach (var b in places)
                {
                    int bx, by;
                    if (!AccessTile(b, out bx, out by) || NearestStop(bx, by, d.Id, first) >= 0) continue;
                    AddStop(b, bx, by, d.Id);
                }
                // and one at the highway, for the jobs out of town
                var t = Terminal;
                int tx, ty;
                if (!highway && t != null && AccessTile(t, out tx, out ty) && Connected(t))
                {
                    if (NearestStop(tx, ty, d.Id, first) < 0) AddStop(t, tx, ty, d.Id);
                    highway = true;
                }
                // loops: the stops split by direction from the depot, each visited nearest-next and back
                var mine = Enumerable.Range(first, BusStops.Count - first).Where(i => i != hub).ToList();
                int drivers = d.Employees.Count;
                int loops = Math.Max(1, Math.Min(drivers, (int)Math.Ceiling(mine.Count / 12.0)));
                var hs = BusStops[hub];
                mine.Sort((p, q) => Angle(BusStops[p], hs).CompareTo(Angle(BusStops[q], hs)));
                var made = new List<BusRoute>();
                for (int r = 0; r < loops; r++)
                {
                    var part = mine.Skip(r * mine.Count / loops).Take((r + 1) * mine.Count / loops - r * mine.Count / loops).ToList();
                    var route = new BusRoute { Depot = d.Id };
                    route.Stops.Add(hub);
                    int at = hub;
                    while (part.Count > 0)
                    {
                        int best = 0, bd = int.MaxValue;
                        for (int i = 0; i < part.Count; i++)
                        {
                            int dd = Math.Abs(BusStops[part[i]].X - BusStops[at].X) + Math.Abs(BusStops[part[i]].Y - BusStops[at].Y);
                            if (dd < bd) { bd = dd; best = i; }
                        }
                        at = part[best]; part.RemoveAt(best);
                        route.Stops.Add(at);
                    }
                    if (route.Stops.Count > 1) made.Add(route);
                }
                // the drivers shared out, the longest loops getting the spare ones
                for (int i = 0; i < made.Count; i++) made[i].Buses = drivers / Math.Max(1, made.Count);
                int spare = drivers - made.Sum(r => r.Buses);
                foreach (var r in made.OrderByDescending(r => r.Stops.Count)) { if (spare <= 0) break; r.Buses++; spare--; }
                BusRoutes.AddRange(made.Where(r => r.Buses > 0));
            }
        }

        static double Angle(BusStop s, BusStop hub) { return Math.Atan2(s.Y - hub.Y, s.X - hub.X); }

        int AddStop(Building b, int x, int y, int depot)
        {
            int side = (b.Facing + 2) % 4;   // the road's on the facing side, so the building's on the other
            BusStops.Add(new BusStop { X = x, Y = y, SideX = StepX(side), SideY = StepY(side), Depot = depot });
            return BusStops.Count - 1;
        }

        int NearestStop(int x, int y, int depot, int from)
        {
            int best = -1, bd = StopWalk + 1;
            for (int i = from; i < BusStops.Count; i++)
            {
                var s = BusStops[i];
                if (depot >= 0 && s.Depot != depot) continue;
                int d = Math.Abs(s.X - x) + Math.Abs(s.Y - y);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        /// <summary>The stop a short walk from a building (-1: none).</summary>
        public int StopNear(Building b)
        {
            if (b == null) return -1;
            EnsureBusNet();
            if (BusStops.Count == 0) return -1;
            int s;
            if (stopOf.TryGetValue(b.Id, out s)) return s;
            int x, y;
            s = AccessTile(b, out x, out y) ? NearestStop(x, y, -1, 0) : -1;
            stopOf[b.Id] = s;
            return s;
        }

        public bool BusServes(Building a) { return StopNear(a) >= 0; }

        /// <summary>Can you get from one to the other by bus: a stop near each, on the same depot's routes.</summary>
        public bool BusConnects(Building a, Building b)
        {
            int sa = StopNear(a), sb = StopNear(b);
            return sa >= 0 && sb >= 0 && BusStops[sa].Depot == BusStops[sb].Depot;
        }

        /// <summary>Could the family get everywhere it has to without a car: every job within a walk or on the buses.</summary>
        public bool BusCovers(Household h, Building home)
        {
            if (home == null || StopNear(home) < 0) return false;
            foreach (var c in Members(h))
            {
                if (c.WorkId < 0) continue;
                var w = GetBuilding(c.WorkId);
                if (w == null || Distance(home, w) <= RideMin) continue;
                if (!BusConnects(home, w)) return false;
            }
            return true;
        }

        /// <summary>Riders a day the depots' buses have room for.</summary>
        public int BusCapacity()
        {
            int n = 0;
            foreach (var b in Buildings) if (b.Type == BuildingType.BusDepot && b.Open) n += b.Employees.Count;
            return n * RidersPerBus;
        }

        /// <summary>Who rides today: those with a long way to work and no car of their own for the day (the family's cars go to
        /// the longest drives), and kids and students with a long way to school.</summary>
        void BusDaily()
        {
            BusRiders = 0;
            EnsureBusNet();
            bool any = BusStops.Count > 0;
            var adults = new List<Citizen>();
            foreach (var h in Households)
            {
                var home = HomeOf(h);
                var ms = Members(h);
                if (!any || home == null) { foreach (var c in ms) c.RidesBus = false; continue; }
                int cars = UsableCars(h);
                adults.Clear();
                foreach (var c in ms)
                {
                    c.RidesBus = false;
                    var to = Commutes(c) ? Terminal : c.WorkId >= 0 ? GetBuilding(c.WorkId) : c.SchoolId >= 0 ? GetBuilding(c.SchoolId) : null;
                    if (to == null || Distance(home, to) <= RideMin) continue;
                    if (c.IsChild || c.WorkId < 0) { if (BusConnects(home, to)) c.RidesBus = true; continue; }
                    adults.Add(c);
                }
                adults.Sort((a, b) => (Commutes(b) ? 1 : 0).CompareTo(Commutes(a) ? 1 : 0));
                foreach (var c in adults)
                {
                    if (cars > 0) { cars--; continue; }
                    if (BusConnects(home, Commutes(c) ? Terminal : GetBuilding(c.WorkId))) c.RidesBus = true;
                }
                foreach (var c in ms) if (c.RidesBus) BusRiders++;
            }
        }

        /// <summary>The month's passes: the riders' families pay the town.</summary>
        void BusFares()
        {
            double total = 0;
            foreach (var h in Households)
            {
                double f = 0;
                foreach (var c in Members(h)) if (c.RidesBus) f += c.IsChild ? FareChild : FareAdult;
                if (f <= 0) continue;
                f = Math.Min(f, Math.Max(0, h.Cash));
                HouseholdPays(h, f, HouseholdBook.Transport);
                total += f;
            }
            Treasury += total; Book.Fares += total;
            LastFares = total;
        }
    }
}
