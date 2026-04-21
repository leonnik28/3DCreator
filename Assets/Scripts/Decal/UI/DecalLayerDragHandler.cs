using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Единый обработчик перетаскивания слоя декали. Используется UIDecalLayer и DecalCenterDragZone.
/// </summary>
public static class DecalLayerDragHandler
{
    private const float DefaultSensitivity = 2f;
    private static readonly Dictionary<int, Vector2> _lastLocalPointByRectId = new Dictionary<int, Vector2>();

    public static void BeginDrag(PointerEventData eventData, RectTransform layerRect, RectTransform parentRect, Canvas canvas)
    {
        if (layerRect == null || parentRect == null || eventData == null) return;

        if (TryGetLocalPointInParent(eventData, parentRect, canvas, out var local))
            _lastLocalPointByRectId[layerRect.GetInstanceID()] = local;
        else
            _lastLocalPointByRectId.Remove(layerRect.GetInstanceID());
    }

    public static void ExecuteDrag(
        PointerEventData eventData,
        RectTransform layerRect,
        RectTransform parentRect,
        Canvas canvas,
        float sensitivity,
        System.Action onMoved)
    {
        float sens = sensitivity > 0.01f ? sensitivity : DefaultSensitivity;

        if (layerRect == null || parentRect == null || eventData == null) return;

        int id = layerRect.GetInstanceID();

        if (TryGetLocalPointInParent(eventData, parentRect, canvas, out var currLocal))
        {
            if (_lastLocalPointByRectId.TryGetValue(id, out var prevLocal))
            {
                Vector2 deltaLocal = (currLocal - prevLocal) * sens;
                layerRect.anchoredPosition += deltaLocal;
            }

            _lastLocalPointByRectId[id] = currLocal;
        }
        else
        {
            float scale = canvas != null && canvas.scaleFactor > 0.1f ? canvas.scaleFactor : 1f;
            layerRect.anchoredPosition += (eventData.delta / scale) * sens;
        }

        onMoved?.Invoke();
    }

    private static bool TryGetLocalPointInParent(
        PointerEventData eventData,
        RectTransform parentRect,
        Canvas canvas,
        out Vector2 local)
    {
        local = Vector2.zero;
        if (eventData == null || parentRect == null) return false;

        Camera cam = eventData.pressEventCamera;
        if (cam == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera ?? Camera.main;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, cam, out local);
    }
}
