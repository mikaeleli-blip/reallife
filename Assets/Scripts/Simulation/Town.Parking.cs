using System;
using System.Collections.Generic;

namespace RealLife.Simulation
{
    /// <summary>
    /// Parking. Drivers going somewhere beyond a walk park in a parking lot near it, or the place's own lot (a grocery store,
    /// a gas station, a garage, a dealer), and walk the rest; given the choice they go where they can park. (Where nobody has
    /// built a lot they pull up at the kerb.)
    /// </summary>
    public partial class Town
    {
        public const int ParkWalk = 7;   // tiles drivers will walk from a parking lot

        [NonSerialized] Dictionary<int, bool> parkingNear;
        [NonSerialized] int parkingDay = -1, parkingMap = -1;

        /// <summary>Is there somewhere to park near this place: a lot within a short walk, or its own.</summary>
        public bool ParkingNear(Building b)
        {
            if (b == null) return false;
            if (b.Type == BuildingType.GroceryStore || b.Type == BuildingType.GasStation || b.Type == BuildingType.CarDealer ||
                b.Type == BuildingType.AutoRepair || b.Type == BuildingType.ParkingLot) return true;
            if (parkingNear == null || parkingDay != Day || parkingMap != MapVersion) { parkingNear = new Dictionary<int, bool>(); parkingDay = Day; parkingMap = MapVersion; }
            bool r;
            if (parkingNear.TryGetValue(b.Id, out r)) return r;
            r = false;
            foreach (var p in Buildings)
                if (p.Type == BuildingType.ParkingLot && p.Built && Distance(p, b) <= ParkWalk) { r = true; break; }
            parkingNear[b.Id] = r;
            return r;
        }

        /// <summary>How much less a driver likes a place they can't park near (1: no difference).</summary>
        float ParkingFactor(Building home, Building b, bool drives)
        {
            if (!drives || Distance(home, b) <= RideMin || ParkingNear(b)) return 1f;
            return 1.15f;
        }
    }
}
