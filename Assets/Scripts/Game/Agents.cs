using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// The people and vehicles you see moving. Each day every citizen gets a plan from what the simulation says they did:
    /// to work or school in the morning, home in the afternoon by way of a shop, out for the evening. They walk, or drive if
    /// there's a car for them and it's far: the family's working cars go to the longest drives first, and each is the town's
    /// own car in its own paint. Without a car, a long way, and a bus stop near both ends, they take the bus (BusLines): kids
    /// to school, too. Trucks carry the day's shipments to and from the highway.
    /// </summary>
    public sealed class Agents : MonoBehaviour
    {
        public const float WalkSpeed = 160f, CarSpeed = 520f, TruckSpeed = 420f;   // metres per game hour
        const int MaxActive = 700;

        internal sealed class Trip
        {
            public int Citizen = -1;     // -1: a truck
            public int From, To;          // building ids (-1: the highway)
            public float Start;           // hour
            public bool Car;
            public uint Seed;
            public List<Vector3> Path;
            public float Length, Speed;
            public bool Failed, Done;
            public GameObject Go;
            public int Kind;              // 0 walking, 1 car, 2 truck, 3 by bus
            public int CarId = -1, Home = -1;
            // by bus: the rides, which one they're on, and where they are in it
            public List<BusLines.Leg> Legs;
            public int LegIdx;
            public int Phase;             // 0 walking to the stop, 1 waiting, 2 riding, 3 walking from the stop
            public float PhaseStart;
            public Vector3 WaitAt;
            public int Pool { get { return Kind == 3 ? 0 : Kind; } }
            // by car: where the car stands before and after, and the three legs (walk to it, drive, walk from it)
            public CarParks.Stay Before, After;
            public List<Vector3>[] Segs;
            public float[] SegLen;
            public GameObject Walker;
        }

        struct Pend { public Building From, To; public float Hour; public int Car; public uint Seed; }

        Town town;
        WorldView view;
        readonly List<Trip> trips = new List<Trip>();
        readonly List<Trip> active = new List<Trip>();
        int next, planned = -1;
        float lastHour;
        readonly Stack<GameObject>[] pools = { new Stack<GameObject>(), new Stack<GameObject>(), new Stack<GameObject>() };
        Model[] people, kids, cars, trucks;
        // today: the car each driver has, where each car is as the plan is made, and each car's trips
        readonly Dictionary<int, int> driverCar = new Dictionary<int, int>();
        readonly Dictionary<int, int> carAt = new Dictionary<int, int>();
        readonly Dictionary<int, List<Trip>> carTrips = new Dictionary<int, List<Trip>>();
        readonly CarParks parks = new CarParks();
        readonly Dictionary<int, CarParks.Stay> carStay = new Dictionary<int, CarParks.Stay>();
        readonly List<Pend> pend = new List<Pend>();
        internal CarParks Parks { get { return parks; } }
        Transform root;
        BusLines lines;
        internal BusLines Lines { get { return lines; } }
        internal List<Trip> ActiveTrips { get { return active; } }
        internal List<Trip> DayTrips { get { return trips; } }
        internal int GaveUp;

        public void Bind(Town t, WorldView v)
        {
            town = t; view = v;
            foreach (var tr in active) Release(tr);
            active.Clear(); trips.Clear(); next = 0; planned = -1;
            if (root == null) { root = new GameObject("Agents").transform; root.SetParent(transform, false); }
            if (lines == null) lines = new BusLines(this, root);
            lines.Bind(t, v.Nav);
            if (people == null)
            {
                people = new Model[14]; kids = new Model[6];
                for (int i = 0; i < people.Length; i++) people[i] = PropArt.Person((uint)(i * 2654435761u + 7), false);
                for (int i = 0; i < kids.Length; i++) kids[i] = PropArt.Person((uint)(i * 40503u + 11), true);
                cars = new Model[PropArt.CarColors.Length];
                for (int i = 0; i < cars.Length; i++) cars[i] = PropArt.CarModel(PropArt.CarColors[i]);
                trucks = new[] { PropArt.TruckModel(MeshKit.Hex("#ecf0f1"), MeshKit.Hex("#0a79df")), PropArt.TruckModel(MeshKit.Hex("#c0392b"), MeshKit.Hex("#dfe4e8")), PropArt.TruckModel(MeshKit.Hex("#2d3436"), MeshKit.Hex("#f39c12")) };
            }
        }

        // ================================================================== the day's plans
        void Plan()
        {
            foreach (var tr in active) Release(tr);
            active.Clear(); trips.Clear(); next = 0;
            lines.ClearRiders();
            planned = town.Day;
            driverCar.Clear(); carAt.Clear(); carTrips.Clear(); carStay.Clear();
            parks.Reset(town);
            var drivers = new List<Citizen>();
            foreach (var h in town.Households)
            {
                var home = town.HomeOf(h);
                var owned = town.CarsOf(h);
                if (home == null || owned.Count == 0) continue;
                drivers.Clear();
                foreach (var m in town.Members(h)) if (!m.IsChild) drivers.Add(m);
                drivers.Sort((a, b) => DriveRank(a, home).CompareTo(DriveRank(b, home)));
                int i = 0, k = -1;
                foreach (var car in owned)
                {
                    k++;
                    if (!Town.Drivable(car)) continue;
                    if (i >= drivers.Count) break;
                    driverCar[drivers[i++].Id] = car.Id;
                    carAt[car.Id] = home.Id;
                    carStay[car.Id] = parks.Home(car.Id, home, k, 0, 24);
                }
            }
            foreach (var c in town.People)
            {
                var h = town.HouseholdOf(c);
                var home = town.HomeOf(h);
                if (home == null || !home.Built) continue;
                uint s = (uint)(c.Id * 2654435761u) ^ (uint)(town.Day * 40503);
                float r1 = (s & 255) / 255f, r2 = ((s >> 8) & 255) / 255f, r3 = ((s >> 16) & 255) / 255f;
                int car;
                if (!driverCar.TryGetValue(c.Id, out car)) car = -1;
                var work = c.AtWork ? town.GetBuilding(c.WorkId) : null;
                var school = c.SchoolId >= 0 && c.Sick == 0 ? town.GetBuilding(c.SchoolId) : null;
                var errand = town.GetBuilding(c.ErrandId);
                var fun = town.GetBuilding(c.LeisureId);
                if (work != null && work != home)
                {
                    float go = 6.4f + r1 * 1.6f;
                    Add(c, home, home, work, go, car, s);
                    float back = 15.5f + r2 * 2.5f;
                    if (errand != null && errand != work) { Add(c, home, work, errand, back, car, s + 1); Add(c, home, errand, home, back + 0.9f + r3 * 0.6f, car, s + 2); }
                    else Add(c, home, work, home, back, car, s + 1);
                    if (fun != null) { Add(c, home, home, fun, 18.6f + r3 * 2f, car, s + 3); Add(c, home, fun, home, 21.0f + r3 * 1.2f, car, s + 4); }
                }
                else if (school != null)
                {
                    Add(c, home, home, school, 7.0f + r1 * 0.8f, -1, s);
                    Add(c, home, school, home, 14.6f + r2 * 1.2f, -1, s + 1);
                    if (fun != null && c.Age >= 14) { Add(c, home, home, fun, 17f + r3 * 2f, -1, s + 3); Add(c, home, fun, home, 19.5f + r3 * 1.5f, -1, s + 4); }
                }
                else if (!c.IsChild || c.Age >= 12)
                {
                    if (errand != null) { float t = 9.5f + r1 * 5f; Add(c, home, home, errand, t, car, s); Add(c, home, errand, home, t + 0.8f + r2, car, s + 1); }
                    if (fun != null) { float t = errand != null ? 16 + r3 * 4 : 11 + r3 * 8; Add(c, home, home, fun, t, car, s + 3); Add(c, home, fun, home, t + 1.5f + r2, car, s + 4); }
                }
                Flush(c, home);
            }
            if (town.Shipments != null)
                foreach (var sh in town.Shipments)
                {
                    var t = new Trip { From = sh.From, To = sh.To, Start = sh.Hour, Car = true, Kind = 2, Seed = (uint)(sh.From * 31 + sh.To * 17 + (int)sh.Good) };
                    trips.Add(t);
                }
            trips.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        /// <summary>Who gets the family's cars first: those who drive out of town, then those with a job across town.</summary>
        int DriveRank(Citizen c, Building home)
        {
            if (town.Commutes(c)) return 0;
            var w = c.WorkId >= 0 ? town.GetBuilding(c.WorkId) : null;
            if (w != null && Mathf.Abs(w.CenterX - home.CenterX) + Mathf.Abs(w.CenterY - home.CenterY) > 6) return 1;
            return c.WorkId >= 0 ? 2 : 3;
        }

        /// <summary>A trip for later today: the citizen's trips are made together (Flush), so each knows when they leave again.</summary>
        void Add(Citizen c, Building home, Building from, Building to, float hour, int carId, uint seed)
        {
            if (from == null || to == null || from == to) return;
            pend.Add(new Pend { From = from, To = to, Hour = hour, Car = carId, Seed = seed });
        }

        void Flush(Citizen c, Building home)
        {
            for (int i = 0; i < pend.Count; i++)
            {
                var p = pend[i];
                float leave = 24f;
                for (int j = i + 1; j < pend.Count; j++) if (pend[j].From == p.To) { leave = pend[j].Hour; break; }
                Make(c, home, p.From, p.To, p.Hour, p.Car, p.Seed, leave);
            }
            pend.Clear();
        }

        void Make(Citizen c, Building home, Building from, Building to, float hour, int carId, uint seed, float leave)
        {
            float dist = Mathf.Abs(from.CenterX - to.CenterX) + Mathf.Abs(from.CenterY - to.CenterY);
            // they drive if their car is where they are: always away from home (it goes where they go), and from home unless
            // it's just round the corner
            int at;
            bool car = carId >= 0 && carAt.TryGetValue(carId, out at) && at == from.Id && (from != home || dist > 5);
            var t = new Trip { Citizen = c.Id, From = from.Id, To = to.Id, Start = hour, Car = car, Kind = car ? 1 : 0, Seed = seed,
                               CarId = car ? carId : -1, Home = home.Id };
            trips.Add(t);
            if (!car)
            {
                // too far to walk: the bus, if there's a stop near both ends; they set off earlier to allow for it
                if (dist > Town.RideMin && lines.Ready)
                {
                    int fs = town.StopNear(from), ts = town.StopNear(to);
                    float hours = 0;
                    var legs = fs >= 0 && ts >= 0 ? lines.Plan(fs, ts, out hours) : null;
                    if (legs != null)
                    {
                        t.Kind = 3; t.Legs = legs;
                        t.Start = Mathf.Max(BusLines.First + 0.1f, hour - Mathf.Min(2f, hours * 0.8f + 0.3f));
                    }
                }
                return;
            }
            carAt[carId] = to.Id;
            List<Trip> l;
            if (!carTrips.TryGetValue(carId, out l)) carTrips[carId] = l = new List<Trip>();
            l.Add(t);
            // where the car stands: it leaves where it was, and parks near where it's going (at home, on the driveway)
            CarParks.Stay before;
            if (!carStay.TryGetValue(carId, out before)) before = parks.Away(carId, 0, 24);
            float drive = 0.1f + dist * Nav.Tile * 1.3f / CarSpeed;
            float walk = before.Building >= 0 ? 0.08f : 0.02f;
            // not before the car has got there from its last drive (a long way out to the warehouse club, say)
            float off = Mathf.Max(hour + walk, before.From + 0.05f);
            t.Start = off - walk;
            float arrive = off + drive;
            CarParks.Stay after;
            if (to == home)
            {
                int k = town.CarsOf(town.HouseholdOf(c)).FindIndex(x => x.Id == carId);
                after = parks.Home(carId, home, k, arrive, 24);
            }
            else if (to.Id == town.TerminalId) after = parks.Away(carId, arrive, leave);   // off out of town
            else after = parks.Find(carId, to, arrive, Mathf.Max(leave + 0.3f, arrive + 0.1f)) ?? parks.Away(carId, arrive, Mathf.Max(leave, arrive + 0.1f));
            before.To = off;
            t.Before = before; t.After = after;
            carStay[carId] = after;
        }

        /// <summary>Whether a family car is on the driveway right now, rather than on the road or parked where its driver is.</summary>
        public bool CarAtHome(int carId)
        {
            if (town == null || planned != town.Day) return true;
            return parks.AtHome(carId, town.Hour);
        }

        // ================================================================== every frame
        void Update()
        {
            if (town == null) return;
            if (lines.Sync()) DropRiders();
            if (planned != town.Day) Plan();
            float hour = town.Hour;
            if (hour < lastHour - 12) { lastHour = hour; return; }
            lastHour = hour;
            // start the trips whose time has come
            while (next < trips.Count && trips[next].Start <= hour)
            {
                var t = trips[next++];
                if (hour - t.Start > 3f) continue;
                if (active.Count >= MaxActive) continue;
                if (!Begin(t)) continue;
                active.Add(t);
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var t = active[i];
                if (t.Kind == 3)
                {
                    if (!StepRider(t, hour)) { Release(t); active.RemoveAt(i); }
                    continue;
                }
                if (t.Segs != null)
                {
                    if (!StepDriver(t, hour)) { Release(t); active.RemoveAt(i); }
                    continue;
                }
                float dist = (hour - t.Start) * t.Speed;
                if (dist >= t.Length || dist < 0) { Release(t); active.RemoveAt(i); continue; }
                Vector3 dir;
                var p = Nav.At(t.Path, dist, out dir);
                t.Go.transform.localPosition = p + new Vector3(0, t.Kind == 0 ? 0.2f : 0.06f, 0);
                if (dir.sqrMagnitude > 0.001f) t.Go.transform.localRotation = Quaternion.LookRotation(dir);
            }
            lines.Update(hour);
        }

        bool Begin(Trip t)
        {
            var nav = view.Nav;
            var from = t.From >= 0 ? town.GetBuilding(t.From) : town.Terminal;
            var to = t.To >= 0 ? town.GetBuilding(t.To) : town.Terminal;
            if (from == null || to == null) return false;
            if (t.Kind == 3)
            {
                // off to the stop; if it can't be reached they walk the whole way
                int stop = t.Legs[0].Board;
                t.LegIdx = 0;
                t.WaitAt = lines.WaitSpot(stop, t.Seed);
                t.Path = nav.WalkToStop(from, lines.StopTile(stop), t.WaitAt);
                if (t.Path != null && t.Path.Count >= 2)
                {
                    t.Length = Nav.Length(t.Path); t.Speed = WalkSpeed;
                    t.Phase = 0; t.PhaseStart = t.Start;
                    t.Go = Take(t);
                    return true;
                }
                t.Kind = 0; t.Legs = null;
            }
            if (t.Kind == 1 && t.Before != null && t.After != null && BeginDrive(t, from, to)) return true;
            t.Path = nav.Polyline(from, to, t.Car);
            if (t.Path == null || t.Path.Count < 2) return false;
            t.Length = Nav.Length(t.Path);
            t.Speed = t.Kind == 2 ? TruckSpeed : t.Car ? CarSpeed : WalkSpeed;
            t.Go = Take(t);
            return true;
        }

        /// <summary>A drive in three legs: out to where the car stands (the driveway, a car park, the kerb), the drive, and from
        /// where it parks to the door. The car's stays get the times it actually leaves and arrives.</summary>
        bool BeginDrive(Trip t, Building from, Building to)
        {
            var nav = view.Nav;
            var startB = parks.BuildingOf(t.Before);
            var endB = parks.BuildingOf(t.After);
            Vector3 carFrom, carTo;
            List<Vector3> leave, arrive;
            if (startB != null) { var sp = parks.SpotOf(t.Before); leave = CarParks.Leave(startB, sp); carFrom = leave[0]; }
            else { carFrom = nav.Kerb(from, false); leave = new List<Vector3> { carFrom }; startB = from; }
            if (endB != null) { var sp = parks.SpotOf(t.After); arrive = CarParks.Arrive(endB, sp); carTo = arrive[arrive.Count - 1]; }
            else { carTo = nav.Kerb(to, true); arrive = new List<Vector3> { carTo }; endB = to; }
            var drive = nav.Drive(startB, endB, leave, arrive);
            if (drive == null || drive.Count < 2) return false;
            var walkA = nav.Walk(nav.Door(from), from, startB, carFrom);
            var walkC = nav.Walk(carTo, endB, to, nav.Door(to));
            t.Segs = new[] { walkA, drive, walkC };
            t.SegLen = new[] { Nav.Length(walkA), Nav.Length(drive), Nav.Length(walkC) };
            t.Length = t.SegLen[0] + t.SegLen[1] + t.SegLen[2];
            float a = t.SegLen[0] / WalkSpeed, b = t.SegLen[1] / CarSpeed;
            t.Before.To = t.Start + a;
            t.After.From = t.Start + a + b;
            t.Go = Take(t, 1);
            t.Go.SetActive(false);
            t.Walker = Take(t, 0);
            return true;
        }

        /// <summary>A driver's turn: walking to the car, driving, or walking from it. False when they're there.</summary>
        bool StepDriver(Trip t, float hour)
        {
            float e = hour - t.Start;
            if (e < 0) return true;
            float a = t.SegLen[0] / WalkSpeed, b = t.SegLen[1] / CarSpeed, c = t.SegLen[2] / WalkSpeed;
            int seg; float dist;
            if (e < a) { seg = 0; dist = e * WalkSpeed; }
            else if (e < a + b) { seg = 1; dist = (e - a) * CarSpeed; }
            else if (e < a + b + c) { seg = 2; dist = (e - a - b) * WalkSpeed; }
            else return false;
            bool driving = seg == 1;
            if (t.Go.activeSelf != driving) t.Go.SetActive(driving);
            if (t.Walker.activeSelf == driving) t.Walker.SetActive(!driving);
            var go = driving ? t.Go : t.Walker;
            Vector3 dir;
            var p = Nav.At(t.Segs[seg], dist, out dir);
            go.transform.localPosition = p + new Vector3(0, driving ? 0.06f : 0.2f, 0);
            if (dir.sqrMagnitude > 0.001f) go.transform.localRotation = Quaternion.LookRotation(dir);
            return true;
        }

        GameObject Take(Trip t) { return Take(t, t.Pool); }

        GameObject Take(Trip t, int poolIndex)
        {
            int kind = poolIndex;
            var pool = pools[poolIndex];
            GameObject go = pool.Count > 0 ? pool.Pop() : null;
            if (go == null)
            {
                go = new GameObject(kind == 0 ? "Person" : kind == 1 ? "Car" : "Truck");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = kind == 0 ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            Model m;
            if (kind == 2) m = trucks[t.Seed % (uint)trucks.Length];
            else if (kind == 1)
            {
                var car = town.GetCar(t.CarId);
                var owner = town.GetCitizen(t.Citizen);
                m = car != null ? CarLook.Of(car) : cars[(uint)((owner != null ? owner.HouseholdId : 0) * 7) % (uint)cars.Length];
            }
            else
            {
                var c = town.GetCitizen(t.Citizen);
                m = c != null && c.Age < 14 ? kids[(uint)c.Id % (uint)kids.Length] : people[(uint)t.Citizen % (uint)people.Length];
            }
            go.GetComponent<MeshFilter>().sharedMesh = m.Mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = m.Materials;
            go.name = kind == 0 ? "Person " + t.Citizen : go.name;
            go.SetActive(true);
            return go;
        }

        void Release(Trip t)
        {
            if (t.Kind == 3) lines.Drop(t);
            if (t.Walker != null) { t.Walker.SetActive(false); pools[0].Push(t.Walker); t.Walker = null; }
            if (t.Go == null) return;
            t.Go.SetActive(false);
            pools[t.Pool].Push(t.Go);
            t.Go = null;
        }

        // ================================================================== riding the bus
        /// <summary>A bus rider's turn: walking to or from a stop, waiting, or on board. False when they've arrived (or given up).</summary>
        bool StepRider(Trip t, float hour)
        {
            switch (t.Phase)
            {
                case 0:
                case 3:
                {
                    float dist = (hour - t.PhaseStart) * WalkSpeed;
                    if (dist < 0) return true;
                    if (dist >= t.Length)
                    {
                        if (t.Phase == 3) return false;
                        t.Phase = 1; t.PhaseStart = hour;
                        Stand(t);
                        lines.Wait(t);
                        return true;
                    }
                    Vector3 dir;
                    var p = Nav.At(t.Path, dist, out dir);
                    t.Go.transform.localPosition = p + new Vector3(0, 0.2f, 0);
                    if (dir.sqrMagnitude > 0.001f) t.Go.transform.localRotation = Quaternion.LookRotation(dir);
                    return true;
                }
                case 1:
                    if (hour - t.PhaseStart < 3f || hour < t.PhaseStart) return true;
                    GaveUp++;   // after three hours they give up
                    return false;
                default:
                    return true;   // on the bus: it carries them
            }
        }

        /// <summary>Waiting at the stop, facing the road.</summary>
        void Stand(Trip t)
        {
            if (t.Go == null) return;
            t.Go.SetActive(true);
            t.Go.transform.localPosition = t.WaitAt + new Vector3(0, 0.2f, 0);
            int stop = t.Legs[t.LegIdx].Board;
            var tile = Nav.Center(lines.StopTile(stop));
            var face = tile - t.WaitAt; face.y = 0;
            if (face.sqrMagnitude > 0.01f) t.Go.transform.localRotation = Quaternion.LookRotation(face);
        }

        internal void Board(Trip t)
        {
            t.Phase = 2;
            if (t.Go != null) t.Go.SetActive(false);
        }

        internal void Alight(Trip t, int stop, float hour)
        {
            t.LegIdx++;
            t.PhaseStart = hour;
            if (t.LegIdx < t.Legs.Count)
            {
                // changing buses at the depot
                t.Phase = 1;
                t.WaitAt = lines.WaitSpot(stop, t.Seed * 7 + 3);
                Stand(t);
                lines.Wait(t);
                return;
            }
            var to = t.To >= 0 ? town.GetBuilding(t.To) : town.Terminal;
            var spot = lines.WaitSpot(stop, t.Seed * 13 + 5);
            t.Path = view.Nav.WalkFromStop(lines.StopTile(stop), spot, to);
            if (t.Path == null || t.Path.Count < 2) { Finish(t); return; }
            t.Length = Nav.Length(t.Path);
            t.Phase = 3;
            if (t.Go != null) t.Go.SetActive(true);
        }

        /// <summary>Their trip's over: off the bus at the end of the day, or the lines changed.</summary>
        internal void Finish(Trip t)
        {
            int i = active.IndexOf(t);
            Release(t);
            if (i >= 0) active.RemoveAt(i);
        }

        void DropRiders()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                if (active[i].Kind == 3) { Release(active[i]); active.RemoveAt(i); }
            // and the day's plan, made with the old lines, walks instead
            foreach (var t in trips) if (t.Kind == 3 && t.Go == null) { t.Kind = 0; t.Legs = null; }
        }

        // ================================================================== picking and following
        /// <summary>The citizen whose figure or car is nearest a screen point, within a few pixels (-1: none).</summary>
        public int Pick(Camera cam, Vector2 screen, float radius = 16f)
        {
            int best = -1; float bd = radius * radius;
            foreach (var t in active)
            {
                var shown = t.Walker != null && t.Walker.activeSelf ? t.Walker : t.Go;
                if (t.Citizen < 0 || shown == null || !shown.activeSelf) continue;
                var sp = cam.WorldToScreenPoint(shown.transform.position + Vector3.up);
                if (sp.z < 0) continue;
                float d = ((Vector2)sp - screen).sqrMagnitude;
                if (d < bd) { bd = d; best = t.Citizen; }
            }
            return best;
        }

        /// <summary>Where a citizen is right now if they're out and about.</summary>
        public bool Where(int citizen, out Vector3 pos)
        {
            foreach (var t in active)
            {
                if (t.Citizen != citizen || t.Go == null) continue;
                if (t.Kind == 3 && t.Phase == 2) return lines.Riding(t, out pos);
                pos = (t.Walker != null && t.Walker.activeSelf ? t.Walker : t.Go).transform.position; return true;
            }
            pos = Vector3.zero; return false;
        }

        public int ActiveCount { get { return active.Count; } }
    }
}
