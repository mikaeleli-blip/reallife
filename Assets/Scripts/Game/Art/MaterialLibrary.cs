using System.Collections.Generic;
using UnityEngine;

namespace RealLife.Game.Art
{
    /// <summary>
    /// Runtime materials (ported from Mobland).
    /// * Surface(name): textured PBR material built in the editor from tools/textures output (Resources/RealLife/Mat/name),
    ///   instanced at runtime so night emission never touches the asset.
    /// * Tinted(name, tint): a surface multiplied by a colour (house siding, roofs, grass by season).
    /// * Get(colour): plain coloured URP Lit material for small parts (trees, cars, people, trim).
    /// * Glow / Pane: things that light up at night (lamps, lit windows); SetNight drives them all.
    /// </summary>
    public static class MaterialLibrary
    {
        static Material template;
        static readonly Dictionary<string, Material> surfaces = new Dictionary<string, Material>();
        static readonly Dictionary<long, Material> colors = new Dictionary<long, Material>();
        static readonly Dictionary<long, Material> panes = new Dictionary<long, Material>();
        static readonly Dictionary<string, Material> glows = new Dictionary<string, Material>();
        static readonly List<NightGlow> nightEmissive = new List<NightGlow>();
        static float night = -1f;

        struct NightGlow { public Material Mat; public Color Color; public float Intensity; }

        public static float Night { get { return Mathf.Max(0, night); } }

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        static readonly int Smoothness = Shader.PropertyToID("_Smoothness");
        static readonly int Metallic = Shader.PropertyToID("_Metallic");

        /// <summary>Textured surface by texture-set name (e.g. "fac_apt_brick"). Never null.</summary>
        public static Material Surface(string name, float nightEmission = 0f)
        {
            Material m;
            if (surfaces.TryGetValue(name, out m) && m != null) return m;
            var asset = Resources.Load<Material>("RealLife/Mat/" + name);
            if (asset != null) m = new Material(asset) { name = name };
            else
            {
                Debug.LogWarning("[Real Life] Missing material '" + name + "'. Use Real Life > Graphics > Rebuild Materials.");
                m = new Material(Template()) { name = name + " (missing)" };
                SetColor(m, new Color(0.6f, 0.2f, 0.6f));
            }
            surfaces[name] = m;
            if (nightEmission > 0f && m.HasProperty(EmissionColor))
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                RegisterNight(m, Color.white, nightEmission);
            }
            return m;
        }

        /// <summary>A textured surface multiplied by a tint. Cached per name and tint.</summary>
        public static Material Tinted(string name, Color tint, float nightEmission = 0f)
        {
            string key = name + "#" + ColorUtility.ToHtmlStringRGB(tint) + "#" + nightEmission;
            Material m;
            if (surfaces.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Surface(name)) { name = key };
            SetColor(m, tint);
            if (nightEmission > 0f && m.HasProperty(EmissionColor))
            {
                m.EnableKeyword("_EMISSION");
                RegisterNight(m, Color.white, nightEmission);
            }
            surfaces[key] = m;
            return m;
        }

        /// <summary>A textured surface that never lights up (a closed shop's windows and sign).</summary>
        public static Material Dark(string name, Color tint)
        {
            string key = name + "#dark#" + ColorUtility.ToHtmlStringRGB(tint);
            Material m;
            if (surfaces.TryGetValue(key, out m) && m != null) return m;
            var asset = Resources.Load<Material>("RealLife/Mat/" + name);
            m = asset != null ? new Material(asset) { name = key } : new Material(Template()) { name = key };
            SetColor(m, tint);
            m.DisableKeyword("_EMISSION");
            if (m.HasProperty(EmissionColor)) m.SetColor(EmissionColor, Color.black);
            surfaces[key] = m;
            return m;
        }

        /// <summary>Plain coloured lit material (cached by colour, smoothness and metallic).</summary>
        public static Material Get(Color32 c, float smoothness = 0.2f, float metallic = 0f)
        {
            long key = ((long)c.r << 32) | ((long)c.g << 24) | ((long)c.b << 16) | ((long)Mathf.RoundToInt(smoothness * 100) << 8) | (long)Mathf.RoundToInt(metallic * 100);
            Material m;
            if (colors.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Template()) { name = "Col_" + c.r.ToString("X2") + c.g.ToString("X2") + c.b.ToString("X2") };
            SetColor(m, (Color)c);
            if (m.HasProperty(Smoothness)) m.SetFloat(Smoothness, smoothness);
            if (m.HasProperty(Metallic)) m.SetFloat(Metallic, metallic);
            m.enableInstancing = true;
            colors[key] = m;
            return m;
        }

        /// <summary>A material that glows at night (lamp bulbs, lit signs). Intensity is HDR. Cached by colours and intensity.</summary>
        public static Material Glow(Color baseColor, Color glowColor, float intensity)
        {
            string key = ColorUtility.ToHtmlStringRGB(baseColor) + ColorUtility.ToHtmlStringRGB(glowColor) + Mathf.RoundToInt(intensity * 100);
            Material m;
            if (glows.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Template()) { name = "Glow" };
            glows[key] = m;
            SetColor(m, baseColor);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            RegisterNight(m, glowColor, intensity);
            return m;
        }

        /// <summary>A window pane: dark glass, or (for a light colour) glass that glows that colour at night.</summary>
        public static Material Pane(Color32 c)
        {
            long key = ((long)c.r << 16) | ((long)c.g << 8) | c.b;
            Material m;
            if (panes.TryGetValue(key, out m) && m != null) return m;
            bool lit = c.r + c.g + c.b > 300;
            if (lit)
            {
                m = Glow(new Color(0.22f, 0.27f, 0.32f), (Color)c, 1.6f);
                if (m.HasProperty(Smoothness)) m.SetFloat(Smoothness, 0.9f);
            }
            else
            {
                m = new Material(Template()) { name = "Glass" };
                SetColor(m, new Color(0.16f, 0.21f, 0.27f));
                if (m.HasProperty(Smoothness)) m.SetFloat(Smoothness, 0.92f);
                if (m.HasProperty(Metallic)) m.SetFloat(Metallic, 0.2f);
            }
            panes[key] = m;
            return m;
        }

        public static void RegisterNight(Material m, Color color, float intensity)
        {
            nightEmissive.Add(new NightGlow { Mat = m, Color = color, Intensity = intensity });
            Apply(m, color, intensity);
        }

        /// <summary>0 full day .. 1 full night. Drives every registered emissive material.</summary>
        public static void SetNight(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(value - night) < 0.002f) return;
            night = value;
            for (int i = nightEmissive.Count - 1; i >= 0; i--)
            {
                var e = nightEmissive[i];
                if (e.Mat == null) { nightEmissive.RemoveAt(i); continue; }
                Apply(e.Mat, e.Color, e.Intensity);
            }
        }

        static void Apply(Material m, Color color, float intensity)
        {
            float k = Mathf.SmoothStep(0f, 1f, Night);
            m.SetColor(EmissionColor, color * (intensity * k));
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty(BaseColor)) m.SetColor(BaseColor, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        public static Material Template()
        {
            if (template != null) return template;
            template = Resources.Load<Material>("RealLife/Mat/_LitTemplate");
            if (template == null)
            {
                var s = Shader.Find("Universal Render Pipeline/Lit");
                if (s == null) s = Shader.Find("Standard");
                template = new Material(s);
            }
            return template;
        }

        /// <summary>See-through, unlit vertex colour (zones, selection, previews).</summary>
        public static Material Overlay()
        {
            var s = Resources.Load<Shader>("RealLife/Overlay");
            if (s == null) s = Shader.Find("RealLife/Overlay");
            if (s == null) s = Shader.Find("Sprites/Default");
            return new Material(s);
        }
    }
}
