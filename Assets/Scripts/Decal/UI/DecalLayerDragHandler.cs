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
                ClampLayerToParentBounds(layerRect, parentRect);
            }

            // Важно: всегда обновляем "предыдущую" точку в одной и той же системе координат.
            _lastLocalPointByRectId[id] = currLocal;
        }
        else
        {
            // Фолбэк: если по какой-то причине не удалось получить локальную точку,
            // двигаем по пиксельной delta, приводя к единицам UI через scaleFactor.
            float scale = canvas != null && canvas.scaleFactor > 0.1f ? canvas.scaleFactor : 1f;
            layerRect.anchoredPosition += (eventData.delta / scale) * sens;
            ClampLayerToParentBounds(layerRect, parentRect);
        }

        onMoved?.Invoke();
    }

    public static void EnsureLayerFitsInsideParent(RectTransform layerRect, RectTransform parentRect)
    {
        if (layerRect == null || parentRect == null)
            return;

        Vector2 fittedSize = GetSizeFittedToParent(layerRect.sizeDelta, layerRect.eulerAngles.z, parentRect.rect.size);
        layerRect.sizeDelta = fittedSize;
        ClampLayerToParentBounds(layerRect, parentRect);
    }

    public static void ClampLayerToParentBounds(RectTransform layerRect, RectTransform parentRect)
    {
        if (layerRect == null || parentRect == null)
            return;

        Vector2 halfBounds = GetRotatedBoundsHalfSize(layerRect.sizeDelta, layerRect.eulerAngles.z);
        Rect parent = parentRect.rect;

        float minX = parent.xMin + halfBounds.x;
        float maxX = parent.xMax - halfBounds.x;
        float minY = parent.yMin + halfBounds.y;
        float maxY = parent.yMax - halfBounds.y;

        Vector2 pos = layerRect.anchoredPosition;
        pos.x = minX <= maxX ? Mathf.Clamp(pos.x, minX, maxX) : parent.center.x;
        pos.y = minY <= maxY ? Mathf.Clamp(pos.y, minY, maxY) : parent.center.y;
        layerRect.anchoredPosition = pos;
    }

    private static Vector2 GetSizeFittedToParent(Vector2 size, float rotationDeg, Vector2 parentSize)
    {
        float width = Mathf.Max(size.x, 1f);
        float height = Mathf.Max(size.y, 1f);
        float parentWidth = Mathf.Max(parentSize.x, 1f);
        float parentHeight = Mathf.Max(parentSize.y, 1f);

        Vector2 halfBounds = GetRotatedBoundsHalfSize(new Vector2(width, height), rotationDeg);
        float scaleX = parentWidth / Mathf.Max(halfBounds.x * 2f, 1f);
        float scaleY = parentHeight / Mathf.Max(halfBounds.y * 2f, 1f);
        float scale = Mathf.Min(scaleX, scaleY, 1f);

        return new Vector2(width * scale, height * scale);
    }

    private static Vector2 GetRotatedBoundsHalfSize(Vector2 size, float rotationDeg)
    {
        float halfWidth = Mathf.Abs(size.x) * 0.5f;
        float halfHeight = Mathf.Abs(size.y) * 0.5f;
        float angle = rotationDeg * Mathf.Deg2Rad;
        float cos = Mathf.Abs(Mathf.Cos(angle));
        float sin = Mathf.Abs(Mathf.Sin(angle));

        return new Vector2(
            cos * halfWidth + sin * halfHeight,
            sin * halfWidth + cos * halfHeight
        );
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
