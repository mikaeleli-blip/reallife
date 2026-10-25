using RealLife.Game.Art;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealLife.Game
{
    /// <summary>
    /// Sun, sky, ambient light, haze and the post-processing grade for the town's hour and season (ported from Mobland's
    /// TimeOfDay, graded brighter and cleaner for a modern town). Drives the night factor that lights windows, lamps and signs.
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        public Light Sun { get; private set; }
        public float Night { get; private set; }
        public float Daylight { get; private set; }

        Camera cam;
        Material sky;
        Bloom bloom;
        ColorAdjustments color;
        Vignette vignette;
        WhiteBalance white;
        float envHour = -99f, lastEnv = -99f;
        int envSeason = -1;

        public void Init(Camera camera)
        {
            cam = camera;
            var sg = new GameObject("Sun");
            sg.transform.SetParent(transform, false);
            Sun = sg.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.8f;
            Sun.shadowBias = 0.05f;
            Sun.shadowNormalBias = 0.4f;
            RenderSettings.sun = Sun;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                sky = new Material(skyShader) { name = "Real Life Sky" };
                sky.SetFloat("_SunSize", 0.03f);
                sky.SetFloat("_SunSizeConvergence", 6f);
                sky.SetColor("_GroundColor", new Color(0.42f, 0.45f, 0.4f));
                RenderSettings.skybox = sky;
                cam.clearFlags = CameraClearFlags.Skybox;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.None;
            camData.renderShadows = true;

            var go = new GameObject("Post Processing");
            go.transform.SetParent(transform, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.35f);
            bloom.scatter.Override(0.7f);
            bloom.highQualityFiltering.Override(true);
            color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(12f);
            color.saturation.Override(0f);
            color.postExposure.Override(0.35f);
            white = profile.Add<WhiteBalance>(true);
            white.temperature.Override(3f);
            var smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.Override(new Vector4(0.96f, 1.0f, 1.07f, 0f));
            smh.highlights.Override(new Vector4(1.04f, 1.0f, 0.96f, 0f));
            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.2f);
            vignette.smoothness.Override(0.45f);
        }

        static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }
        static readonly Color SunLow = Hex("#ff9a5a"), SunMid = Hex("#ffe0b8"), SunHigh = Hex("#fff6ea"), MoonColor = Hex("#8ea6d6");

        /// <summary>Sets everything for the town's hour (0..24) and season (0 winter .. 3 autumn); viewDistance scales the haze.</summary>
        public void Apply(float hour, int season, float viewDistance)
        {
            if (Sun == null) return;
            // short days in winter, long in summer
            float rise = season == 0 ? 7.5f : season == 2 ? 5.2f : 6.2f, set = season == 0 ? 17.5f : season == 2 ? 21.3f : 19.6f;
            float t = (hour - rise) / (set - rise);
            float sunSin = Mathf.Sin(t * Mathf.PI);
            if (t < 0f || t > 1f) sunSin = -Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * 0.6f - 0.05f;
            float peak = season == 0 ? 32f : season == 2 ? 62f : 50f;
            float elev = Mathf.Max(sunSin, 0f) * peak;
            float yaw = Mathf.Lerp(-100f, 100f, Mathf.Clamp01(t)) + 30f;

            Daylight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.06f, 0.2f, sunSin));
            Night = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.04f, 0.3f, sunSin));
            float golden = Mathf.Clamp01(1f - Mathf.Abs(sunSin - 0.12f) / 0.2f) * Daylight;

            var sunRot = Quaternion.Euler(Mathf.Max(elev, 10f), yaw, 0f);
            var moonRot = Quaternion.Euler(40f, 190f, 0f);
            Sun.transform.rotation = Quaternion.Slerp(moonRot, sunRot, Daylight);
            Color sunCol = elev < 18f ? Color.Lerp(SunLow, SunMid, elev / 18f) : Color.Lerp(SunMid, SunHigh, (elev - 18f) / 37f);
            Sun.color = Color.Lerp(MoonColor, sunCol, Daylight);
            Sun.intensity = Mathf.Lerp(0.28f, 1.3f + 0.4f * Mathf.Clamp01(elev / 40f), Daylight);
            Sun.shadowStrength = Mathf.Lerp(0.55f, 0.82f, Daylight);

            bool snow = season == 0;
            Color skyDay = snow ? Hex("#a9b8c8") : Hex("#9fb8d6"), skyDusk = Hex("#b98d7c"), skyNight = Hex("#323e60");
            Color eqDay = snow ? Hex("#a3a7ab") : Hex("#949386"), eqDusk = Hex("#8a6a58"), eqNight = Hex("#262c42");
            Color grDay = snow ? Hex("#8e9298") : Hex("#4f4a40"), grNight = Hex("#0b0c10");
            RenderSettings.ambientSkyColor = Color.Lerp(Color.Lerp(skyNight, skyDay, Daylight), skyDusk, golden * 0.5f);
            RenderSettings.ambientEquatorColor = Color.Lerp(Color.Lerp(eqNight, eqDay, Daylight), eqDusk, golden * 0.5f);
            RenderSettings.ambientGroundColor = Color.Lerp(grNight, grDay, Daylight);
            RenderSettings.reflectionIntensity = Mathf.Lerp(0.3f, 0.9f, Daylight);

            Color fogDay = snow ? Hex("#c9d2dc") : Hex("#b9c8d6"), fogDusk = Hex("#c99a7e"), fogNight = Hex("#151b28");
            RenderSettings.fogColor = Color.Lerp(Color.Lerp(fogNight, fogDay, Daylight), fogDusk, golden * 0.7f);
            float near = Mathf.Max(350f, viewDistance * 1.8f), far = Mathf.Max(1400f, viewDistance * 5f + 600f);
            RenderSettings.fogStartDistance = near * Mathf.Lerp(0.7f, 1f, Daylight);
            RenderSettings.fogEndDistance = far * Mathf.Lerp(0.8f, 1f, Daylight);

            if (sky != null)
            {
                sky.SetFloat("_Exposure", Mathf.Lerp(0.05f, 1.25f, Daylight));
                sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.5f, 0.9f, Daylight) + golden * 0.5f);
                sky.SetColor("_SkyTint", Color.Lerp(Hex("#3a4a6a"), snow ? Hex("#a7afb9") : Hex("#8f9fb5"), Daylight));
                sky.SetColor("_GroundColor", Color.Lerp(Hex("#101318"), snow ? Hex("#b8bec4") : Hex("#6b7466"), Daylight));
            }
            if (color != null)
            {
                color.postExposure.value = Mathf.Lerp(0.3f, 0.8f, Night);
                color.saturation.value = Mathf.Lerp(snow ? -6f : 0f, -14f, Night);
                color.colorFilter.value = Color.Lerp(Color.white, Hex("#c9d6ff"), Night * 0.55f);
                bloom.intensity.value = Mathf.Lerp(0.3f, 1.0f, Night);
                bloom.threshold.value = Mathf.Lerp(1.05f, 0.85f, Night);
                white.temperature.value = Mathf.Lerp(3f, -8f, Night) + golden * 10f;
                vignette.intensity.value = Mathf.Lerp(0.18f, 0.3f, Night);
            }

            MaterialLibrary.SetNight(Night);

            // refresh reflections now and then
            if (season != envSeason || (Mathf.Abs(Mathf.DeltaAngle(hour * 15f, envHour * 15f)) > 7.5f && Time.unscaledTime - lastEnv > 1f))
            {
                envHour = hour; envSeason = season; lastEnv = Time.unscaledTime;
                DynamicGI.UpdateEnvironment();
            }
        }
    }
}
