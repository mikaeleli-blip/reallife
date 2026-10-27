using System.Collections.Generic;
using RealLife.Game.Art;
using RealLife.Simulation;
using UnityEngine;

namespace RealLife.Game
{
    /// <summary>What a car looks like: its class picks the body (a small hatchback, a family saloon, a premium SUV), and it
    /// keeps its own paint from the day it's made.</summary>
    public static class CarLook
    {
        static Model[] models;
        static bool[] made;

        public static int Style(CarClass c) { return c == CarClass.Budget ? 0 : c == CarClass.Premium ? 2 : 1; }

        public static Model Of(Car car)
        {
            int n = PropArt.CarColors.Length;
            if (models == null) { models = new Model[3 * n]; made = new bool[3 * n]; }
            int style = Style(car.Class), color = ((car.Color % n) + n) % n;
            int i = style * n + color;
            if (!made[i]) { models[i] = PropArt.CarModel(PropArt.CarColors[color], style); made[i] = true; }
            return models[i];
        }
    }

    /// <summary>
    /// The cars standing still: each family's cars on the driveway while they're at home, drivers' cars in the car parks and
    /// grocery lots while they're out (CarParks), the cars for sale on a dealer's lot, the cars waiting their turn outside a
    /// garage. Every one is the town's own car, so the car on the driveway is the one you see on the road.
    /// </summary>
    public sealed class Parking : MonoBehaviour
    {
        const float Every = 0.25f;   // seconds between refreshes

        Town town;
        Agents agents;
        Transform root;
        readonly List<GameObject> objs = new List<GameObject>();
        readonly Dictionary<int, KeyValuePair<int, List<Vector3>>> spots = new Dictionary<int, KeyValuePair<int, List<Vector3>>>();
        float next;
        int used;
        readonly List<KeyValuePair<int, int>> parked = new List<KeyValuePair<int, int>>();

        public int Shown { get { return used; } }

        public void Bind(Town t, Agents a)
        {
            town = t; agents = a;
            if (root == null) { root = new GameObject("Parked cars").transform; root.SetParent(transform, false); }
            foreach (var o in objs) o.SetActive(false);
            spots.Clear();
            next = 0;
        }

        void LateUpdate()
        {
            if (town == null || Time.unscaledTime < next) return;
            next = Time.unscaledTime + Every;
            used = 0;
            foreach (var b in town.Buildings)
            {
                if (!b.Built) continue;
                switch (b.Type)
                {
                    case BuildingType.House:
                    {
                        if (b.Households.Count == 0) continue;
                        // each car has its own place on the driveway (the family's first car the first), empty while it's out
                        var at = Spots(b);
                        foreach (int hid in b.Households)
                        {
                            var cars = town.CarsOf(town.GetHousehold(hid));
                            for (int k = 0; k < cars.Count && k < at.Count; k++)
                            {
                                var car = cars[k];
                                if (car.ShopId != -1 || (agents != null && !agents.CarAtHome(car.Id))) continue;
                                Put(b, at[k], car);
                            }
                            break;
                        }
                        break;
                    }
                    case BuildingType.ParkingLot:
                    case BuildingType.GroceryStore:
                    case BuildingType.DiscountStore:
                    case BuildingType.Supermarket:
                    case BuildingType.WarehouseClub:
                    {
                        if (agents == null) continue;
                        var at = Spots(b);
                        agents.Parks.ParkedAt(b, town.Hour, parked);
                        foreach (var kv in parked)
                        {
                            var car = town.GetCar(kv.Value);
                            if (car != null && kv.Key >= 0 && kv.Key < at.Count) Put(b, at[kv.Key], car);
                        }
                        break;
                    }
                    case BuildingType.CarDealer:
                    {
                        var at = Spots(b);
                        int n = 0;
                        foreach (var car in town.LotOf(b)) { if (n >= at.Count) break; Put(b, at[n++], car); }
                        break;
                    }
                    case BuildingType.AutoRepair:
                    {
                        if (b.Closed) continue;
                        var at = Spots(b);
                        int n = 0;
                        // the ones being worked on are inside: those waiting stand out front
                        int inside = Mathf.Max(1, b.Employees.Count);
                        foreach (var car in town.QueueAt(b))
                        {
                            if (inside-- > 0) continue;
                            if (n >= at.Count) break;
                            Put(b, at[n++], car);
                        }
                        break;
                    }
                }
            }
            for (int i = used; i < objs.Count; i++) if (objs[i].activeSelf) objs[i].SetActive(false);
        }

        /// <summary>A lot's parking spots, in its own frame (x, z, yaw), kept until the building turns.</summary>
        List<Vector3> Spots(Building b)
        {
            KeyValuePair<int, List<Vector3>> kv;
            int key = b.Facing * 16 + (int)b.Type;
            if (spots.TryGetValue(b.Id, out kv) && kv.Key == key) return kv.Value;
            var d = b.Def;
            bool sideways = b.Facing == 1 || b.Facing == 3;
            float w = (sideways ? d.H : d.W) * WorldView.Tile, dep = (sideways ? d.W : d.H) * WorldView.Tile;
            var l = BuildingArt.ParkingSpots(b.Type, w, dep, (uint)(b.Id * 2654435761u));
            spots[b.Id] = new KeyValuePair<int, List<Vector3>>(key, l);
            return l;
        }

        void Put(Building b, Vector3 spot, Car car)
        {
            GameObject go;
            if (used < objs.Count) go = objs[used];
            else
            {
                go = new GameObject("Parked car");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                objs.Add(go);
            }
            used++;
            var m = CarLook.Of(car);
            var mf = go.GetComponent<MeshFilter>();
            if (mf.sharedMesh != m.Mesh)
            {
                mf.sharedMesh = m.Mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials = m.Materials;
            }
            float yaw = WorldView.Yaw(b.Facing);
            var rot = Quaternion.Euler(0, yaw, 0);
            go.transform.localPosition = WorldView.FootprintCenter(b) + rot * new Vector3(spot.x, 0.07f, spot.y);
            go.transform.localRotation = Quaternion.Euler(0, yaw + spot.z, 0);
            if (!go.activeSelf) go.SetActive(true);
        }
    }
}
