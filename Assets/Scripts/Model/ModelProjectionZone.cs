using UnityEngine;

/// <summary>
/// Настраиваемая зона проекции на модели. Оставлена для совместимости с DecalTransformService,
/// но больше не завязана на Unity Projector/DecalProjector.
/// </summary>
public class ModelProjectionZone : MonoBehaviour
{
    [Tooltip("Размер зоны в мировых единицах (высота)")]
    [SerializeField] private float _zoneHeight = 1f;

    [Tooltip("Смещение центра зоны от центра модели")]
    [SerializeField] private Vector3 _offset;

    [Tooltip("Пропорция 2D полотна (ширина/высота)")]
    [SerializeField] private float _canvasAspect = 1f;

    [Tooltip("Явный renderer поверхности печати. Если не задан, будет взят первый найденный renderer в иерархии.")]
    [SerializeField] private Renderer _surfaceRenderer;

    [Tooltip("Если включено, оси зоны берутся с transform этой зоны, а не с renderer. Полезно для импортированных плоских мешей с дочерним поворотом.")]
    [SerializeField] private bool _useZoneTransformAxes = false;

    public float ZoneHeight => _zoneHeight;
    public Vector3 Offset => _offset;
    public float CanvasAspect => _canvasAspect;
    public Renderer SurfaceRenderer => _surfaceRenderer;
    public bool UseZoneTransformAxes => _useZoneTransformAxes;
}
