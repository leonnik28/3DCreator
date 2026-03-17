using UnityEngine;
using System;
using System.Reflection;

/// <summary>
/// Контроллер 3D-декали.
/// Предпочтительно использует DecalProjector (URP/HDRP), но имеет fallback на legacy Projector.
/// </summary>
public class DecalController : MonoBehaviour
{
    public event System.Action<DecalController> OnDeleted;

    private Texture2D _texture;
    private Material _baseMaterial;
    private float _aspectRatio = 1f;
    private float _worldHalfHeight;

    /// <summary>����� ��������, ������������ ��� ���������� ���� � UI.</summary>
    public float CreationTime { get; set; }

    private void Awake()
    {
    }

    /// <summary>
    /// ��������� ������������� ������ ��������� � ���������� �� �����������.
    /// </summary>
    public void Initialize(Texture2D texture, Vector3 hitPoint, Vector3 hitNormal, float size, LayerMask targetLayers)
    {
        if (texture == null)
            throw new ArgumentNullException(nameof(texture));

        _texture = texture;
        _aspectRatio = (float)texture.width / texture.height;
        _worldHalfHeight = size;
        _baseMaterial = null;

        PlaceOnSurface(hitPoint, hitNormal);
    }

    /// <summary>
    /// ����������� ������ �� ��������� ����� �����������.
    /// </summary>
    public void PlaceOnSurface(Vector3 hitPoint, Vector3 hitNormal)
    {
        transform.position = hitPoint + hitNormal * 0.01f;
        var baseRotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
        transform.rotation = baseRotation;
    }

    /// <summary>
    /// ���������� ������ �������� (���������� � ������� ��������).
    /// </summary>
    public void SetSize(float worldHalfHeight)
    {
        _worldHalfHeight = worldHalfHeight;
    }

    /// <summary>
    /// ���������� ����������� ������ (������/������) ��� ������������� ��������.
    /// </summary>
    public void SetAspectRatio(float aspect)
    {
        _aspectRatio = Mathf.Max(aspect, 0.01f);
    }

    /// <summary>
    /// ��������/��������� ����� ��������� (���������).
    /// </summary>
    public void SetSelected(bool selected)
    {
        // Оставлено пустым: выделение теперь обрабатывается только в UI-превью.
    }

    public Texture2D GetTexture() => _texture;
    public float GetAspectRatio() => _aspectRatio;
    public float GetSize() => _worldHalfHeight;

    public void SetUpHint(Vector3 up) { }
    public void SetRollDegrees(float rollDegrees) { }

    /// <summary>
    /// Устанавливает порядок отрисовки декали (для URP/HDRP DecalProjector, если поддерживается).
    /// Чем больше значение, тем "выше" слой.
    /// </summary>
    public void SetDrawOrder(int order)
    {
        // В новом подходе порядок рисования 3D-декали не используется.
    }

    /// <summary>
    /// Маска полотна в нормализованных координатах превью (offset = центр, scale = ширина/высота).
    /// Используется шейдером для обрезки только видимой части слоя.
    /// </summary>
    public void SetCanvasMask(Vector2 offset, Vector2 scale)
    {
        // Маска полотна теперь применяется только в UI-превью.
    }

    private void ApplySizeAndAspect()
    {
        // Размер и аспект больше не управляют Projector/DecalProjector.
    }

    private void ApplyMaterial(Material mat)
    {
        // В новом подходе материал используется только на UI-слоях.
    }

    private void ApplyLayerMask()
    {
        // Маска слоёв для Projector больше не используется.
    }

    private void OnDestroy()
    {
        if (_baseMaterial != null)
            Destroy(_baseMaterial);

        OnDeleted?.Invoke(this);
    }
}

