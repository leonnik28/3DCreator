using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Единый обработчик перетаскивания слоя декали. Используется UIDecalLayer и DecalCenterDragZone.
/// </summary>
public static class DecalLayerDragHandler
{
    private const float DefaultSensitivity = 2f;

    public static void ExecuteDrag(
        PointerEventData eventData,
        RectTransform layerRect,
        RectTransform parentRect,
        Canvas canvas,
        float sensitivity,
        System.Action onMoved)
    {
        if (layerRect == null || parentRect == null) return;

        Camera cam = eventData.pressEventCamera ?? canvas?.worldCamera ?? Camera.main;

        bool currOk = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, cam, out var currLocal);
        bool prevOk = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position - eventData.delta, cam, out var prevLocal);

        float sens = sensitivity > 0.01f ? sensitivity : DefaultSensitivity;

        if (currOk && prevOk)
        {
            Vector2 deltaLocal = (currLocal - prevLocal) * sens;
            layerRect.anchoredPosition += deltaLocal;
        }
        else
        {
            float scale = canvas != null && canvas.scaleFactor > 0.1f ? canvas.scaleFactor : 1f;
            layerRect.anchoredPosition += eventData.delta / scale * sens;
        }

        onMoved?.Invoke();
    }
}
