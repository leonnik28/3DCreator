using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Fotocentr.Core;

/// <summary>
/// Прозрачная зона в центре изображения для перетаскивания. Делегирует в IDragTarget.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DecalCenterDragZone : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private IDragTarget _dragTarget;

    public void Initialize(RectTransform layerRect, RectTransform parentRect, System.Action onMoved)
    {
        _dragTarget = layerRect.GetComponent<IDragTarget>();

        var img = GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(1, 1, 1, 0.001f);
            img.raycastTarget = true;
        }

        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.15f, 0.15f);
            rt.anchorMax = new Vector2(0.85f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) { }

    public void OnDrag(PointerEventData eventData)
    {
        _dragTarget?.HandlePointerDrag(eventData);
    }
}
