using UnityEngine;

namespace DecalSystem.Extensions
{
    /// <summary>
    /// Extension methods для CanvasGroup
    /// </summary>
    public static class CanvasGroupExtensions
    {
        public static void SetActive(this CanvasGroup canvasGroup, bool active)
        {
            if (canvasGroup == null) return;

            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.blocksRaycasts = active;
            canvasGroup.interactable = active;
        }
    }
}