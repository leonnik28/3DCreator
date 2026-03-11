using UnityEngine;
using UnityEngine.EventSystems;

public class DecalCornerHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public enum CornerType { TopLeft, TopRight, BottomLeft, BottomRight }

    [SerializeField] private CornerType _cornerType;
    [SerializeField] private float _minSize = 50f;
    [SerializeField] private float _maxSize = 500f;

    private IDecalEditor _editor;
    private RectTransform _targetRect;
    private Vector2 _dragStartPoint;
    private Vector2 _dragStartSize;
    private Vector2 _dragStartPosition;

    public void Initialize(IDecalEditor editor)
    {
        _editor = editor;
        _targetRect = editor.GetPreviewRect();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStartPoint = eventData.position;
        _dragStartSize = _targetRect.sizeDelta;
        _dragStartPosition = _targetRect.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.position - _dragStartPoint;
        Vector2 newSize = CalculateNewSize(delta);

        ApplyNewSize(newSize);
        _editor.OnTransformChanged();
    }

    private Vector2 CalculateNewSize(Vector2 delta)
    {
        float signX = (_cornerType == CornerType.TopLeft || _cornerType == CornerType.BottomLeft) ? -1 : 1;
        float signY = (_cornerType == CornerType.TopLeft || _cornerType == CornerType.TopRight) ? 1 : -1;

        Vector2 newSize = new Vector2(
            _dragStartSize.x + delta.x * signX,
            _dragStartSize.y + delta.y * signY
        );

        newSize.x = Mathf.Clamp(newSize.x, _minSize, _maxSize);
        newSize.y = Mathf.Clamp(newSize.y, _minSize, _maxSize);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            float aspect = _dragStartSize.x / _dragStartSize.y;
            newSize.y = newSize.x / aspect;
        }

        return newSize;
    }

    private void ApplyNewSize(Vector2 newSize)
    {
        Vector2 sizeDelta = newSize - _dragStartSize;
        Vector2 positionDelta = new Vector2(
            (_cornerType == CornerType.TopLeft || _cornerType == CornerType.BottomLeft) ? -sizeDelta.x : sizeDelta.x,
            (_cornerType == CornerType.TopLeft || _cornerType == CornerType.TopRight) ? sizeDelta.y : -sizeDelta.y
        ) * 0.5f;

        _targetRect.sizeDelta = newSize;
        _targetRect.anchoredPosition = _dragStartPosition + positionDelta;
    }
}