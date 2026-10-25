using UnityEngine;

namespace RealLife.Game
{
    /// <summary>
    /// A city-builder camera: it orbits a point on the ground. WASD or arrows (or the middle mouse button) to pan, Q/E or
    /// right-drag to turn, the wheel to zoom, R/F or right-drag up and down to tilt.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Cam { get; private set; }
        public Vector3 Pivot;
        public float Yaw = 35f, Pitch = 52f, Distance = 260f;
        Vector3 pivotGoal; float yawGoal, pitchGoal, distGoal;
        public float MinDistance = 25f, MaxDistance = 900f;
        public Vector2 Bounds = new Vector2(960, 960);
        Vector3 lastMouse; bool rotating, panning;
        Vector3 follow; bool following;

        void Awake()
        {
            Cam = GetComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.nearClipPlane = 1f; Cam.farClipPlane = 4000f;
            Cam.fieldOfView = 40f;
            pivotGoal = Pivot; yawGoal = Yaw; pitchGoal = Pitch; distGoal = Distance;
        }

        public void CenterOn(Vector3 world) { pivotGoal = new Vector3(world.x, 0, world.z); following = false; }
        public void Snap(Vector3 world) { Pivot = pivotGoal = new Vector3(world.x, 0, world.z); following = false; Apply(); }
        public void Follow(Vector3 world) { follow = world; following = true; }
        public void StopFollowing() { following = false; }
        public void ZoomTo(float d) { distGoal = Mathf.Clamp(d, MinDistance, MaxDistance); }

        /// <summary>Handles camera input. pointerFree is false while the mouse is over the HUD.</summary>
        public void Control(bool pointerFree, bool keysFree)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float speed = distGoal * 1.1f;
            var fwd = Quaternion.Euler(0, yawGoal, 0) * Vector3.forward;
            var right = Quaternion.Euler(0, yawGoal, 0) * Vector3.right;
            if (keysFree)
            {
                var move = Vector3.zero;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move += fwd;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move -= fwd;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move += right;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move -= right;
                if (move.sqrMagnitude > 0) { pivotGoal += move.normalized * speed * dt * (Input.GetKey(KeyCode.LeftShift) ? 2.5f : 1f); following = false; }
                if (Input.GetKey(KeyCode.Q)) yawGoal += 90 * dt;
                if (Input.GetKey(KeyCode.E)) yawGoal -= 90 * dt;
                if (Input.GetKey(KeyCode.R)) pitchGoal += 50 * dt;
                if (Input.GetKey(KeyCode.F)) pitchGoal -= 50 * dt;
                if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus)) distGoal *= 1 - dt * 1.5f;
                if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus)) distGoal *= 1 + dt * 1.5f;
            }
            if (pointerFree)
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    // zoom toward the point under the mouse
                    float before = distGoal;
                    distGoal = Mathf.Clamp(distGoal * Mathf.Pow(0.87f, wheel), MinDistance, MaxDistance);
                    Vector3 hit;
                    if (wheel > 0 && GroundPoint(Input.mousePosition, out hit)) pivotGoal = Vector3.Lerp(pivotGoal, hit, (1 - distGoal / before) * 0.9f);
                }
                if (Input.GetMouseButtonDown(1)) { rotating = true; lastMouse = Input.mousePosition; }
                if (Input.GetMouseButtonDown(2)) { panning = true; lastMouse = Input.mousePosition; following = false; }
            }
            if (Input.GetMouseButtonUp(1)) rotating = false;
            if (Input.GetMouseButtonUp(2)) panning = false;
            if (rotating)
            {
                var dm = Input.mousePosition - lastMouse; lastMouse = Input.mousePosition;
                yawGoal += dm.x * 0.25f; pitchGoal -= dm.y * 0.2f;
            }
            if (panning)
            {
                var dm = Input.mousePosition - lastMouse; lastMouse = Input.mousePosition;
                float k = Distance * 0.0022f;
                pivotGoal -= (right * dm.x + fwd * dm.y) * k;
            }
            pitchGoal = Mathf.Clamp(pitchGoal, 18f, 85f);
            distGoal = Mathf.Clamp(distGoal, MinDistance, MaxDistance);
            if (following) pivotGoal = new Vector3(follow.x, 0, follow.z);
            pivotGoal.x = Mathf.Clamp(pivotGoal.x, -50, Bounds.x + 50);
            pivotGoal.z = Mathf.Clamp(pivotGoal.z, -50, Bounds.y + 50);
            float t = 1 - Mathf.Exp(-dt * 10f);
            Pivot = Vector3.Lerp(Pivot, pivotGoal, t);
            Yaw = Mathf.LerpAngle(Yaw, yawGoal, t);
            Pitch = Mathf.Lerp(Pitch, pitchGoal, t);
            Distance = Mathf.Lerp(Distance, distGoal, t);
            Apply();
        }

        void Apply()
        {
            var rot = Quaternion.Euler(Pitch, Yaw, 0);
            transform.position = Pivot - rot * Vector3.forward * Distance;
            transform.rotation = rot;
        }

        /// <summary>Where a screen point lands on the ground (y = 0).</summary>
        public bool GroundPoint(Vector3 screen, out Vector3 hit)
        {
            var ray = Cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            float d;
            if (plane.Raycast(ray, out d)) { hit = ray.GetPoint(d); return true; }
            hit = Vector3.zero; return false;
        }
    }
}
