using UnityEngine;
using UnityEngine.EventSystems;
using DecalSystem.CornerResize;

/// <summary>
/// Угловая ручка для изменения размера изображения декали.
/// </summary>
public class DecalCornerHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    public enum CornerType { TopLeft, TopRight, BottomLeft, BottomRight }

    [SerializeField] private CornerType _cornerType;
    [Tooltip("Минимальный размер слоя в UI-единицах. 0 или меньше — авто (по размеру ручек).")]
    [SerializeField] private float _minSize = 0f;
    [Tooltip("Максимальный размер слоя в UI-единицах. 0 или меньше — без лимита.")]
    [SerializeField] private float _maxSize = 0f;
    [SerializeField, Range(0.5f, 3f)] private float _resizeSensitivity = 1f; // Уменьшил чувствительность

    private IDecalEditor _editor;
    private RectTransform _targetRect;
    private Vector2 _dragStartPoint;
    private Vector2 _dragStartSize;
    private Vector2 _dragStartPosition;
    private float _dragStartRotation;
    private ICornerResizeStrategy _strategy;

    public void Initialize(IDecalEditor editor)
    {
        _editor = editor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _targetRect = _editor?.GetPreviewRect();
        if (_targetRect == null) return;

        _dragStartPoint = eventData.position;
        _dragStartSize = _targetRect.sizeDelta;
        _dragStartPosition = _targetRect.anchoredPosition;
        _dragStartRotation = _targetRect.eulerAngles.z;

        // Определяем эффективный угол с учетом текущего поворота
        CornerType effectiveCorner = GetEffectiveCornerFromRotation();
        _strategy = CornerResizeStrategyFactory.GetStrategy(ToResizeCornerType(effectiveCorner));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_targetRect == null || _strategy == null) return;

        // Конвертируем экранные координаты в локальные координаты относительно родителя
        RectTransform parentRect = _targetRect.parent as RectTransform;
        Camera cam = eventData.pressEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, cam, out Vector2 currentLocalPos)) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, _dragStartPoint, cam, out Vector2 startLocalPos)) return;

        // Вычисляем дельту в локальном пространстве родителя
        Vector2 localDelta = currentLocalPos - startLocalPos;

        // Поворачиваем дельту обратно на угол поворота объекта
        // Это ключевой момент - мы "отменяем" поворот, чтобы дельта была в локальном пространстве объекта
        float rotationRad = -_dragStartRotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rotationRad);
        float sin = Mathf.Sin(rotationRad);

        Vector2 rotatedDelta = new Vector2(
            localDelta.x * cos - localDelta.y * sin,
            localDelta.x * sin + localDelta.y * cos
        );

        // Получаем изменение размера от стратегии, но с учетом знаков для каждого угла
        Vector2 sizeDelta = GetSizeDeltaForCorner(rotatedDelta, _cornerType);

        // Применяем чувствительность
        sizeDelta *= _resizeSensitivity;

        // Вычисляем новый размер
        Vector2 newSize = _dragStartSize + sizeDelta;
        newSize = ClampAndApplyAspectRatio(newSize);

        // Вычисляем фактическое изменение размера
        Vector2 actualSizeDelta = newSize - _dragStartSize;

        // Вычисляем смещение позиции для сохранения противоположного угла на месте
        Vector2 positionDelta = GetPositionDeltaForCorner(actualSizeDelta, _cornerType, _dragStartRotation);

        // Применяем изменения
        _targetRect.sizeDelta = newSize;
        _targetRect.anchoredPosition = _dragStartPosition + positionDelta;

        _editor?.OnTransformChanged();
    }

    private Vector2 GetSizeDeltaForCorner(Vector2 delta, CornerType corner)
    {
        // Для каждого угла определяем, как движение мыши влияет на размер
        switch (corner)
        {
            case CornerType.TopLeft:
                return new Vector2(-delta.x, delta.y);
            case CornerType.TopRight:
                return new Vector2(delta.x, delta.y);
            case CornerType.BottomLeft:
                return new Vector2(-delta.x, -delta.y);
            case CornerType.BottomRight:
                return new Vector2(delta.x, -delta.y);
            default:
                return delta;
        }
    }

    private Vector2 GetPositionDeltaForCorner(Vector2 sizeDelta, CornerType corner, float rotation)
    {
        // Вычисляем смещение позиции, чтобы противоположный угол оставался на месте
        Vector2 posDelta = Vector2.zero;

        switch (corner)
        {
            case CornerType.TopLeft:
                posDelta = new Vector2(-sizeDelta.x * 0.5f, sizeDelta.y * 0.5f);
                break;
            case CornerType.TopRight:
                posDelta = new Vector2(sizeDelta.x * 0.5f, sizeDelta.y * 0.5f);
                break;
            case CornerType.BottomLeft:
                posDelta = new Vector2(-sizeDelta.x * 0.5f, -sizeDelta.y * 0.5f);
                break;
            case CornerType.BottomRight:
                posDelta = new Vector2(sizeDelta.x * 0.5f, -sizeDelta.y * 0.5f);
                break;
        }

        // Поворачиваем смещение позиции обратно с учетом поворота объекта
        float rotationRad = rotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rotationRad);
        float sin = Mathf.Sin(rotationRad);

        return new Vector2(
            posDelta.x * cos - posDelta.y * sin,
            posDelta.x * sin + posDelta.y * cos
        );
    }

    private CornerType GetEffectiveCornerFromRotation()
    {
        if (_targetRect == null) return _cornerType;

        float z = _targetRect.eulerAngles.z;
        int rotations = Mathf.RoundToInt(z / 90f) % 4;

        // Маппинг углов при повороте на 90, 180, 270 градусов
        return (_cornerType, rotations) switch
        {
            (CornerType.TopLeft, 1) => CornerType.TopRight,
            (CornerType.TopLeft, 2) => CornerType.BottomRight,
            (CornerType.TopLeft, 3) => CornerType.BottomLeft,

            (CornerType.TopRight, 1) => CornerType.BottomRight,
            (CornerType.TopRight, 2) => CornerType.BottomLeft,
            (CornerType.TopRight, 3) => CornerType.TopLeft,

            (CornerType.BottomRight, 1) => CornerType.BottomLeft,
            (CornerType.BottomRight, 2) => CornerType.TopLeft,
            (CornerType.BottomRight, 3) => CornerType.TopRight,

            (CornerType.BottomLeft, 1) => CornerType.TopLeft,
            (CornerType.BottomLeft, 2) => CornerType.TopRight,
            (CornerType.BottomLeft, 3) => CornerType.BottomRight,

            _ => _cornerType
        };
    }

    private Vector2 ClampAndApplyAspectRatio(Vector2 size)
    {
        float min = GetEffectiveMinSize();
        float max = GetEffectiveMaxSize();

        size.x = ClampOptional(size.x, min, max);
        size.y = ClampOptional(size.y, min, max);

        if (_editor != null && _editor.GetLockAspectRatio())
        {
            float aspect = _dragStartSize.x / _dragStartSize.y;
            float newSizeX = size.x;
            float newSizeY = newSizeX / aspect;

            if (!IsWithinOptional(newSizeY, min, max))
            {
                newSizeY = ClampOptional(newSizeY, min, max);
                newSizeX = newSizeY * aspect;
            }

            size = new Vector2(newSizeX, newSizeY);
        }

        return size;
    }

    private float GetEffectiveMinSize()
    {
        // Ручки ~24px (см. TransformControlsController.HandleSize). Чтобы ручки оставались видимыми,
        // ставим минимальный размер немного больше одной ручки по ширине/высоте.
        const float autoMin = 32f;
        return _minSize > 0f ? _minSize : autoMin;
    }

    private float GetEffectiveMaxSize()
    {
        // 0 (или меньше) = без верхнего лимита.
        return _maxSize > 0f ? _maxSize : float.PositiveInfinity;
    }

    private static float ClampOptional(float value, float min, float max)
    {
        if (!float.IsNaN(min) && !float.IsInfinity(min))
            value = Mathf.Max(value, min);
        if (!float.IsNaN(max) && !float.IsInfinity(max))
            value = Mathf.Min(value, max);
        return value;
    }

    private static bool IsWithinOptional(float value, float min, float max)
    {
        if (!float.IsNaN(min) && !float.IsInfinity(min) && value < min) return false;
        if (!float.IsNaN(max) && !float.IsInfinity(max) && value > max) return false;
        return true;
    }

    private static DecalSystem.CornerResize.CornerType ToResizeCornerType(CornerType t)
    {
        return t switch
        {
            CornerType.TopLeft => DecalSystem.CornerResize.CornerType.TopLeft,
            CornerType.TopRight => DecalSystem.CornerResize.CornerType.TopRight,
            CornerType.BottomLeft => DecalSystem.CornerResize.CornerType.BottomLeft,
            CornerType.BottomRight => DecalSystem.CornerResize.CornerType.BottomRight,
            _ => DecalSystem.CornerResize.CornerType.TopLeft
        };
    }
}