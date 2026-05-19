using TMPro;
using UnityEngine;

public static class TMPCyrillicFallbackRuntime
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Apply()
    {
        TMP_FontAsset cyrillicFont =
            Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF") ??
            Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");

        if (cyrillicFont == null)
        {
            Debug.LogWarning("[UI] Cyrillic TMP font not found in Resources. Cyrillic fallback won't be applied.");
            return;
        }

        var allTexts = Object.FindObjectsOfType<TMP_Text>(true);
        if (allTexts == null || allTexts.Length == 0)
            return;

        for (int i = 0; i < allTexts.Length; i++)
        {
            var t = allTexts[i];
            if (t == null || t.font == null)
                continue;

            if (t.font != null && t.font.name.Contains("LiberationSans SDF"))
                t.font = cyrillicFont;
        }
    }
}

