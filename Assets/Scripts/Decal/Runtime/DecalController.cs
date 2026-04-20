using UnityEngine;
using System;

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

    public float CreationTime { get; set; }

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

    public void PlaceOnSurface(Vector3 hitPoint, Vector3 hitNormal)
    {
        transform.position = hitPoint + hitNormal * 0.01f;
        var baseRotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
        transform.rotation = baseRotation;
    }

    public void SetSize(float worldHalfHeight)
    {
        _worldHalfHeight = worldHalfHeight;
    }

    public void SetAspectRatio(float aspect)
    {
        _aspectRatio = Mathf.Max(aspect, 0.01f);
    }

    public Texture2D GetTexture() => _texture;
    public float GetAspectRatio() => _aspectRatio;

    private void OnDestroy()
    {
        if (_baseMaterial != null)
            Destroy(_baseMaterial);

        OnDeleted?.Invoke(this);
    }
}

