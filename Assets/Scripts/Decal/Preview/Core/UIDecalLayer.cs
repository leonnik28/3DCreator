using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PreviewSystem.Interfaces;
using Fotocentr.Core;

/// <summary>
/// UI ���� ������ � ���������� ������ � ���������� ��������
/// </summary>
public class UIDecalLayer : MonoBehaviour, IDecalLayer, IDragTarget, IPointerDownHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, ICanvasRaycastFilter
{
    [SerializeField] private RectTransform _layerRect;
    [SerializeField] private RawImage _layerImage;
    [SerializeField, Range(0.5f, 10f)] private float _dragSensitivity = 10f;
    [SerializeField, Range(0f, 1f)] private float _alphaRaycastThreshold = 0.05f;

    public DecalController SourceDecal { get; private set; }
    public RectTransform RectTransform => _layerRect;
    public event System.Action<DecalController> OnLayerClicked;
    private System.Action _onMoved;

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
        SetMirrored(decal.IsMirroredX());
    }

    public void SetOnMoved(System.Action callback) => _onMoved = callback;

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

    private const float InitialScale = 1f;

    private void FitToWindow()
    {
        if (_parentRect == null || _aspectRatio <= 0) return;

        float windowWidth = _parentRect.rect.width;
        float windowHeight = _parentRect.rect.height;

        if (windowWidth <= 0 || windowHeight <= 0) return;

        float windowAspect = windowWidth / windowHeight;
        float targetWidth = _aspectRatio > windowAspect
            ? windowWidth
            : windowHeight * _aspectRatio;
        float targetHeight = _aspectRatio > windowAspect
            ? windowWidth / _aspectRatio
            : windowHeight;

        Vector2 newSize = new Vector2(targetWidth * InitialScale, targetHeight * InitialScale);
        _layerRect.sizeDelta = newSize;
        _layerRect.anchoredPosition = Vector2.zero;
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

        // �������������� ����������
        _layerImage.enabled = false;
        _layerImage.enabled = true;
    }

    public void SetMirrored(bool mirrored)
    {
        if (_layerImage == null)
            return;

        _layerImage.uvRect = mirrored
            ? new Rect(1f, 0f, -1f, 1f)
            : new Rect(0f, 0f, 1f, 1f);
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (_layerRect == null || _layerImage == null)
            return false;

        var texture = _layerImage.texture as Texture2D;
        if (texture == null)
            return true;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _layerRect, screenPoint, eventCamera, out var localPoint))
        {
            return false;
        }

        Rect rect = _layerRect.rect;
        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        Rect uvRect = _layerImage.uvRect;
        float sampleX = uvRect.x + normalizedX * uvRect.width;
        float sampleY = uvRect.y + normalizedY * uvRect.height;

        try
        {
            return texture.GetPixelBilinear(sampleX, sampleY).a >= _alphaRaycastThreshold;
        }
        catch (UnityException)
        {
            // For non-readable runtime textures we keep the old behavior instead of breaking input.
            return true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_wasDragging)
            OnLayerClicked?.Invoke(SourceDecal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            OnLayerClicked?.Invoke(SourceDecal);
    }

    private bool _wasDragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _wasDragging = false;
        if (_layerRect == null || _parentRect == null) return;
        OnLayerClicked?.Invoke(SourceDecal);

        var canvas = GetComponentInParent<Canvas>();
        DecalLayerDragHandler.BeginDrag(eventData, _layerRect, _parentRect, canvas);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_layerRect == null || _parentRect == null) return;
        _wasDragging = true;

        // ������ delta �� ���� � �������� ������ ��������� ���������� �� ����� �����
        var canvas = GetComponentInParent<Canvas>();
        DecalLayerDragHandler.ExecuteDrag(
            eventData, _layerRect, _parentRect, canvas, _dragSensitivity, _onMoved);
    }

    public void HandlePointerDrag(PointerEventData eventData)
    {
        if (_layerRect == null || _parentRect == null) return;
        var canvas = GetComponentInParent<Canvas>();
        DecalLayerDragHandler.ExecuteDrag(
            eventData, _layerRect, _parentRect, canvas, _dragSensitivity, _onMoved);
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
