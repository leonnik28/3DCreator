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
    private RectTransform _layerRect;
    private RectTransform _parentRect;

    public void Initialize(RectTransform layerRect, RectTransform parentRect, System.Action onMoved)
    {
        _layerRect = layerRect;
        _parentRect = parentRect;
        _dragTarget = layerRect.GetComponent<IDragTarget>();

        var img = GetComponent<Image>();
        if (img != null)
        {
            img.color = new Color(1, 1, 1, 0.001f);
            // UIDecalLayer already handles click/drag input. This overlay should not block
            // clicks that need to pass through to other visible decal layers.
            img.raycastTarget = false;
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        var canvas = GetComponentInParent<Canvas>();
        DecalLayerDragHandler.BeginDrag(eventData, _layerRect, _parentRect, canvas);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _dragTarget?.HandlePointerDrag(eventData);
    }
}
