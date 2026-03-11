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
/// Контроллер окна предпросмотра декалей
/// </summary>
public class PreviewWindowController : MonoBehaviour, IPreviewWindow, IVisualParametersProvider
{
    [Header("UI Components")]
    [SerializeField] private RectTransform _previewWindow;
    [SerializeField] private CanvasGroup _handlesCanvasGroup;
    [SerializeField] private TextMeshProUGUI _noDecalText;

    [Header("Layer Settings")]
    [SerializeField] private GameObject _uiDecalLayerPrefab;
    [SerializeField] private Transform _layersContainer;

    [Header("Visual Settings")]
    [SerializeField] private LayerVisualParameters _visualParameters;

    public event Action<DecalController> OnDecalLayerClicked;

    private IDecalEditor _editor;
    private Camera _mainCamera;
    private RectTransform _canvasRect;
    private Vector2 _lastWindowSize;

    private IDecalLayerFactory _layerFactory;
    private IDecalLayerVisualStrategy _visualStrategy;
    private ILayerOrderStrategy _orderStrategy;
    private ISelectionStrategy _selectionStrategy;

    private Dictionary<DecalController, IDecalLayer> _decalLayers =
        new Dictionary<DecalController, IDecalLayer>();

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeStrategies();
        ValidateReferences();
    }

    private void Update()
    {
        CheckWindowResize();
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
        if (_layersContainer == null)
            _layersContainer = _previewWindow;

        if (_uiDecalLayerPrefab == null)
            Debug.LogError("UIDecalLayer prefab is not assigned!");
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
            _layersContainer,
            _visualStrategy,
            _visualParameters
        );
    }

    private void SetupMask()
    {
        if (_previewWindow == null) return;

        // Добавляем Mask если нужно
        if (_previewWindow.GetComponent<Mask>() == null)
        {
            var mask = _previewWindow.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
        }

        // Добавляем Image для маски если нужно
        if (_previewWindow.GetComponent<Image>() == null)
        {
            var maskImage = _previewWindow.gameObject.AddComponent<Image>();
            maskImage.color = new Color(1, 1, 1, 0.1f);
            maskImage.raycastTarget = false;
        }
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
        if (_previewWindow != null && _previewWindow.rect.size != _lastWindowSize)
        {
            _lastWindowSize = _previewWindow.rect.size;
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

    #endregion

    #region Layer Management

    public void AddLayer(DecalController decal)
    {
        if (decal == null || _decalLayers.ContainsKey(decal)) return;

        var layer = _layerFactory.Create(decal, _previewWindow);
        if (layer == null) return;

        layer.OnLayerClicked += OnLayerClicked;
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

        // Показываем handles только если есть выбранная декаль
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

    public RectTransform GetRectTransform() => _previewWindow;
    public RectTransform GetCanvasRect() => _canvasRect;
    public ILayerVisualParameters GetParameters() => _visualParameters;

    #endregion
}