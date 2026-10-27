using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// The buses you see. Each route the depot set up is a loop from the depot's stop and back; its buses go round it all day,
    /// evenly spaced, from early morning to late evening, and pull in at a stop when someone's waiting there for that route or
    /// someone on board gets off there. Riders walk to their stop, wait under the shelter, ride, change at the depot if their
    /// stop's on another route, and walk the rest of the way.
    /// </summary>
    sealed class BusLines
    {
        public const float Speed = 600f;          // metres per game hour, a little brisker than a car on these streets
        public const float Dwell = 0.05f;         // hours at a stop
        public const float First = 5.5f, Last = 23f;
        public const int Seats = 40;

        /// <summary>One ride: which line, where they get on and off (stop indices).</summary>
        public struct Leg { public int Line, Board, Alight; }

        sealed class Line
        {
            public BusRoute Route;
            public List<Vector3> Path;
            public float Length;
            public float[] StopAt;                 // distance along the loop of each of its stops (the first is 0)
            public int[] Stops;                    // the town's stop indices, in order
            public readonly Dictionary<int, int> Index = new Dictionary<int, int>();
        }

        sealed class Bus
        {
            public Line Line;
            public float S;                        // distance along the loop
            public int Next;                       // the next of the line's stops it comes to
            public float Until;                    // pulled in until
            public readonly List<Agents.Trip> On = new List<Agents.Trip>();
            public GameObject Go;
        }

        readonly Agents agents;
        readonly Transform root;
        Town town;
        Nav nav;
        readonly List<Line> lines = new List<Line>();
        readonly List<Bus> buses = new List<Bus>();
        readonly Dictionary<int, List<Agents.Trip>> waiting = new Dictionary<int, List<Agents.Trip>>();
        readonly Dictionary<int, List<int>> linesAt = new Dictionary<int, List<int>>();
        readonly Stack<GameObject> pool = new Stack<GameObject>();
        int version = -1;
        GameObject stopsGo;
        Mesh stopsMesh;
        Model busModel;
        bool haveModel, running;
        float lastHour = -1;

        public BusLines(Agents a, Transform parent) { agents = a; root = parent; }

        // for the headless check
        internal int Boarded, Alighted;
        internal int BusesOut { get { return buses.Count; } }
        internal int LineCount { get { return lines.Count; } }
        internal int WaitingNow { get { int n = 0; foreach (var q in waiting.Values) n += q.Count; return n; } }
        internal int RidingNow { get { int n = 0; foreach (var b in buses) n += b.On.Count; return n; } }
        internal string Describe()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var l in lines) sb.Append(l.Stops.Length).Append(" stops ").Append((int)l.Length).Append(" m x").Append(l.Route.Buses).Append("; ");
            return sb.ToString();
        }

        public bool Ready { get { return lines.Count > 0; } }

        public void Bind(Town t, Nav n)
        {
            town = t; nav = n;
            version = -1;
            Clear();
        }

        /// <summary>Rebuilds the lines when the town's stops change. True if it did (riders on the old lines are dropped).</summary>
        public bool Sync()
        {
            if (town == null) return false;
            town.EnsureBusNet();
            if (town.BusNetVersion == version) return false;
            version = town.BusNetVersion;
            Clear();
            Build();
            return true;
        }

        void Clear()
        {
            foreach (var b in buses) if (b.Go != null) { b.Go.SetActive(false); pool.Push(b.Go); }
            buses.Clear(); lines.Clear(); waiting.Clear(); linesAt.Clear();
            running = false;
            if (stopsGo != null) stopsGo.SetActive(false);
        }

        void Build()
        {
            if (town.BusStops == null || town.BusStops.Count == 0) return;
            var tiles = new List<Vector2Int>();
            var at = new List<int>();
            foreach (var r in town.BusRoutes)
            {
                tiles.Clear();
                foreach (var s in r.Stops) tiles.Add(new Vector2Int(town.BusStops[s].X, town.BusStops[s].Y));
                var path = nav.Loop(tiles, at);
                if (path == null) continue;
                var along = Nav.Along(path);
                var l = new Line { Route = r, Path = path, Length = along[along.Length - 1], Stops = r.Stops.ToArray(), StopAt = new float[r.Stops.Count] };
                if (l.Length < 1) continue;
                for (int k = 0; k < l.Stops.Length; k++) { l.StopAt[k] = along[at[k]]; if (!l.Index.ContainsKey(l.Stops[k])) l.Index[l.Stops[k]] = k; }
                int li = lines.Count;
                lines.Add(l);
                foreach (var s in l.Stops)
                {
                    List<int> ls;
                    if (!linesAt.TryGetValue(s, out ls)) linesAt[s] = ls = new List<int>();
                    if (!ls.Contains(li)) ls.Add(li);
                }
            }
            BuildStops();
        }

        // ================================================================== the stops
        void BuildStops()
        {
            var k = new MeshKit();
            foreach (var s in town.BusStops)
            {
                Vector3 side, along;
                var c = Kerb(s, out side, out along);
                float yaw = Mathf.Atan2(side.x, side.z) * Mathf.Rad2Deg;
                PropArt.BusShelter(k, c + side * 4.15f + along * 2.3f + Vector3.up * 0.2f, yaw);
                PropArt.BusStopSign(k, c + side * 3.5f - along * 1.3f + Vector3.up * 0.2f, yaw);
            }
            if (stopsMesh != null) Object.Destroy(stopsMesh);
            var m = k.ToModel("bus stops");
            stopsMesh = m.Mesh;
            if (stopsGo == null)
            {
                stopsGo = new GameObject("Bus stops");
                stopsGo.transform.SetParent(root, false);
                stopsGo.AddComponent<MeshFilter>();
                stopsGo.AddComponent<MeshRenderer>();
            }
            stopsGo.GetComponent<MeshFilter>().sharedMesh = m.Mesh;
            stopsGo.GetComponent<MeshRenderer>().sharedMaterials = m.Materials;
            stopsGo.SetActive(true);
        }

        /// <summary>A stop's tile centre, the way to its kerb, and the way along the street.</summary>
        Vector3 Kerb(BusStop s, out Vector3 side, out Vector3 along)
        {
            side = new Vector3(s.SideX, 0, s.SideY);
            along = new Vector3(-side.z, 0, side.x);
            return Nav.Center(s.X, s.Y);
        }

        public Vector2Int StopTile(int stop) { var s = town.BusStops[stop]; return new Vector2Int(s.X, s.Y); }

        /// <summary>Where someone stands waiting at a stop: by the shelter, each in their own spot.</summary>
        public Vector3 WaitSpot(int stop, uint seed)
        {
            Vector3 side, along;
            var c = Kerb(town.BusStops[stop], out side, out along);
            float r1 = (seed & 255) / 255f, r2 = ((seed >> 8) & 255) / 255f;
            return c + side * (3.6f + r2 * 0.7f) + along * (1.2f + r1 * 2.3f);
        }

        // ================================================================== planning a ride
        /// <summary>The rides from one stop to another: straight there if a line serves both, else to the depot and change.
        /// Null if the buses don't connect them.</summary>
        public List<Leg> Plan(int from, int to, out float hours)
        {
            hours = 0;
            List<int> lf, lt;
            if (from == to || !linesAt.TryGetValue(from, out lf) || !linesAt.TryGetValue(to, out lt)) return null;
            int best = -1; float bd = float.MaxValue;
            foreach (int li in lf)
                if (lt.Contains(li)) { float d = Ride(lines[li], from, to); if (d < bd) { bd = d; best = li; } }
            if (best >= 0) { hours = Wait(lines[best]) + bd / Speed; return new List<Leg> { new Leg { Line = best, Board = from, Alight = to } }; }
            // change at the depot: every line starts there
            int a = lf[0], b = lt[0];
            int hub = lines[a].Stops[0];
            if (lines[b].Stops[0] != hub) return null;
            hours = Wait(lines[a]) + Ride(lines[a], from, hub) / Speed + Wait(lines[b]) + Ride(lines[b], hub, to) / Speed;
            return new List<Leg> { new Leg { Line = a, Board = from, Alight = hub }, new Leg { Line = b, Board = hub, Alight = to } };
        }

        static float Ride(Line l, int from, int to)
        {
            int a, b;
            if (!l.Index.TryGetValue(from, out a) || !l.Index.TryGetValue(to, out b)) return float.MaxValue / 4;
            float d = l.StopAt[b] - l.StopAt[a];
            return d > 0 ? d : d + l.Length;
        }

        static float Wait(Line l) { return l.Length / Speed / Mathf.Max(1, l.Route.Buses) * 0.5f; }

        public void Wait(Agents.Trip t)
        {
            int stop = t.Legs[t.LegIdx].Board;
            List<Agents.Trip> q;
            if (!waiting.TryGetValue(stop, out q)) waiting[stop] = q = new List<Agents.Trip>();
            q.Add(t);
        }

        /// <summary>Forget a rider (their trip's over or called off).</summary>
        public void Drop(Agents.Trip t)
        {
            if (t.Legs == null || t.LegIdx >= t.Legs.Count) return;
            List<Agents.Trip> q;
            if (waiting.TryGetValue(t.Legs[t.LegIdx].Board, out q)) q.Remove(t);
            foreach (var b in buses) b.On.Remove(t);
        }

        public void ClearRiders()
        {
            waiting.Clear();
            foreach (var b in buses) b.On.Clear();
        }

        /// <summary>Where the bus someone's riding is.</summary>
        public bool Riding(Agents.Trip t, out Vector3 pos)
        {
            foreach (var b in buses)
                if (b.Go != null && b.On.Contains(t)) { pos = b.Go.transform.position; return true; }
            pos = Vector3.zero; return false;
        }

        // ================================================================== every frame
        public void Update(float hour)
        {
            if (!Ready) return;
            bool service = hour >= First && hour < Last;
            if (!service)
            {
                if (running) Park();
                lastHour = hour;
                return;
            }
            if (!running || hour < lastHour) Start(hour);
            float dt = Mathf.Max(0, hour - lastHour);
            lastHour = hour;
            foreach (var b in buses)
            {
                Step(b, hour, dt);
                Vector3 dir;
                var p = Nav.At(b.Line.Path, b.S, out dir);
                b.Go.transform.localPosition = p + new Vector3(0, 0.06f, 0);
                if (dir.sqrMagnitude > 0.001f) b.Go.transform.localRotation = Quaternion.LookRotation(dir);
            }
        }

        /// <summary>Out of the depot in the morning: each line's buses spread evenly round it.</summary>
        void Start(float hour)
        {
            foreach (var b in buses) if (b.Go != null) { b.Go.SetActive(false); pool.Push(b.Go); }
            buses.Clear();
            if (!haveModel) { busModel = PropArt.BusModel(MeshKit.Hex("#2e86de")); haveModel = true; }
            foreach (var l in lines)
                for (int i = 0; i < l.Route.Buses; i++)
                {
                    var b = new Bus { Line = l, S = l.Length * i / l.Route.Buses, Until = hour };
                    b.Next = 0;
                    for (int k = 1; k < l.StopAt.Length; k++) if (l.StopAt[k] > b.S) { b.Next = k; break; }
                    b.Go = pool.Count > 0 ? pool.Pop() : NewBus();
                    b.Go.SetActive(true);
                    buses.Add(b);
                }
            running = true;
            lastHour = hour;
        }

        GameObject NewBus()
        {
            var go = new GameObject("Bus");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = busModel.Mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = busModel.Materials;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>Back to the depot for the night: anyone still on board gets off where they are going.</summary>
        void Park()
        {
            foreach (var b in buses)
            {
                foreach (var t in b.On.ToArray()) agents.Finish(t);
                b.On.Clear();
                if (b.Go != null) { b.Go.SetActive(false); pool.Push(b.Go); }
            }
            buses.Clear();
            waiting.Clear();
            running = false;
        }

        void Step(Bus b, float hour, float dt)
        {
            if (hour < b.Until) return;
            var l = b.Line;
            float move = Mathf.Min(dt, hour - b.Until) * Speed;
            int guard = 0;
            while (move > 0 && guard++ < 64)
            {
                float target = b.Next == 0 ? l.Length : l.StopAt[b.Next];
                if (b.S + move < target) { b.S += move; break; }
                move -= target - b.S;
                b.S = target;
                int k = b.Next;
                b.Next = (b.Next + 1) % l.Stops.Length;
                if (k == 0) b.S = 0;   // round again
                if (Serve(b, k, hour)) { b.Until = hour + Dwell; break; }
            }
        }

        /// <summary>At a stop: those getting off, then those waiting for this line, while there are seats.</summary>
        bool Serve(Bus b, int k, float hour)
        {
            int stop = b.Line.Stops[k];
            bool any = false;
            for (int i = b.On.Count - 1; i >= 0; i--)
            {
                var t = b.On[i];
                if (t.Legs[t.LegIdx].Alight != stop) continue;
                b.On.RemoveAt(i);
                Alighted++;
                agents.Alight(t, stop, hour);
                any = true;
            }
            List<Agents.Trip> q;
            if (waiting.TryGetValue(stop, out q))
            {
                int line = lines.IndexOf(b.Line);
                for (int i = 0; i < q.Count && b.On.Count < Seats; i++)
                {
                    var t = q[i];
                    if (t.Legs[t.LegIdx].Line != line) continue;
                    q.RemoveAt(i--);
                    b.On.Add(t);
                    Boarded++;
                    agents.Board(t);
                    any = true;
                }
            }
            return any;
        }
    }
}
