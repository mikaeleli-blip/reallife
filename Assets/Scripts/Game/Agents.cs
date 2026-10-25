using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// The people and vehicles you see moving. Each day every citizen gets a plan from what the simulation says they did:
    /// to work or school in the morning, home in the afternoon by way of a shop, out for the evening. They walk, or drive if
    /// the family has a car and it's far. Trucks carry the day's shipments to and from the highway.
    /// </summary>
    public sealed class Agents : MonoBehaviour
    {
        public const float WalkSpeed = 160f, CarSpeed = 520f, TruckSpeed = 420f;   // metres per game hour
        const int MaxActive = 700;

        sealed class Trip
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
            public int Kind;              // 0 walking, 1 car, 2 truck
        }

        Town town;
        WorldView view;
        readonly List<Trip> trips = new List<Trip>();
        readonly List<Trip> active = new List<Trip>();
        int next, planned = -1;
        float lastHour;
        readonly Stack<GameObject>[] pools = { new Stack<GameObject>(), new Stack<GameObject>(), new Stack<GameObject>() };
        Model[] people, kids, cars, trucks;
        Transform root;

        public void Bind(Town t, WorldView v)
        {
            town = t; view = v;
            foreach (var tr in active) Release(tr);
            active.Clear(); trips.Clear(); next = 0; planned = -1;
            if (root == null) { root = new GameObject("Agents").transform; root.SetParent(transform, false); }
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
            planned = town.Day;
            foreach (var c in town.People)
            {
                var h = town.HouseholdOf(c);
                var home = town.HomeOf(h);
                if (home == null || !home.Built) continue;
                uint s = (uint)(c.Id * 2654435761u) ^ (uint)(town.Day * 40503);
                float r1 = (s & 255) / 255f, r2 = ((s >> 8) & 255) / 255f, r3 = ((s >> 16) & 255) / 255f;
                bool hasCar = h.Cars > 0 && !h.CarBroken && !c.IsChild;
                var work = c.AtWork ? town.GetBuilding(c.WorkId) : null;
                var school = c.SchoolId >= 0 && c.Sick == 0 ? town.GetBuilding(c.SchoolId) : null;
                var errand = town.GetBuilding(c.ErrandId);
                var fun = town.GetBuilding(c.LeisureId);
                if (work != null && work != home)
                {
                    float go = 6.4f + r1 * 1.6f;
                    Add(c, home, work, go, hasCar, s);
                    float back = 15.5f + r2 * 2.5f;
                    if (errand != null && errand != work) { Add(c, work, errand, back, hasCar, s + 1); Add(c, errand, home, back + 0.9f + r3 * 0.6f, hasCar, s + 2); }
                    else Add(c, work, home, back, hasCar, s + 1);
                    if (fun != null) { Add(c, home, fun, 18.6f + r3 * 2f, hasCar, s + 3); Add(c, fun, home, 21.0f + r3 * 1.2f, hasCar, s + 4); }
                }
                else if (school != null)
                {
                    Add(c, home, school, 7.0f + r1 * 0.8f, false, s);
                    Add(c, school, home, 14.6f + r2 * 1.2f, false, s + 1);
                    if (fun != null && c.Age >= 14) { Add(c, home, fun, 17f + r3 * 2f, false, s + 3); Add(c, fun, home, 19.5f + r3 * 1.5f, false, s + 4); }
                }
                else if (!c.IsChild || c.Age >= 12)
                {
                    if (errand != null) { float t = 9.5f + r1 * 5f; Add(c, home, errand, t, hasCar, s); Add(c, errand, home, t + 0.8f + r2, hasCar, s + 1); }
                    if (fun != null) { float t = errand != null ? 16 + r3 * 4 : 11 + r3 * 8; Add(c, home, fun, t, hasCar, s + 3); Add(c, fun, home, t + 1.5f + r2, hasCar, s + 4); }
                }
            }
            if (town.Shipments != null)
                foreach (var sh in town.Shipments)
                {
                    var t = new Trip { From = sh.From, To = sh.To, Start = sh.Hour, Car = true, Kind = 2, Seed = (uint)(sh.From * 31 + sh.To * 17 + (int)sh.Good) };
                    trips.Add(t);
                }
            trips.Sort((a, b) => a.Start.CompareTo(b.Start));
        }

        void Add(Citizen c, Building from, Building to, float hour, bool hasCar, uint seed)
        {
            if (from == null || to == null || from == to) return;
            float dist = Mathf.Abs(from.CenterX - to.CenterX) + Mathf.Abs(from.CenterY - to.CenterY);
            bool car = hasCar && dist > 5;
            trips.Add(new Trip { Citizen = c.Id, From = from.Id, To = to.Id, Start = hour, Car = car, Kind = car ? 1 : 0, Seed = seed });
        }

        // ================================================================== every frame
        void Update()
        {
            if (town == null) return;
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
                float dist = (hour - t.Start) * t.Speed;
                if (dist >= t.Length || dist < 0) { Release(t); active.RemoveAt(i); continue; }
                Vector3 dir;
                var p = Nav.At(t.Path, dist, out dir);
                t.Go.transform.localPosition = p + new Vector3(0, t.Kind == 0 ? 0.2f : 0.06f, 0);
                if (dir.sqrMagnitude > 0.001f) t.Go.transform.localRotation = Quaternion.LookRotation(dir);
            }
        }

        bool Begin(Trip t)
        {
            var nav = view.Nav;
            var from = t.From >= 0 ? town.GetBuilding(t.From) : town.Terminal;
            var to = t.To >= 0 ? town.GetBuilding(t.To) : town.Terminal;
            if (from == null || to == null) return false;
            t.Path = nav.Polyline(from, to, t.Car);
            if (t.Path == null || t.Path.Count < 2) return false;
            t.Length = Nav.Length(t.Path);
            t.Speed = t.Kind == 2 ? TruckSpeed : t.Car ? CarSpeed : WalkSpeed;
            t.Go = Take(t);
            return true;
        }

        GameObject Take(Trip t)
        {
            var pool = pools[t.Kind];
            GameObject go = pool.Count > 0 ? pool.Pop() : null;
            if (go == null)
            {
                go = new GameObject(t.Kind == 0 ? "Person" : t.Kind == 1 ? "Car" : "Truck");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>();
                var r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = t.Kind == 0 ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            }
            Model m;
            if (t.Kind == 2) m = trucks[t.Seed % (uint)trucks.Length];
            else if (t.Kind == 1)
            {
                var owner = town.GetCitizen(t.Citizen);
                m = cars[(uint)((owner != null ? owner.HouseholdId : 0) * 7) % (uint)cars.Length];
            }
            else
            {
                var c = town.GetCitizen(t.Citizen);
                m = c != null && c.Age < 14 ? kids[(uint)c.Id % (uint)kids.Length] : people[(uint)t.Citizen % (uint)people.Length];
            }
            go.GetComponent<MeshFilter>().sharedMesh = m.Mesh;
            go.GetComponent<MeshRenderer>().sharedMaterials = m.Materials;
            go.name = t.Kind == 0 ? "Person " + t.Citizen : go.name;
            go.SetActive(true);
            return go;
        }

        void Release(Trip t)
        {
            if (t.Go == null) return;
            t.Go.SetActive(false);
            pools[t.Kind].Push(t.Go);
            t.Go = null;
        }

        // ================================================================== picking and following
        /// <summary>The citizen whose figure or car is nearest a screen point, within a few pixels (-1: none).</summary>
        public int Pick(Camera cam, Vector2 screen, float radius = 16f)
        {
            int best = -1; float bd = radius * radius;
            foreach (var t in active)
            {
                if (t.Citizen < 0 || t.Go == null) continue;
                var sp = cam.WorldToScreenPoint(t.Go.transform.position + Vector3.up);
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
                if (t.Citizen == citizen && t.Go != null) { pos = t.Go.transform.position; return true; }
            pos = Vector3.zero; return false;
        }

        public int ActiveCount { get { return active.Count; } }
    }
}
