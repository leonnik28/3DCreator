using UnityEngine;

/// <summary>
/// Настраиваемая зона проекции на модели. Позволяет задать область, пропорциональную 2D полотну,
/// на которую проецируется декаль. Можно добавить на модель и настроить размер/позицию.
/// Для полной поддержки потребуется модификация Projector или использование mesh-based decal.
/// </summary>
public class ModelProjectionZone : MonoBehaviour
{
    [Tooltip("Размер зоны в мировых единицах (высота)")]
    [SerializeField] private float _zoneHeight = 1f;

    [Tooltip("Смещение центра зоны от центра модели")]
    [SerializeField] private Vector3 _offset;

    [Tooltip("Пропорция 2D полотна (ширина/высота)")]
    [SerializeField] private float _canvasAspect = 1f;

    public float ZoneHeight => _zoneHeight;
    public Vector3 Offset => _offset;
    public float CanvasAspect => _canvasAspect;
}
