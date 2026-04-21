using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ручка поворота над изображением (как в редакторах). Поворот по углу от центра.
/// </summary>
[RequireComponent(typeof(Image))]
public class DecalRotateHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Texture2D _rotateCursor;
    [SerializeField] private Vector2 _rotateCursorHotspot = new Vector2(16, 16);
    [SerializeField] private float _sensitivity = 1f;

    private static Texture2D _defaultRotateCursor;

    private IDecalEditor _editor;
    private RectTransform _targetRect;
    private RectTransform _parentRect;
    private float _dragStartRotation;
    private float _dragStartAngle;
    private Canvas _canvas;

    public void Initialize(IDecalEditor editor)
    {
        _editor = editor;

        var img = GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
            img.color = new Color(1, 1, 1, 1f);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _targetRect = _editor?.GetPreviewRect() as RectTransform;
        if (_targetRect == null) return;

        _parentRect = _targetRect.parent as RectTransform;
        _canvas = _targetRect.GetComponentInParent<Canvas>();

        _dragStartRotation = _targetRect.eulerAngles.z;
        _dragStartAngle = GetAngleFromCenter(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetRect == null) return;

        float currentAngle = GetAngleFromCenter(eventData.position);
        float deltaAngle = Mathf.DeltaAngle(_dragStartAngle, currentAngle);
        float newRotation = _dragStartRotation + deltaAngle * _sensitivity;
        newRotation = (newRotation % 360f + 360f) % 360f;

        _targetRect.eulerAngles = new Vector3(0f, 0f, newRotation);
        _dragStartRotation = newRotation;
        _dragStartAngle = currentAngle;

        _editor?.OnTransformChanged();
    }

    private float GetAngleFromCenter(Vector2 screenPos)
    {
        if (_targetRect == null || _parentRect == null) return 0f;

        Camera cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentRect, screenPos, cam, out var localPoint))
        {
            Vector2 center = _targetRect.anchoredPosition;
            Vector2 dir = localPoint - center;
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }
        return 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Texture2D tex = _rotateCursor;
        if (tex == null)
            tex = GetDefaultRotateCursor();
        if (tex != null && IsValidCursorTexture(tex))
            Cursor.SetCursor(tex, _rotateCursorHotspot, CursorMode.Auto);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private static bool IsValidCursorTexture(Texture2D tex)
    {
        if (tex == null) return false;
        return tex.format == TextureFormat.RGBA32 && tex.mipmapCount <= 1;
    }

    private static Texture2D GetDefaultRotateCursor()
    {
        if (_defaultRotateCursor != null) return _defaultRotateCursor;

        int s = 32;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        var cols = new Color[s * s];
        Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            Vector2 p = new Vector2(x - c.x, y - c.y);
            float a = Mathf.Atan2(p.y, p.x);
            float r = p.magnitude;
            float alpha = 0f;
            if (r >= 5f && r <= 12f && a > -2.6f && a < 2.6f)
                alpha = 0.9f;
            cols[y * s + x] = new Color(0.2f, 0.2f, 0.2f, alpha);
        }
        tex.SetPixels(cols);
        tex.Apply();
        _defaultRotateCursor = tex;
        return tex;
    }
}
