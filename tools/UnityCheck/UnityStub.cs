// A stand-in for the parts of UnityEngine and UnityEditor the game uses, so the game code can be compiled and its mesh
// building tested outside Unity. Maths types are real; engine objects are inert.
#pragma warning disable CS0067, CS0649, CS0414, CS0169, CS1998, CS0626
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0);
        public static Vector2 zero => new Vector2(0, 0); public static Vector2 one => new Vector2(1, 1);
    }
    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public override bool Equals(object o) => o is Vector2Int v && v == this;
        public override int GetHashCode() => x * 73856093 ^ y;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public Vector3(float x, float y) { this.x = x; this.y = y; z = 0; }
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 Min(Vector3 a, Vector3 b) => new Vector3(Math.Min(a.x, b.x), Math.Min(a.y, b.y), Math.Min(a.z, b.z));
        public static Vector3 Max(Vector3 a, Vector3 b) => new Vector3(Math.Max(a.x, b.x), Math.Max(a.y, b.y), Math.Max(a.z, b.z));
        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized { get { float m = magnitude; return m > 1e-5f ? this / m : zero; } }
        public void Normalize() { this = normalized; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) { t = Mathf.Clamp01(t); return a + (b - a) * t; }
        public override string ToString() => $"({x:0.##},{y:0.##},{z:0.##})";
    }
    public struct Vector4 { public float x, y, z, w; public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; } }
    public struct Quaternion
    {
        public float x, y, z, w;
        public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        // Unity applies Z, then X, then Y
        public static Quaternion Euler(float ex, float ey, float ez)
        {
            float d2r = (float)Math.PI / 180f * 0.5f;
            var qx = new Quaternion((float)Math.Sin(ex * d2r), 0, 0, (float)Math.Cos(ex * d2r));
            var qy = new Quaternion(0, (float)Math.Sin(ey * d2r), 0, (float)Math.Cos(ey * d2r));
            var qz = new Quaternion(0, 0, (float)Math.Sin(ez * d2r), (float)Math.Cos(ez * d2r));
            return qy * qx * qz;
        }
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v)
        {
            var u = new Vector3(q.x, q.y, q.z);
            var t = 2f * Vector3.Cross(u, v);
            return v + q.w * t + Vector3.Cross(u, t);
        }
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => t < 0.5f ? a : b;
        public static Quaternion LookRotation(Vector3 f) { float yaw = (float)(Math.Atan2(f.x, f.z) * 180 / Math.PI); return Euler(0, yaw, 0); }
    }
    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;
        public static Matrix4x4 identity => new Matrix4x4 { m00 = 1, m11 = 1, m22 = 1, m33 = 1 };
        public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s)
        {
            var ax = q * new Vector3(s.x, 0, 0); var ay = q * new Vector3(0, s.y, 0); var az = q * new Vector3(0, 0, s.z);
            return new Matrix4x4 { m00 = ax.x, m10 = ax.y, m20 = ax.z, m01 = ay.x, m11 = ay.y, m21 = ay.z, m02 = az.x, m12 = az.y, m22 = az.z, m03 = p.x, m13 = p.y, m23 = p.z, m33 = 1 };
        }
        public Vector3 MultiplyPoint3x4(Vector3 v) => new Vector3(m00 * v.x + m01 * v.y + m02 * v.z + m03, m10 * v.x + m11 * v.y + m12 * v.z + m13, m20 * v.x + m21 * v.y + m22 * v.z + m23);
        public float determinant => m00 * (m11 * m22 - m12 * m21) - m01 * (m10 * m22 - m12 * m20) + m02 * (m10 * m21 - m11 * m20);
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
        {
            var r = new Matrix4x4();
            float[,] A = a.M(), B = b.M(), R = new float[4, 4];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) for (int k = 0; k < 4; k++) R[i, j] += A[i, k] * B[k, j];
            r.m00 = R[0, 0]; r.m01 = R[0, 1]; r.m02 = R[0, 2]; r.m03 = R[0, 3]; r.m10 = R[1, 0]; r.m11 = R[1, 1]; r.m12 = R[1, 2]; r.m13 = R[1, 3];
            r.m20 = R[2, 0]; r.m21 = R[2, 1]; r.m22 = R[2, 2]; r.m23 = R[2, 3]; r.m30 = R[3, 0]; r.m31 = R[3, 1]; r.m32 = R[3, 2]; r.m33 = R[3, 3];
            return r;
        }
        float[,] M() => new float[,] { { m00, m01, m02, m03 }, { m10, m11, m12, m13 }, { m20, m21, m22, m23 }, { m30, m31, m32, m33 } };
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1); public static Color clear => new Color(0, 0, 0, 0); public static Color black => new Color(0, 0, 0); public static Color grey => new Color(0.5f, 0.5f, 0.5f); public static Color gray => grey;
        public static Color Lerp(Color a, Color b, float t) { t = Mathf.Clamp01(t); return new Color(a.r + (b.r - a.r) * t, a.g + (b.g - a.g) * t, a.b + (b.b - a.b) * t, a.a + (b.a - a.a) * t); }
        public static Color operator *(Color c, float f) => new Color(c.r * f, c.g * f, c.b * f, c.a * f);
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => new Color32((byte)(Mathf.Clamp01(c.r) * 255), (byte)(Mathf.Clamp01(c.g) * 255), (byte)(Mathf.Clamp01(c.b) * 255), (byte)(Mathf.Clamp01(c.a) * 255));
    }
    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string s, out Color c)
        {
            c = new Color(0, 0, 0, 1);
            if (string.IsNullOrEmpty(s) || s[0] != '#') return false;
            s = s.Substring(1);
            if (s.Length == 3) s = "" + s[0] + s[0] + s[1] + s[1] + s[2] + s[2];
            if (s.Length != 6) return false;
            c = new Color(Convert.ToInt32(s.Substring(0, 2), 16) / 255f, Convert.ToInt32(s.Substring(2, 2), 16) / 255f, Convert.ToInt32(s.Substring(4, 2), 16) / 255f, 1);
            return true;
        }
        public static string ToHtmlStringRGB(Color c) => ((int)(c.r * 255)).ToString("X2") + ((int)(c.g * 255)).ToString("X2") + ((int)(c.b * 255)).ToString("X2");
    }
    public static class Mathf
    {
        public const float PI = (float)Math.PI;
        public const float Rad2Deg = 57.29578f, Deg2Rad = 0.0174532924f;
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static int Clamp(int v, int a, int b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0, 1);
        public static float Sign(float f) => f >= 0 ? 1 : -1;
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float LerpAngle(float a, float b, float t) => Lerp(a, b, t);
        public static float InverseLerp(float a, float b, float v) => a == b ? 0 : Clamp01((v - a) / (b - a));
        public static float SmoothStep(float a, float b, float t) { t = Clamp01(t); t = t * t * (3 - 2 * t); return a + (b - a) * t; }
        public static float Sin(float f) => (float)Math.Sin(f); public static float Cos(float f) => (float)Math.Cos(f);
        public static float Abs(float f) => Math.Abs(f); public static int Abs(int f) => Math.Abs(f);
        public static float Min(float a, float b) => Math.Min(a, b); public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b); public static int Max(int a, int b) => Math.Max(a, b);
        public static int FloorToInt(float f) => (int)Math.Floor(f); public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
        public static float Floor(float f) => (float)Math.Floor(f); public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        public static float DeltaAngle(float a, float b) { float d = (b - a) % 360; if (d > 180) d -= 360; if (d < -180) d += 360; return d; }
        public static float Round(float f) => (float)Math.Round(f); public static float Exp(float f) => (float)Math.Exp(f); public static float Pow(float a, float b) => (float)Math.Pow(a, b);
    }
    public class Object
    {
        public string name; public HideFlags hideFlags;
        public static void Destroy(Object o) { }
        public static implicit operator bool(Object o) => !ReferenceEquals(o, null);
    }
    public enum HideFlags { None, HideAndDontSave }
    public class Component : Object
    {
        public GameObject gameObject; public Transform transform => gameObject?.transform;
        public T GetComponent<T>() where T : Component => gameObject?.GetComponent<T>();
    }
    public class Behaviour : Component { public bool enabled; }
    public class MonoBehaviour : Behaviour { }
    public class Transform : Component
    {
        public Vector3 localPosition, position; public Quaternion localRotation, rotation;
        public void SetParent(Transform t, bool w) { }
    }
    public class GameObject : Object
    {
        readonly List<Component> comps = new List<Component>();
        public Transform transform; public string tag;
        public GameObject() : this("GameObject") { }
        public GameObject(string n) { name = n; transform = new Transform(); transform.gameObject = this; }
        public T AddComponent<T>() where T : Component, new() { var c = new T(); c.gameObject = this; comps.Add(c); return c; }
        public T GetComponent<T>() where T : Component { foreach (var c in comps) if (c is T t) return t; return null; }
        public bool activeSelf;
        public void SetActive(bool b) { activeSelf = b; }
    }
    public enum CameraClearFlags { Skybox, SolidColor }
    public class Camera : Behaviour
    {
        public static Camera main; public CameraClearFlags clearFlags; public Color backgroundColor; public float nearClipPlane, farClipPlane, fieldOfView;
        public Ray ScreenPointToRay(Vector3 p) => new Ray(Vector3.zero, Vector3.forward);
        public Vector3 WorldToScreenPoint(Vector3 p) => p;
    }
    public class AudioListener : Behaviour { }
    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour { public LightType type; public LightShadows shadows; public float shadowStrength, shadowBias, shadowNormalBias, intensity; public Color color; }
    public enum FogMode { Linear = 1, Exponential, ExponentialSquared }
    public static class DynamicGI { public static void UpdateEnvironment() { } }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject, new() => new T(); }
    public static class RenderSettings { public static Light sun; public static Material skybox; public static UnityEngine.Rendering.DefaultReflectionMode defaultReflectionMode; public static float reflectionIntensity; public static UnityEngine.Rendering.AmbientMode ambientMode; public static Color ambientSkyColor, ambientEquatorColor, ambientGroundColor, fogColor; public static bool fog; public static FogMode fogMode; public static float fogStartDistance, fogEndDistance; }
    public enum ShadowQuality { Disable, HardOnly, All }
    public enum ShadowResolution { Low, Medium, High, VeryHigh }
    public static class QualitySettings { public static ShadowQuality shadows; public static ShadowResolution shadowResolution; public static float shadowDistance; public static int shadowCascades, antiAliasing; }
    public class Shader : Object { public static Shader Find(string n) => new Shader(); public static int PropertyToID(string n) => n.GetHashCode(); }
    public enum MaterialGlobalIlluminationFlags { None, RealtimeEmissive, BakedEmissive, EmissiveIsBlack }
    public class Material : Object
    {
        public Material(Shader s) { } public Material(Material m) { if (m != null) { BaseColor = m.BaseColor; name = m.name; } }
        public Color BaseColor = Color.white;   // (the check tool's preview reads it)
        public MaterialGlobalIlluminationFlags globalIlluminationFlags; public bool enableInstancing;
        public void SetFloat(string n, float v) { } public void SetColor(string n, Color c) { if (n == "_BaseColor") BaseColor = c; }
        public void SetFloat(int n, float v) { } public void SetColor(int n, Color c) { if (n == "_BaseColor".GetHashCode()) BaseColor = c; } public void SetTexture(string n, Texture t) { }
        public bool HasProperty(int n) => true; public bool HasProperty(string n) => true;
        public void EnableKeyword(string k) { } public void DisableKeyword(string k) { }
    }
    public static class Resources { public static T Load<T>(string p) where T : Object => null; }
    public class Texture : Object { }
    public enum TextureFormat { RGBA32 = 4 }
    public class Texture2D : Texture { public Texture2D(int w, int h, TextureFormat f, bool m) { } public void SetPixel(int x, int y, Color c) { } public void Apply() { } }
    public class Mesh : Object
    {
        public UnityEngine.Rendering.IndexFormat indexFormat; public int subMeshCount;
        public Vector3[] vertices = new Vector3[0], normals = new Vector3[0]; public Color32[] colors32 = new Color32[0];
        readonly List<int[]> tris = new List<int[]> { new int[0], new int[0] };
        public void SetVertices(List<Vector3> v) { vertices = v.ToArray(); }
        public void SetNormals(List<Vector3> v) { normals = v.ToArray(); }
        public void SetColors(List<Color32> v) { colors32 = v.ToArray(); }
        public Vector2[] uv = new Vector2[0];
        public void SetUVs(int ch, List<Vector2> v) { uv = v.ToArray(); }
        public void RecalculateTangents() { }
        public void SetTriangles(List<int> t, int s) { SetTriangles(t.ToArray(), s); }
        public void SetTriangles(int[] t, int s) { while (tris.Count <= s) tris.Add(new int[0]); tris[s] = t; }
        public int[] GetTriangles(int s) => s < tris.Count ? tris[s] : new int[0];
        public void RecalculateBounds() { } public void MarkDynamic() { } public void Clear() { vertices = new Vector3[0]; }
    }
    public class MeshFilter : Component { public Mesh sharedMesh; }
    public class Renderer : Component { public Material sharedMaterial; public Material[] sharedMaterials; public UnityEngine.Rendering.ShadowCastingMode shadowCastingMode; public bool receiveShadows; }
    public class MeshRenderer : Renderer { }
    public struct Ray { public Vector3 origin, direction; public Ray(Vector3 o, Vector3 d) { origin = o; direction = d; } public Vector3 GetPoint(float d) => origin + direction * d; }
    public struct Plane { public Plane(Vector3 n, Vector3 p) { } public bool Raycast(Ray r, out float d) { d = 0; return false; } }
    public static class Time { public static float unscaledDeltaTime, unscaledTime; public static int frameCount; }
    public enum KeyCode { None, W, A, S, D, Q, E, R, F, T, X, UpArrow, DownArrow, LeftArrow, RightArrow, Equals, Minus, KeypadPlus, KeypadMinus, LeftShift, Space, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Escape, Delete, F5 }
    public static class Input
    {
        public static Vector3 mousePosition; public static Vector2 mouseScrollDelta;
        public static bool GetKey(KeyCode k) => false; public static bool GetKeyDown(KeyCode k) => false;
        public static bool GetMouseButton(int b) => false; public static bool GetMouseButtonDown(int b) => false; public static bool GetMouseButtonUp(int b) => false;
    }
    public static class Screen { public static int width = 1920, height = 1080; }
    public static class Application { public static int targetFrameRate; public static string persistentDataPath = "."; }
    public static class JsonUtility { public static string ToJson(object o) => ""; public static T FromJson<T>(string s) => default(T); }
    public static class Debug { public static void LogWarning(object o) { } public static void Log(object o) { } }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad }
    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMax => x + width; public float yMax => y + height;
        public bool Contains(Vector2 p) => p.x >= x && p.x < xMax && p.y >= y && p.y < yMax;
    }
    public class RectOffset { public RectOffset(int l, int r, int t, int b) { } }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextClipping { Overflow, Clip }
    public class GUIStyleState { public Color textColor; }
    public class GUIContent { public GUIContent(string s) { } }
    public class GUIStyle
    {
        public static GUIStyle none = new GUIStyle();
        public GUIStyle() { } public GUIStyle(GUIStyle o) { }
        public int fontSize; public FontStyle fontStyle; public TextAnchor alignment; public TextClipping clipping; public bool wordWrap, richText;
        public GUIStyleState normal = new GUIStyleState(); public RectOffset padding, margin;
        public float CalcHeight(GUIContent c, float w) => 20; public Vector2 CalcSize(GUIContent c) => new Vector2(100, 20);
    }
    public class GUISkin { public GUIStyle label = new GUIStyle(); }
    public enum EventType { MouseDown, MouseUp, MouseDrag, KeyDown, KeyUp, ScrollWheel, Repaint, Layout }
    public class Event { public static Event current = new Event(); public EventType type; public int button; public Vector2 mousePosition; public void Use() { } }
    public static class GUI
    {
        public static Color color; public static bool changed; public static GUISkin skin = new GUISkin();
        public static void Label(Rect r, string s, GUIStyle st) { } public static void DrawTexture(Rect r, Texture t) { }
        public static Vector2 BeginScrollView(Rect p, Vector2 s, Rect v) => s; public static void EndScrollView() { }
    }
}
namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox, Trilight, Flat }
    public enum IndexFormat { UInt16, UInt32 }
    public enum ShadowCastingMode { Off, On }
    public enum DefaultReflectionMode { Skybox, Custom }
    public class VolumeParameter<T> { public T value; public bool overrideState; public void Override(T v) { value = v; overrideState = true; } }
    public class VolumeComponent : ScriptableObject { }
    public class VolumeProfile : ScriptableObject { public T Add<T>(bool overrides = false) where T : VolumeComponent, new() => new T(); }
    public class Volume : Behaviour { public bool isGlobal; public float priority; public VolumeProfile sharedProfile; }
}
namespace UnityEngine.Rendering.Universal
{
    public enum TonemappingMode { None, Neutral, ACES }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public class Tonemapping : VolumeComponent { public VolumeParameter<TonemappingMode> mode = new VolumeParameter<TonemappingMode>(); }
    public class Bloom : VolumeComponent { public VolumeParameter<float> threshold = new VolumeParameter<float>(), intensity = new VolumeParameter<float>(), scatter = new VolumeParameter<float>(); public VolumeParameter<bool> highQualityFiltering = new VolumeParameter<bool>(); }
    public class ColorAdjustments : VolumeComponent { public VolumeParameter<float> contrast = new VolumeParameter<float>(), saturation = new VolumeParameter<float>(), postExposure = new VolumeParameter<float>(); public VolumeParameter<Color> colorFilter = new VolumeParameter<Color>(); }
    public class WhiteBalance : VolumeComponent { public VolumeParameter<float> temperature = new VolumeParameter<float>(); }
    public class ShadowsMidtonesHighlights : VolumeComponent { public VolumeParameter<Vector4> shadows = new VolumeParameter<Vector4>(), highlights = new VolumeParameter<Vector4>(); }
    public class Vignette : VolumeComponent { public VolumeParameter<float> intensity = new VolumeParameter<float>(), smoothness = new VolumeParameter<float>(); }
    public class UniversalAdditionalCameraData : Behaviour { public bool renderPostProcessing, renderShadows; public AntialiasingMode antialiasing; }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) => new UniversalAdditionalCameraData(); }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { public string path; public bool isDirty; }
    public static class SceneManager { public static Scene GetActiveScene() => new Scene(); }
}
namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Class)] public class InitializeOnLoadAttribute : Attribute { }
    public static class EditorApplication { public static Action delayCall; public static bool isPlayingOrWillChangePlaymode; }
    public static class EditorPrefs { public static int GetInt(string k, int d) => d; public static void SetInt(string k, int v) { } }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p, bool e) { } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes; }
    public static class PlayerSettings { public static string productName, companyName; }
    public static class AssetDatabase { public static void SaveAssets() { } public static void Refresh() { } }
    public static class AssemblyReloadEvents { public static Action beforeAssemblyReload; }
}
namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public static class EditorSceneManager
    {
        public static UnityEngine.SceneManagement.Scene NewScene(NewSceneSetup s, NewSceneMode m) => new UnityEngine.SceneManagement.Scene();
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string p) => true;
        public static UnityEngine.SceneManagement.Scene OpenScene(string p) => new UnityEngine.SceneManagement.Scene();
    }
}
