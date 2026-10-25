using System.Collections.Generic;
using UnityEngine;

namespace RealLife.Game.UI
{
    /// <summary>
    /// A small immediate-mode UI kit on IMGUI with a clean, modern look: dark glass panels, flat buttons, bars, sliders and
    /// little charts. Everything is laid out in "design pixels" at 1080p and scaled to the screen.
    /// </summary>
    public static class Ui
    {
        public static readonly Color Panel = new Color32(22, 27, 34, 238), Panel2 = new Color32(32, 39, 48, 255), Border = new Color32(58, 70, 86, 255),
            Text = new Color32(232, 237, 242, 255), Dim = new Color32(150, 163, 178, 255), Accent = new Color32(79, 163, 255, 255),
            Good = new Color32(92, 209, 122, 255), Bad = new Color32(255, 107, 107, 255), Warn = new Color32(255, 196, 77, 255),
            Hover = new Color32(46, 56, 70, 255), Pressed = new Color32(60, 74, 92, 255), On = new Color32(40, 92, 160, 255);

        static Texture2D white;
        static GUIStyle label, small, title, bold, right, button, wrap, center, big;
        static int builtFor = -1;
        public static float S { get; private set; } = 1f;

        public static Texture2D White
        {
            get
            {
                if (white == null) { white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave }; white.SetPixel(0, 0, Color.white); white.Apply(); }
                return white;
            }
        }

        /// <summary>Call at the top of OnGUI: sets the scale and (re)builds the styles for it.</summary>
        public static void Begin()
        {
            S = Mathf.Clamp(Screen.height / 1080f, 0.75f, 2.2f);
            int key = Mathf.RoundToInt(S * 100);
            if (key == builtFor && label != null) return;
            builtFor = key;
            label = Style(15, FontStyle.Normal, Text, TextAnchor.MiddleLeft);
            small = Style(13, FontStyle.Normal, Dim, TextAnchor.MiddleLeft);
            title = Style(20, FontStyle.Bold, Text, TextAnchor.MiddleLeft);
            big = Style(26, FontStyle.Bold, Text, TextAnchor.MiddleLeft);
            bold = Style(15, FontStyle.Bold, Text, TextAnchor.MiddleLeft);
            right = Style(15, FontStyle.Normal, Text, TextAnchor.MiddleRight);
            center = Style(15, FontStyle.Normal, Text, TextAnchor.MiddleCenter);
            wrap = Style(14, FontStyle.Normal, Text, TextAnchor.UpperLeft); wrap.wordWrap = true;
            button = Style(14, FontStyle.Bold, Text, TextAnchor.MiddleCenter);
        }

        static GUIStyle Style(int size, FontStyle fs, Color c, TextAnchor a)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(size * S), fontStyle = fs, alignment = a, clipping = TextClipping.Clip, wordWrap = false, richText = true };
            s.normal.textColor = c;
            s.padding = new RectOffset(0, 0, 0, 0); s.margin = new RectOffset(0, 0, 0, 0);
            return s;
        }

        /// <summary>A design-pixel rect scaled to the screen.</summary>
        public static Rect R(float x, float y, float w, float h) { return new Rect(x * S, y * S, w * S, h * S); }

        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color; GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = old;
        }

        public static void Frame(Rect r, Color fill, Color edge)
        {
            Fill(r, fill);
            float t = Mathf.Max(1, S);
            Fill(new Rect(r.x, r.y, r.width, t), edge); Fill(new Rect(r.x, r.yMax - t, r.width, t), edge);
            Fill(new Rect(r.x, r.y, t, r.height), edge); Fill(new Rect(r.xMax - t, r.y, t, r.height), edge);
        }

        public static void PanelBox(Rect r) { Frame(r, Panel, Border); }

        public static void Label(Rect r, string s) { GUI.Label(r, s, label); }
        public static void Label(Rect r, string s, Color c) { var o = label.normal.textColor; label.normal.textColor = c; GUI.Label(r, s, label); label.normal.textColor = o; }
        public static void Small(Rect r, string s) { GUI.Label(r, s, small); }
        public static void Small(Rect r, string s, Color c) { var o = small.normal.textColor; small.normal.textColor = c; GUI.Label(r, s, small); small.normal.textColor = o; }
        public static void Title(Rect r, string s) { GUI.Label(r, s, title); }
        public static void Big(Rect r, string s) { GUI.Label(r, s, big); }
        public static void Bold(Rect r, string s) { GUI.Label(r, s, bold); }
        public static void Bold(Rect r, string s, Color c) { var o = bold.normal.textColor; bold.normal.textColor = c; GUI.Label(r, s, bold); bold.normal.textColor = o; }
        public static void Right(Rect r, string s) { GUI.Label(r, s, right); }
        public static void Right(Rect r, string s, Color c) { var o = right.normal.textColor; right.normal.textColor = c; GUI.Label(r, s, right); right.normal.textColor = o; }
        public static void Center(Rect r, string s) { GUI.Label(r, s, center); }
        public static void Center(Rect r, string s, Color c) { var o = center.normal.textColor; center.normal.textColor = c; GUI.Label(r, s, center); center.normal.textColor = o; }
        public static float Wrap(Rect r, string s) { float h = wrap.CalcHeight(new GUIContent(s), r.width); GUI.Label(new Rect(r.x, r.y, r.width, h), s, wrap); return h; }
        public static float WrapHeight(string s, float width) { return wrap.CalcHeight(new GUIContent(s), width); }

        /// <summary>A flat button. Returns true when clicked.</summary>
        public static bool Button(Rect r, string s, bool on = false, bool enabled = true)
        {
            var e = Event.current;
            bool over = r.Contains(e.mousePosition);
            Fill(r, on ? On : over && enabled ? Hover : Panel2);
            if (on) Fill(new Rect(r.x, r.yMax - 2 * S, r.width, 2 * S), Accent);
            var o = button.normal.textColor; button.normal.textColor = enabled ? Text : Dim;
            GUI.Label(r, s, button);
            button.normal.textColor = o;
            if (enabled && e.type == EventType.MouseDown && e.button == 0 && over) { e.Use(); return true; }
            return false;
        }

        /// <summary>Text that acts as a link (for names of people and places).</summary>
        public static bool Link(Rect r, string s, Color? c = null)
        {
            var e = Event.current;
            bool over = r.Contains(e.mousePosition);
            Label(r, s, over ? Accent : c ?? Text);
            if (e.type == EventType.MouseDown && e.button == 0 && over) { e.Use(); return true; }
            return false;
        }

        public static void Bar(Rect r, float frac, Color c)
        {
            Fill(r, Panel2);
            Fill(new Rect(r.x, r.y, r.width * Mathf.Clamp01(frac), r.height), c);
        }

        /// <summary>A slider. Returns the new value.</summary>
        public static float Slider(Rect r, float v, float min, float max, int id)
        {
            var e = Event.current;
            var track = new Rect(r.x, r.y + r.height / 2 - 3 * S, r.width, 6 * S);
            Fill(track, Panel2);
            float f = Mathf.InverseLerp(min, max, v);
            Fill(new Rect(track.x, track.y, track.width * f, track.height), Accent);
            var knob = new Rect(track.x + track.width * f - 7 * S, r.y + r.height / 2 - 8 * S, 14 * S, 16 * S);
            Fill(knob, Text);
            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition)) { dragging = id; e.Use(); }
            if (e.type == EventType.MouseUp && dragging == id) { dragging = -1; }
            if (dragging == id && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp))
            {
                v = Mathf.Lerp(min, max, Mathf.InverseLerp(track.x, track.xMax, e.mousePosition.x));
                if (e.type == EventType.MouseDrag) e.Use();
                GUI.changed = true;
            }
            return v;
        }
        static int dragging = -1;
        public static bool Dragging { get { return dragging >= 0; } }

        /// <summary>A little line chart drawn with bars.</summary>
        public static void Chart(Rect r, IList<float> values, Color c, bool zeroBased = true)
        {
            Fill(r, new Color(0, 0, 0, 0.2f));
            if (values == null || values.Count < 2) return;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var v in values) { lo = Mathf.Min(lo, v); hi = Mathf.Max(hi, v); }
            if (zeroBased) lo = Mathf.Min(0, lo);
            if (hi - lo < 1e-4f) hi = lo + 1;
            int n = values.Count;
            float bw = r.width / n;
            for (int i = 0; i < n; i++)
            {
                float f = (values[i] - lo) / (hi - lo);
                float h = Mathf.Max(1, f * r.height);
                Fill(new Rect(r.x + i * bw, r.yMax - h, Mathf.Max(1, bw - (bw > 3 ? 1 : 0)), h), new Color(c.r, c.g, c.b, i == n - 1 ? 1f : 0.65f));
            }
            if (lo < 0) { float z = r.yMax - (0 - lo) / (hi - lo) * r.height; Fill(new Rect(r.x, z, r.width, 1), Dim); }
        }

        public static void Tooltip(Vector2 mouse, string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            float w = Mathf.Min(360 * S, wrap.CalcSize(new GUIContent(s)).x + 20 * S);
            float h = wrap.CalcHeight(new GUIContent(s), w - 16 * S) + 12 * S;
            var r = new Rect(mouse.x + 18 * S, mouse.y + 18 * S, w, h);
            if (r.xMax > Screen.width) r.x = mouse.x - w - 8 * S;
            if (r.yMax > Screen.height) r.y = mouse.y - h - 8 * S;
            Frame(r, new Color32(14, 18, 24, 245), Border);
            GUI.Label(new Rect(r.x + 8 * S, r.y + 6 * S, r.width - 16 * S, r.height - 12 * S), s, wrap);
        }

        public static string Hex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
        public static string Col(string s, Color c) { return "<color=" + Hex(c) + ">" + s + "</color>"; }
    }

    /// <summary>Rows down a panel, in design pixels.</summary>
    public sealed class Rows
    {
        public float X, Y, W;
        public Rows(float x, float y, float w) { X = x; Y = y; W = w; }
        public Rect Next(float h, float gap = 4) { var r = Ui.R(X, Y, W, h); Y += h + gap; return r; }
        public Rect Peek(float h) { return Ui.R(X, Y, W, h); }
        public void Space(float h) { Y += h; }
        /// <summary>A label on the left and a value on the right.</summary>
        public void Pair(string k, string v, Color? vc = null)
        {
            var r = Next(22, 2);
            Ui.Small(r, k);
            if (vc.HasValue) Ui.Right(r, v, vc.Value); else Ui.Right(r, v);
        }
        public void Header(string s)
        {
            Space(6);
            var r = Next(24, 4);
            Ui.Bold(r, s);
            Ui.Fill(new Rect(r.x, r.yMax, r.width, Mathf.Max(1, Ui.S)), Ui.Border);
            Space(2);
        }
    }
}
