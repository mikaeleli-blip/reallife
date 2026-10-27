using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// Where the cars stand through the day. Each car's day is a string of stays between its drives: on the family's
    /// driveway, in a spot in a parking lot near where its driver went (a public car park, or a grocery store's own lot), or
    /// at the kerb or out of town, where you don't see it. Spots are booked when the day is planned, so two cars never share
    /// one; a driver who finds nothing free pulls up at the kerb.
    /// </summary>
    sealed class CarParks
    {
        internal sealed class Stay
        {
            public int Car;
            public int Building = -1;   // where it stands (-1: at the kerb or out of town, not shown)
            public int Spot = -1;       // which of that building's spots
            public bool Home;
            public float From, To = 24f;
        }

        sealed class Lot
        {
            public Building B;
            public List<Vector3> Local;
            public List<Stay>[] Booked;
        }

        Town town;
        readonly Dictionary<int, Lot> lots = new Dictionary<int, Lot>();
        readonly List<Lot> publicLots = new List<Lot>();
        readonly Dictionary<int, List<Stay>> byCar = new Dictionary<int, List<Stay>>();
        readonly Dictionary<int, List<Stay>> byBuilding = new Dictionary<int, List<Stay>>();

        /// <summary>A new day: the lots as they are, nothing booked.</summary>
        public void Reset(Town t)
        {
            town = t;
            lots.Clear(); publicLots.Clear(); byCar.Clear(); byBuilding.Clear();
            foreach (var b in t.Buildings)
            {
                if (!b.Built) continue;
                if (b.Type != BuildingType.ParkingLot && !(HasOwnLot(b.Type) && !b.Closed)) continue;
                var l = new Lot { B = b, Local = LocalSpots(b) };
                l.Booked = new List<Stay>[l.Local.Count];
                for (int i = 0; i < l.Booked.Length; i++) l.Booked[i] = new List<Stay>();
                lots[b.Id] = l;
                if (b.Type == BuildingType.ParkingLot) publicLots.Add(l);
            }
        }

        /// <summary>Shops whose own car park its customers use.</summary>
        public static bool HasOwnLot(BuildingType t)
        {
            return t == BuildingType.GroceryStore || t == BuildingType.DiscountStore || t == BuildingType.Supermarket || t == BuildingType.WarehouseClub;
        }

        // ================================================================== geometry
        /// <summary>A building's parking spots in its own frame (x, z, yaw).</summary>
        public static List<Vector3> LocalSpots(Building b)
        {
            var d = b.Def;
            bool sideways = b.Facing == 1 || b.Facing == 3;
            float w = (sideways ? d.H : d.W) * Nav.Tile, dep = (sideways ? d.W : d.H) * Nav.Tile;
            return BuildingArt.ParkingSpots(b.Type, w, dep, (uint)(b.Id * 2654435761u));
        }

        static float Depth(Building b) { var d = b.Def; bool sideways = b.Facing == 1 || b.Facing == 3; return (sideways ? d.W : d.H) * Nav.Tile; }

        /// <summary>A point in a building's frame, in the world.</summary>
        public static Vector3 World(Building b, float x, float z)
        {
            var rot = Quaternion.Euler(0, WorldView.Yaw(b.Facing), 0);
            return WorldView.FootprintCenter(b) + rot * new Vector3(x, 0.07f, z);
        }

        public static float WorldYaw(Building b, float yaw) { return WorldView.Yaw(b.Facing) + yaw; }

        /// <summary>The way out of a spot to the street: through a car park's aisle, or straight down a driveway.</summary>
        public static List<Vector3> Leave(Building b, Vector3 spot)
        {
            float d = Depth(b);
            var pts = new List<Vector3> { World(b, spot.x, spot.y) };
            float az = BuildingArt.AisleZ(b.Type, d);
            if (!float.IsNaN(az))
            {
                pts.Add(World(b, spot.x, az));
                pts.Add(World(b, 0, az));
                pts.Add(World(b, 0, -d / 2 + 0.6f));
            }
            else pts.Add(World(b, spot.x, -d / 2 + 0.4f));
            return pts;
        }

        public static List<Vector3> Arrive(Building b, Vector3 spot) { var l = Leave(b, spot); l.Reverse(); return l; }

        // ================================================================== booking
        /// <summary>A spot for a car near a place, for a while: its own lot first, then the nearest public car park within a short
        /// walk. Null if there's nowhere free (they'll pull up at the kerb).</summary>
        public Stay Find(int car, Building dest, float from, float to)
        {
            Lot own;
            if (lots.TryGetValue(dest.Id, out own))
            {
                var s = Book(own, car, from, to);
                if (s != null) return s;
            }
            Lot best = null; float bd = float.MaxValue;
            Stay got = null;
            // nearest first: few enough lots to just look each time
            var tried = new List<Lot>();
            while (got == null)
            {
                best = null; bd = float.MaxValue;
                foreach (var l in publicLots)
                {
                    if (tried.Contains(l) || l.B == dest) continue;
                    float dd = town.Distance(l.B, dest);
                    if (dd <= Town.ParkWalk && dd < bd) { bd = dd; best = l; }
                }
                if (best == null) return null;
                tried.Add(best);
                got = Book(best, car, from, to);
            }
            return got;
        }

        Stay Book(Lot l, int car, float from, float to)
        {
            for (int i = 0; i < l.Booked.Length; i++)
            {
                bool free = true;
                foreach (var o in l.Booked[i]) if (o.From < to + 0.15f && from < o.To + 0.15f) { free = false; break; }
                if (!free) continue;
                var s = new Stay { Car = car, Building = l.B.Id, Spot = i, From = from, To = to };
                l.Booked[i].Add(s);
                Note(s);
                return s;
            }
            return null;
        }

        /// <summary>At home: on the driveway if it has a spot for this car, else somewhere you don't see (a garage, the street).</summary>
        public Stay Home(int car, Building home, int index, float from, float to)
        {
            var s = new Stay { Car = car, Home = true, From = from, To = to };
            if (home != null && home.Type == BuildingType.House && index >= 0 && index < LocalSpots(home).Count) { s.Building = home.Id; s.Spot = index; }
            Note(s);
            return s;
        }

        /// <summary>At the kerb, or out of town: not shown.</summary>
        public Stay Away(int car, float from, float to)
        {
            var s = new Stay { Car = car, From = from, To = to };
            Note(s);
            return s;
        }

        void Note(Stay s)
        {
            List<Stay> l;
            if (!byCar.TryGetValue(s.Car, out l)) byCar[s.Car] = l = new List<Stay>();
            l.Add(s);
            if (s.Building < 0) return;
            if (!byBuilding.TryGetValue(s.Building, out l)) byBuilding[s.Building] = l = new List<Stay>();
            l.Add(s);
        }

        // ================================================================== what's where now
        /// <summary>Is the car at home now (true for a car that hasn't gone anywhere today).</summary>
        public bool AtHome(int car, float hour)
        {
            List<Stay> l;
            if (!byCar.TryGetValue(car, out l)) return true;
            foreach (var s in l) if (hour >= s.From && hour < s.To) return s.Home;
            return false;   // between stays: on the road
        }

        /// <summary>The cars standing in a building's spots now (spot index, car id).</summary>
        public void ParkedAt(Building b, float hour, List<KeyValuePair<int, int>> into)
        {
            into.Clear();
            List<Stay> l;
            if (!byBuilding.TryGetValue(b.Id, out l)) return;
            foreach (var s in l) if (!s.Home && hour >= s.From && hour < s.To) into.Add(new KeyValuePair<int, int>(s.Spot, s.Car));
        }

        public Building BuildingOf(Stay s) { return s != null && s.Building >= 0 ? town.GetBuilding(s.Building) : null; }

        public Vector3 SpotOf(Stay s)
        {
            var b = BuildingOf(s);
            if (b == null) return Vector3.zero;
            Lot l;
            var spots = lots.TryGetValue(b.Id, out l) ? l.Local : LocalSpots(b);
            return s.Spot >= 0 && s.Spot < spots.Count ? spots[s.Spot] : Vector3.zero;
        }
    }
}
