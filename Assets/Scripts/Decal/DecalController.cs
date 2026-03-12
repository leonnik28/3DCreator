using UnityEngine;

/// <summary>
/// ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ 3D-˜˜˜˜˜˜˜, ˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜ Projector.
/// </summary>
[RequireComponent(typeof(Projector))]
public class DecalController : MonoBehaviour
{
    [Header("Components")]
    private Projector _projector;

    [Header("Decal Settings")]
    [SerializeField] private float _heightOffset = 0.1f;

    [Header("Outline Settings")]
    [SerializeField] private Color _outlineColor = Color.yellow;
    [SerializeField, Range(0f, 1f)] private float _outlineIntensity = 0.5f;

    public event System.Action<DecalController> OnDeleted;

    private Texture2D _texture;
    private Material _baseMaterial;
    private Material _outlineMaterial;
    private float _aspectRatio = 1f;
    private bool _isSelected;

    /// <summary>˜˜˜˜˜ ˜˜˜˜˜˜˜˜, ˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜ ˜ UI.</summary>
    public float CreationTime { get; set; }

    private void Awake()
    {
        _projector = GetComponent<Projector>();
        if (_projector == null)
            _projector = gameObject.AddComponent<Projector>();

        _projector.nearClipPlane = 0.01f;
        _projector.farClipPlane = 2f;
        _projector.orthographic = true;

        CreateOutlineMaterial();
    }

    private void CreateOutlineMaterial()
    {
        var shader = Shader.Find("Custom/HighlightShader");
        if (shader == null)
        {
            Debug.LogError("Custom/HighlightShader not found. Using Standard shader as fallback.");
            shader = Shader.Find("Standard");
        }

        _outlineMaterial = new Material(shader)
        {
            color = _outlineColor
        };

        if (_outlineMaterial.HasProperty("_HighlightIntensity"))
        {
            _outlineMaterial.SetFloat("_HighlightIntensity", _outlineIntensity);
        }
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜˜˜˜.
    /// </summary>
    public void Initialize(Texture2D texture, Vector3 hitPoint, Vector3 hitNormal, float size, LayerMask targetLayers)
    {
        if (texture == null)
            throw new System.ArgumentNullException(nameof(texture));

        _texture = texture;
        _aspectRatio = (float)texture.width / texture.height;

        var projectorShader = Shader.Find("Projector/DecalProjector");
        if (projectorShader == null)
        {
            Debug.LogError("Projector/DecalProjector shader not found. Using Standard shader as fallback.");
            projectorShader = Shader.Find("Standard");
        }

        _baseMaterial = new Material(projectorShader)
        {
            mainTexture = texture
        };

        _projector.material = _baseMaterial;
        _projector.aspectRatio = _aspectRatio;
        _projector.orthographicSize = size;

        // ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜ ˜˜˜˜˜˜
        _projector.ignoreLayers = ~targetLayers;

        PlaceOnSurface(hitPoint, hitNormal);
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜.
    /// </summary>
    public void PlaceOnSurface(Vector3 hitPoint, Vector3 hitNormal)
    {
        transform.position = hitPoint + hitNormal * _heightOffset;
        transform.rotation = Quaternion.LookRotation(-hitNormal, Vector3.up);
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ (˜˜˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜).
    /// </summary>
    public void SetSize(float worldHalfHeight)
    {
        _projector.orthographicSize = worldHalfHeight;
        _projector.aspectRatio = _aspectRatio;
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ (˜˜˜˜˜˜/˜˜˜˜˜˜) ˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜.
    /// </summary>
    public void SetAspectRatio(float aspect)
    {
        _aspectRatio = Mathf.Max(aspect, 0.01f);
        _projector.aspectRatio = _aspectRatio;
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜/˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ (˜˜˜˜˜˜˜˜˜).
    /// </summary>
    public void SetSelected(bool selected)
    {
        if (_isSelected == selected)
            return;

        _isSelected = selected;

        if (_isSelected)
        {
            _outlineMaterial.mainTexture = _texture;
            _projector.material = _outlineMaterial;
        }
        else
        {
            _projector.material = _baseMaterial;
        }
    }

    public Texture2D GetTexture() => _texture;
    public float GetAspectRatio() => _aspectRatio;
    public float GetSize() => _projector.orthographicSize;

    private void OnDestroy()
    {
        if (_baseMaterial != null)
            Destroy(_baseMaterial);
        if (_outlineMaterial != null)
            Destroy(_outlineMaterial);

        OnDeleted?.Invoke(this);
    }
}

