using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PreviewSystem.Interfaces;
using PreviewSystem.Strategies;
using PreviewSystem.Visual;
using PreviewSystem.Factories;

/// <summary>
/// ���������� ���� ������������� �������
/// </summary>
public class PreviewWindowController : MonoBehaviour, IPreviewWindow, IVisualParametersProvider
{
    [Header("UI Components")]
    [SerializeField] private RectTransform _previewWindow;
    [SerializeField] private CanvasGroup _handlesCanvasGroup;
    [SerializeField] private TextMeshProUGUI _noDecalText;
    [SerializeField] private ModelManager _modelManager;

    [Header("Layer Settings")]
    [SerializeField] private GameObject _uiDecalLayerPrefab;
    [SerializeField] private Transform _layersContainer;

    [Header("Visual Settings")]
    [SerializeField] private LayerVisualParameters _visualParameters;
    [SerializeField, Min(0.01f)] private float _defaultCanvasAspect = 1f;

    public event Action<DecalController> OnDecalLayerClicked;

    private IDecalEditor _editor;
    private Camera _mainCamera;
    private RectTransform _canvasRect;
    private Vector2 _lastWindowSize;

    private IDecalLayerFactory _layerFactory;
    private IDecalLayerVisualStrategy _visualStrategy;
    private ILayerOrderStrategy _orderStrategy;
    private ISelectionStrategy _selectionStrategy;
    private RectTransform _workingAreaRect;

    private Dictionary<DecalController, IDecalLayer> _decalLayers =
        new Dictionary<DecalController, IDecalLayer>();

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeStrategies();
        ValidateReferences();
        ResolveModelManager();
    }

    private void Update()
    {
        CheckWindowResize();
    }

    private void Start()
    {
        SubscribeToModelChanges();
        ApplyAspectForCurrentModel();
    }

    private void OnDestroy()
    {
        UnsubscribeFromModelChanges();
    }

    #endregion

    #region Initialization

    private void InitializeStrategies()
    {
        _visualStrategy = new ShaderBasedVisualStrategy(_visualParameters);
        _orderStrategy = new TimeBasedLayerOrderStrategy();
        _selectionStrategy = new HighlightSelectionStrategy(_visualStrategy);
    }

    private void ValidateReferences()
    {
        _workingAreaRect = EnsureDefaultLayersContainer() as RectTransform;

        if (_uiDecalLayerPrefab == null)
            Debug.LogError("UIDecalLayer prefab is not assigned!");
    }

    private Transform EnsureDefaultLayersContainer()
    {
        if (_previewWindow == null)
            return null;

        var existing = _previewWindow.Find("PreviewContent");
        if (existing != null)
            return existing;

        var containerGo = new GameObject("PreviewContent", typeof(RectTransform));
        var rect = containerGo.GetComponent<RectTransform>();
        rect.SetParent(_previewWindow, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = _previewWindow.rect.size;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
        return rect;
    }

    private void ResolveModelManager()
    {
        if (_modelManager == null)
            _modelManager = FindObjectOfType<ModelManager>();
    }

    private void SubscribeToModelChanges()
    {
        if (_modelManager != null)
            _modelManager.OnModelChanged += OnModelChanged;
    }

    private void UnsubscribeFromModelChanges()
    {
        if (_modelManager != null)
            _modelManager.OnModelChanged -= OnModelChanged;
    }

    public void Initialize(IDecalEditor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));

        InitializeCamera();
        InitializeCanvas();
        InitializeFactory();
        SetupMask();
        SetInitialState();

        _lastWindowSize = _previewWindow.rect.size;
        ApplyAspectForCurrentModel();
    }

    private void InitializeCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
            Debug.LogError("Main camera not found!");
    }

    private void InitializeCanvas()
    {
        var canvas = GetComponentInParent<Canvas>();
        _canvasRect = canvas?.GetComponent<RectTransform>();
    }

    private void InitializeFactory()
    {
        _layerFactory = new DecalLayerFactory(
            _uiDecalLayerPrefab,
            _workingAreaRect != null ? _workingAreaRect : GetWorkingAreaRect(),
            _visualStrategy,
            _visualParameters
        );
    }

    private void SetupMask()
    {
        if (_previewWindow == null) return;

        // ��������� Mask ���� �����
        if (_previewWindow.GetComponent<Mask>() == null)
        {
            var mask = _previewWindow.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }

        // ��������� Image ��� ����� ���� �����
        if (_previewWindow.GetComponent<Image>() == null)
        {
            var maskImage = _previewWindow.gameObject.AddComponent<Image>();
            maskImage.color = new Color(1, 1, 1, 0.1f);
            maskImage.raycastTarget = false;
        }

        SetupWorkingAreaMask();
    }

    private void SetupWorkingAreaMask()
    {
        var workingArea = GetWorkingAreaRect();
        if (workingArea == null)
            return;

        if (workingArea.GetComponent<RectMask2D>() == null)
            workingArea.gameObject.AddComponent<RectMask2D>();
    }

    private void SetInitialState()
    {
        ShowNoDecalText(true);
        SetCanvasGroupActive(_handlesCanvasGroup, false);
        ClearLayers();
    }

    private void SetCanvasGroupActive(CanvasGroup group, bool active)
    {
        if (group == null) return;

        group.alpha = active ? 1f : 0f;
        group.blocksRaycasts = active;
        group.interactable = active;
    }

    #endregion

    #region Window Management

    private void CheckWindowResize()
    {
        if (_previewWindow == null)
            return;

        if (_previewWindow.rect.size != _lastWindowSize)
        {
            _lastWindowSize = _previewWindow.rect.size;
            ApplyAspectForCurrentModel();
            OnWindowResized();
        }
    }

    private void OnWindowResized()
    {
        foreach (var layer in _decalLayers.Values)
        {
            layer?.UpdateWindowSize();
        }
    }

    private void OnModelChanged(GameObject model, int index)
    {
        ApplyAspectForModel(model);
    }

    private void ApplyAspectForCurrentModel()
    {
        ApplyAspectForModel(_modelManager != null ? _modelManager.CurrentModel : null);
    }

    private void ApplyAspectForModel(GameObject model)
    {
        var workingArea = GetWorkingAreaRect();
        if (_previewWindow == null || workingArea == null)
            return;

        float aspect = GetAspectForModel(model);
        if (aspect <= 0f)
            aspect = _defaultCanvasAspect;

        FitWorkingAreaToAspect(workingArea, aspect);
        OnWindowResized();
    }

    private float GetAspectForModel(GameObject model)
    {
        if (model == null)
            return _defaultCanvasAspect;

        var projectionZone = model.GetComponentInChildren<ModelProjectionZone>(true);
        if (projectionZone == null || projectionZone.CanvasAspect <= 0f)
            return _defaultCanvasAspect;

        Rect usableRect = GetUsableCanvasRectForModel(model);
        float usableWidth = Mathf.Max(usableRect.width, 0.0001f);
        float usableHeight = Mathf.Max(usableRect.height, 0.0001f);
        return projectionZone.CanvasAspect * (usableWidth / usableHeight);
    }

    private Rect GetUsableCanvasRectForModel(GameObject model)
    {
        if (model == null)
            return new Rect(0f, 0f, 1f, 1f);

        var projector = model.GetComponentInChildren<OverallDecalProjector>(true);
        if (projector == null)
            return new Rect(0f, 0f, 1f, 1f);

        return projector.GetUsableCanvasRectNormalized();
    }

    private void FitWorkingAreaToAspect(RectTransform workingArea, float aspect)
    {
        float availableWidth = Mathf.Max(_previewWindow.rect.width, 1f);
        float availableHeight = Mathf.Max(_previewWindow.rect.height, 1f);
        float availableAspect = availableWidth / availableHeight;

        float targetWidth;
        float targetHeight;

        if (aspect >= availableAspect)
        {
            targetWidth = availableWidth;
            targetHeight = targetWidth / aspect;
        }
        else
        {
            targetHeight = availableHeight;
            targetWidth = targetHeight * aspect;
        }

        workingArea.anchorMin = new Vector2(0.5f, 0.5f);
        workingArea.anchorMax = new Vector2(0.5f, 0.5f);
        workingArea.pivot = new Vector2(0.5f, 0.5f);
        workingArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        workingArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        workingArea.anchoredPosition = Vector2.zero;
        workingArea.localScale = Vector3.one;

        if (_layersContainer is RectTransform assignedContainer && assignedContainer != workingArea)
        {
            assignedContainer.anchorMin = workingArea.anchorMin;
            assignedContainer.anchorMax = workingArea.anchorMax;
            assignedContainer.pivot = workingArea.pivot;
            assignedContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
            assignedContainer.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            assignedContainer.anchoredPosition = Vector2.zero;
            assignedContainer.localScale = Vector3.one;
        }
    }

    #endregion

    #region Layer Management

    public void AddLayer(DecalController decal)
    {
        if (decal == null || _decalLayers.ContainsKey(decal)) return;

        var layer = _layerFactory.Create(decal, GetWorkingAreaRect());
        if (layer == null) return;

        layer.OnLayerClicked += OnLayerClicked;
        layer.SetOnMoved(() => _editor?.OnTransformChanged());
        _decalLayers[decal] = layer;

        ApplyLayerOrder();
        ShowNoDecalText(false);
    }

    public void RemoveLayer(DecalController decal)
    {
        if (_decalLayers.TryGetValue(decal, out var layer))
        {
            layer.OnLayerClicked -= OnLayerClicked;
            _decalLayers.Remove(decal);
            _layerFactory.Destroy(layer);
        }

        if (_decalLayers.Count == 0)
        {
            ShowNoDecalText(true);
            SetCanvasGroupActive(_handlesCanvasGroup, false);
        }
    }

    private void OnLayerClicked(DecalController decal)
    {
        OnDecalLayerClicked?.Invoke(decal);
    }

    public void UpdateLayerPosition(DecalController decal)
    {
        if (_decalLayers.TryGetValue(decal, out var layer))
        {
            layer.UpdateTransform(decal.transform.position, decal.transform.eulerAngles.z);
        }
    }

    public void UpdateLayerMirror(DecalController decal)
    {
        if (decal != null && _decalLayers.TryGetValue(decal, out var layer))
            layer.SetMirrored(decal.IsMirroredX());
    }

    public void UpdateAllLayers()
    {
        foreach (var kvp in _decalLayers)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.UpdateTransform(kvp.Key.transform.position, kvp.Key.transform.eulerAngles.z);
            }
        }
    }

    public void HighlightSelected(DecalController selected)
    {
        _selectionStrategy.ApplySelection(_decalLayers.Values, selected);

        // ���������� handles ������ ���� ���� ��������� ������
        SetCanvasGroupActive(_handlesCanvasGroup, selected != null);
    }

    private void ApplyLayerOrder()
    {
        _orderStrategy.ApplyOrder(_decalLayers.Values);
    }

    public void Clear()
    {
        ClearLayers();
        SetInitialState();
    }

    public void ClearLayers()
    {
        foreach (var layer in _decalLayers.Values)
        {
            _layerFactory.Destroy(layer);
        }
        _decalLayers.Clear();
    }

    #endregion

    #region UI State

    private void ShowNoDecalText(bool show)
    {
        if (_noDecalText != null)
            _noDecalText.gameObject.SetActive(show);
    }

    #endregion

    #region Public Accessors

    public RectTransform GetRectTransform() => GetWorkingAreaRect();
    public RectTransform GetCanvasRect() => _canvasRect;

    public Canvas GetCanvas() => _canvasRect != null ? _canvasRect.GetComponentInParent<Canvas>() : null;

    public RectTransform GetLayerRect(DecalController decal)
    {
        if (decal == null || !_decalLayers.TryGetValue(decal, out var layer))
            return GetWorkingAreaRect();
        return layer.RectTransform;
    }
    public ILayerVisualParameters GetParameters() => _visualParameters;

    private RectTransform GetWorkingAreaRect()
    {
        return _workingAreaRect != null ? _workingAreaRect : _previewWindow;
    }

    #endregion
}
