using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RealLife.EditorTools
{
    /// <summary>
    /// Graphics setup (ported from Mobland), run automatically when something is missing and from the Real Life menu:
    ///  * a URP pipeline asset (Forward+, HDR, soft shadows, SSAO) on Graphics and every Quality level
    ///  * linear colour space
    ///  * a material in Resources/RealLife/Mat for every generated texture set in Resources/RealLife/Tex
    /// </summary>
    [InitializeOnLoad]
    static class GraphicsSetup
    {
        const string SettingsFolder = "Assets/Settings";
        const string PipelinePath = SettingsFolder + "/RealLifeURP.asset";
        const string RendererPath = SettingsFolder + "/RealLifeURP_Renderer.asset";
        const string MatFolder = "Assets/Resources/RealLife/Mat";
        public const string TexFolder = "Assets/Resources/RealLife/Tex";
        const string SessionKey = "RealLife.GraphicsChecked";

        static GraphicsSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (SessionState.GetBool(SessionKey, false)) return;
                SessionState.SetBool(SessionKey, true);
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                try
                {
                    if (GraphicsSettings.defaultRenderPipeline == null || !File.Exists(PipelinePath)) SetupPipeline();
                    if (NeedsMaterials()) BuildMaterials();
                }
                catch (Exception e) { Debug.LogException(e); }
            };
        }

        // ------------------------------------------------------------------ pipeline

        [MenuItem("Real Life/Graphics/Set Up URP Pipeline")]
        public static void SetupPipeline()
        {
            Directory.CreateDirectory(SettingsFolder);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }
            var rso = new SerializedObject(renderer);
            SetInt(rso, "m_RenderingMode", 2);          // Forward+
            SetInt(rso, "m_DepthPrimingMode", 0);
            rso.ApplyModifiedPropertiesWithoutUndo();
            AddSsao(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            var pso = new SerializedObject(pipeline);
            SetBool(pso, "m_SupportsHDR", true);
            SetInt(pso, "m_MSAA", 4);
            SetFloat(pso, "m_RenderScale", 1f);
            SetBool(pso, "m_RequireDepthTexture", true);
            SetBool(pso, "m_MainLightShadowsSupported", true);
            SetInt(pso, "m_MainLightShadowmapResolution", 4096);
            SetFloat(pso, "m_ShadowDistance", 900f);
            SetInt(pso, "m_ShadowCascadeCount", 4);
            SetBool(pso, "m_SoftShadowsSupported", true);
            SetInt(pso, "m_SoftShadowQuality", 2);
            SetInt(pso, "m_AdditionalLightsRenderingMode", 1);
            SetInt(pso, "m_AdditionalLightsPerObjectLimit", 8);
            SetBool(pso, "m_AdditionalLightShadowsSupported", false);
            SetInt(pso, "m_ColorGradingMode", 1);               // HDR grading
            SetInt(pso, "m_ColorGradingLutSize", 32);
            pso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();
            Debug.Log("[Real Life] URP pipeline configured (Forward+, HDR, soft shadows, SSAO).");
        }

        static void AddSsao(UniversalRendererData renderer)
        {
            if (renderer.rendererFeatures.Any(f => f != null && f.GetType().Name == "ScreenSpaceAmbientOcclusion")) return;
            Type t = typeof(UniversalRendererData).Assembly.GetTypes().FirstOrDefault(x => x.Name == "ScreenSpaceAmbientOcclusion");
            if (t == null) { Debug.LogWarning("[Real Life] SSAO feature type not found."); return; }
            var feature = (ScriptableRendererFeature)ScriptableObject.CreateInstance(t);
            feature.name = "SSAO";
            AssetDatabase.AddObjectToAsset(feature, renderer);
            AssetDatabase.SaveAssets();
            var so = new SerializedObject(renderer);
            var list = so.FindProperty("m_RendererFeatures");
            var map = so.FindProperty("m_RendererFeatureMap");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
            if (map != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId))
            {
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            var fso = new SerializedObject(feature);
            SetFloat(fso, "m_Settings.Intensity", 1.1f);
            SetFloat(fso, "m_Settings.Radius", 0.9f);
            SetFloat(fso, "m_Settings.DirectLightingStrength", 0.3f);
            SetFloat(fso, "m_Settings.Falloff", 250f);
            fso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);
        }

        // ------------------------------------------------------------------ materials

        static IEnumerable<string> AlbedoTextures()
        {
            if (!Directory.Exists(TexFolder)) yield break;
            foreach (var f in Directory.GetFiles(TexFolder, "*.png"))
            {
                string n = Path.GetFileNameWithoutExtension(f);
                if (n.EndsWith("_n") || n.EndsWith("_m") || n.EndsWith("_e")) continue;
                yield return n;
            }
        }

        static bool NeedsMaterials()
        {
            if (!File.Exists(MatFolder + "/_LitTemplate.mat")) return true;
            foreach (var n in AlbedoTextures())
            {
                string mat = $"{MatFolder}/{n}.mat";
                if (!File.Exists(mat)) return true;
                if (File.GetLastWriteTimeUtc($"{TexFolder}/{n}.png") > File.GetLastWriteTimeUtc(mat)) return true;
            }
            return false;
        }

        [MenuItem("Real Life/Graphics/Rebuild Materials")]
        public static void BuildMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) { Debug.LogError("[Real Life] URP Lit shader not found - is URP installed?"); return; }
            Directory.CreateDirectory(MatFolder);
            foreach (var f in Directory.GetFiles(TexFolder, "*.png"))
            {
                var ti = AssetImporter.GetAtPath(f.Replace('\\', '/')) as TextureImporter;
                if (ti != null && TextureImport.Configure(ti, f)) ti.SaveAndReimport();
            }
            var template = LoadOrCreate($"{MatFolder}/_LitTemplate.mat", lit);
            template.SetColor("_BaseColor", Color.white);
            template.SetFloat("_Smoothness", 0.2f);
            EditorUtility.SetDirty(template);

            int count = 0;
            foreach (var n in AlbedoTextures())
            {
                var m = LoadOrCreate($"{MatFolder}/{n}.mat", lit);
                Texture2D Tex(string suffix) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexFolder}/{n}{suffix}.png");
                m.SetColor("_BaseColor", Color.white);
                m.SetTexture("_BaseMap", Tex(""));
                var nrm = Tex("_n");
                m.SetTexture("_BumpMap", nrm);
                m.SetFloat("_BumpScale", 1f);
                SetKeyword(m, "_NORMALMAP", nrm != null);
                var mask = Tex("_m");
                m.SetTexture("_MetallicGlossMap", mask);
                m.SetFloat("_Smoothness", 1f);
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_SmoothnessTextureChannel", 0f);
                SetKeyword(m, "_METALLICSPECGLOSSMAP", mask != null);
                var emit = Tex("_e");
                m.SetTexture("_EmissionMap", emit);
                m.SetColor("_EmissionColor", Color.black);
                SetKeyword(m, "_EMISSION", emit != null);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Real Life] Built {count} surface materials.");
        }

        static Material LoadOrCreate(string path, Shader shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        static void SetKeyword(Material m, string k, bool on) { if (on) m.EnableKeyword(k); else m.DisableKeyword(k); }

        static void SetInt(SerializedObject so, string prop, int v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.Log($"[Real Life] (setup) property not found: {prop}"); return; }
            p.intValue = v;
        }

        static void SetBool(SerializedObject so, string prop, bool v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.Log($"[Real Life] (setup) property not found: {prop}"); return; }
            p.boolValue = v;
        }

        static void SetFloat(SerializedObject so, string prop, float v)
        {
            var p = so.FindProperty(prop);
            if (p == null) { Debug.Log($"[Real Life] (setup) property not found: {prop}"); return; }
            p.floatValue = v;
        }
    }

    /// <summary>Import settings for generated textures, by suffix (_n normal, _m mask, _e emission).</summary>
    sealed class TextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(GraphicsSetup.TexFolder)) return;
            Configure((TextureImporter)assetImporter, assetPath);
        }

        public static bool Configure(TextureImporter ti, string path)
        {
            string file = Path.GetFileNameWithoutExtension(path);
            bool changed = false;
            void Set<T>(T current, T value, Action<T> apply) { if (!Equals(current, value)) { apply(value); changed = true; } }
            var type = file.EndsWith("_n") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            bool srgb = !(file.EndsWith("_n") || file.EndsWith("_m"));
            bool alpha = file.EndsWith("_m");
            Set(ti.textureType, type, v => ti.textureType = v);
            Set(ti.sRGBTexture, srgb, v => ti.sRGBTexture = v);
            Set(ti.wrapMode, TextureWrapMode.Repeat, v => ti.wrapMode = v);
            Set(ti.anisoLevel, 8, v => ti.anisoLevel = v);
            Set(ti.mipmapEnabled, true, v => ti.mipmapEnabled = v);
            Set(ti.maxTextureSize, 2048, v => ti.maxTextureSize = v);
            Set(ti.alphaSource, alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None, v => ti.alphaSource = v);
            Set(ti.filterMode, FilterMode.Trilinear, v => ti.filterMode = v);
            return changed;
        }
    }
}
