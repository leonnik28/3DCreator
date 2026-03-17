using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;
using UnityEngine.Rendering.Universal;

namespace Fotocentr.Decals.Services
{
    public static class UrpDecalAutoCheck
    {
        private static bool _hasRun;

        public static void ValidateOnce()
        {
            if (_hasRun)
                return;

            _hasRun = true;

            try
            {
                ValidateInternal();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"URP decal auto-check failed: {e.GetType().Name}: {e.Message}");
            }
        }

        private static void ValidateInternal()
        {
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null)
                return;

            var urpAssetType = Type.GetType(
                "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            if (urpAssetType == null || !urpAssetType.IsInstanceOfType(rp))
                return;

            var decalShader =
                Shader.Find("Universal Render Pipeline/Decal") ??
                Shader.Find("Shader Graphs/Decal");

            if (decalShader == null)
            {
                Debug.LogError(
                    "URP decals: decal shader not found. Expected one of: " +
                    "\"Universal Render Pipeline/Decal\" or \"Shader Graphs/Decal\". " +
                    "If you use a custom URP Decal Shader Graph, assign it on the decal prefab/material.");
            }

            var rendererData = GetUrpScriptableRendererData(rp);
            if (rendererData == null)
            {
                Debug.LogWarning(
                    "URP decals: couldn't resolve ScriptableRendererData from current URP asset. " +
                    "Auto-check skipped (legacy URP DecalProjector is no longer used).");
                return;
            }

            if (!HasDecalRendererFeature(rendererData))
            {
                Debug.LogError(
                    "URP decals are not enabled in the active URP Renderer.\n" +
                    "Fix: open your URP Renderer asset (Forward Renderer / Renderer Data) and add \"Decal Renderer Feature\".\n" +
                    "Then ensure 'Decals' are enabled in the renderer settings if present.");
            }
        }

        private static ScriptableRendererData GetUrpScriptableRendererData(RenderPipelineAsset rp)
        {
            if (rp == null)
                return null;

            var t = rp.GetType();

            // 1) Public property in some URP versions
            var prop = t.GetProperty("scriptableRendererData", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null)
                return prop.GetValue(rp) as ScriptableRendererData;

            // 2) Renderer data list (serialized field) + default renderer index
            var listField =
                t.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic) ??
                t.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.Public);

            var list = listField?.GetValue(rp) as ScriptableRendererData[];
            if (list == null || list.Length == 0)
            {
                // Some URP versions expose rendererDataList as a property
                var listProp = t.GetProperty("rendererDataList", BindingFlags.Instance | BindingFlags.Public);
                list = listProp?.GetValue(rp) as ScriptableRendererData[];
            }

            if (list == null || list.Length == 0)
                return null;

            int idx = 0;
            var idxProp = t.GetProperty("defaultRendererIndex", BindingFlags.Instance | BindingFlags.Public);
            if (idxProp != null && idxProp.PropertyType == typeof(int))
                idx = (int)idxProp.GetValue(rp);
            else
            {
                var idxField = t.GetField("m_DefaultRendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);
                if (idxField != null && idxField.FieldType == typeof(int))
                    idx = (int)idxField.GetValue(rp);
            }

            if (idx < 0 || idx >= list.Length)
                idx = 0;

            if (list[idx] != null)
                return list[idx];

            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null)
                    return list[i];
            }

            return null;
        }

        private static bool HasDecalRendererFeature(ScriptableRendererData rendererData)
        {
            if (rendererData == null)
                return false;

            var features = rendererData.rendererFeatures;
            if (features == null)
                return false;

            for (int i = 0; i < features.Count; i++)
            {
                var f = features[i];
                if (f == null)
                    continue;

                var name = f.GetType().FullName ?? f.GetType().Name;
                if (name.Contains("DecalRendererFeature", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}

