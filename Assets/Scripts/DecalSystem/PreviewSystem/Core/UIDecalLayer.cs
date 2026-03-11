using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PreviewSystem.Interfaces;

/// <summary>
/// UI слой декали с поддержкой кликов и визуальных эффектов
/// </summary>
public class UIDecalLayer : MonoBehaviour, IDecalLayer, IPointerClickHandler
{
    [SerializeField] private RectTransform _layerRect;
    [SerializeField] private RawImage _layerImage;

    public DecalController SourceDecal { get; private set; }
    public event System.Action<DecalController> OnLayerClicked;

    private RectTransform _parentRect;
    private float _aspectRatio;
    private Material _instanceMaterial;
    private ILayerVisualParameters _visualParameters;

    private static readonly int SelectedProperty = Shader.PropertyToID("_Selected");

    private void Awake()
    {
        ValidateComponents();
        CreateMaterial();
    }

    private void ValidateComponents()
    {
        if (_layerRect == null)
            _layerRect = GetComponent<RectTransform>();

        if (_layerImage == null)
            _layerImage = GetComponent<RawImage>();
    }

    private void CreateMaterial()
    {
        if (_layerImage == null) return;

        Shader shader = Shader.Find("UI/UIDecalLayer");
        if (shader == null)
        {
            Debug.LogError("UI/UIDecalLayer shader not found!");
            shader = Shader.Find("UI/Default");
        }

        _instanceMaterial = new Material(shader);
        _instanceMaterial.name = $"LayerMaterial_{GetInstanceID()}";
        _layerImage.material = _instanceMaterial;
    }

    public void Initialize(DecalController decal, RectTransform parentRect)
    {
        SourceDecal = decal ?? throw new System.ArgumentNullException(nameof(decal));
        _parentRect = parentRect;

        SetupTexture(decal.GetTexture());
        FitToWindow();
    }

    private void SetupTexture(Texture2D texture)
    {
        if (texture == null) return;

        _aspectRatio = (float)texture.width / texture.height;
        _layerImage.texture = texture;
        _layerImage.raycastTarget = true;
    }

    public void SetVisualParameters(ILayerVisualParameters parameters)
    {
        _visualParameters = parameters;
        UpdateMaterialParameters();
    }

    private void UpdateMaterialParameters()
    {
        if (_instanceMaterial == null || _visualParameters == null) return;

        _instanceMaterial.SetColor("_OutlineColor", _visualParameters.OutlineColor);
        _instanceMaterial.SetColor("_DimColor", _visualParameters.DimColor);
        _instanceMaterial.SetFloat("_OutlineWidth", _visualParameters.OutlineWidth);
    }

    private void FitToWindow()
    {
        if (_parentRect == null || _aspectRatio <= 0) return;

        float windowWidth = _parentRect.rect.width;
        float windowHeight = _parentRect.rect.height;

        if (windowWidth <= 0 || windowHeight <= 0) return;

        float windowAspect = windowWidth / windowHeight;

        Vector2 newSize = _aspectRatio > windowAspect
            ? new Vector2(windowWidth, windowWidth / _aspectRatio)
            : new Vector2(windowHeight * _aspectRatio, windowHeight);

        _layerRect.sizeDelta = newSize;
    }

    public void UpdateTransform(Vector2 worldPosition, float rotation)
    {
        if (_layerRect == null || _parentRect == null || Camera.main == null) return;

        Vector2 viewportPoint = Camera.main.WorldToViewportPoint(worldPosition);

        _layerRect.anchoredPosition = new Vector2(
            (viewportPoint.x - 0.5f) * _parentRect.rect.width,
            (viewportPoint.y - 0.5f) * _parentRect.rect.height
        );

        _layerRect.eulerAngles = new Vector3(0, 0, rotation);
    }

    public void UpdateWindowSize()
    {
        FitToWindow();
    }

    public void SetSelected(bool selected)
    {
        if (_instanceMaterial == null) return;

        float value = selected ? 1f : 0f;
        _instanceMaterial.SetFloat(SelectedProperty, value);

        // Принудительное обновление
        _layerImage.enabled = false;
        _layerImage.enabled = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnLayerClicked?.Invoke(SourceDecal);
    }

    private void OnDestroy()
    {
        if (_instanceMaterial != null)
        {
            Destroy(_instanceMaterial);
            _instanceMaterial = null;
        }

        OnLayerClicked = null;
    }
}