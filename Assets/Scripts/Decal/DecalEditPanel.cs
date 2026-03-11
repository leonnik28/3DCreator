using UnityEngine;
using PreviewSystem.Services;
using PreviewSystem.Interfaces;

public class DecalEditPanel : MonoBehaviour, IDecalEditor
{
    [Header("Controllers")]
    [SerializeField] private PreviewWindowController _previewController;
    [SerializeField] private TransformControlsController _transformControls;
    [SerializeField] private ImageLoaderController _imageLoader;

    [Header("UI Panels")]
    [SerializeField] private UIDecalsActionPanel _actionPanel; // Панель действий

    private DecalManager _decalManager;
    private DecalController _activeDecal;
    private IDecalRemovalService _removalService;

    private void Start()
    {
        _decalManager = FindObjectOfType<DecalManager>();
        if (_decalManager == null)
        {
            Debug.LogError("DecalManager not found!");
            return;
        }

        InitializeServices();
        InitializeControllers();
        SubscribeToEvents();
    }

    private void InitializeServices()
    {
        _removalService = new DecalRemovalService(_decalManager, this);
    }

    private void InitializeControllers()
    {
        _previewController.Initialize(this);
        _transformControls.Initialize(this, _decalManager);
        _imageLoader.Initialize(this, _decalManager);

        // Инициализируем панель действий с сервисом удаления
        _actionPanel?.Initialize(_removalService);
    }

    private void SubscribeToEvents()
    {
        // Preview events
        if (_previewController != null)
            _previewController.OnDecalLayerClicked += OnDecalLayerClicked;

        // Image loader events
        if (_imageLoader != null)
            _imageLoader.OnImageLoaded += OnImageLoaded;

        // Action panel events
        if (_actionPanel != null)
        {
            _actionPanel.OnDeleteSelectedClicked += OnDeleteSelectedClicked;
            _actionPanel.OnClearAllClicked += OnClearAllClicked;
            _actionPanel.OnSnapshotClicked += OnSnapshotClicked;
        }

        // Decal manager events
        if (_decalManager != null)
        {
            _decalManager.OnDecalCreated += OnDecalCreated;
            _decalManager.OnDecalSelected += OnDecalSelected;
            _decalManager.OnDecalDeleted += OnDecalDeleted;
            _decalManager.OnDecalTransformChanged += OnDecalTransformChanged;
        }
    }

    // Обработчики для панели действий
    private void OnDeleteSelectedClicked()
    {
        _removalService?.DeleteSelected();
    }

    private void OnClearAllClicked()
    {
        _decalManager?.ClearAllDecals();
    }

    private void OnSnapshotClicked()
    {
        // TODO: Implement screenshot functionality
        Debug.Log("Snapshot clicked");
    }

    private void OnDecalLayerClicked(DecalController decal)
    {
        _decalManager.SelectDecal(decal);
    }

    private void OnDecalCreated(DecalController decal)
    {
        _previewController.AddLayer(decal);
        _decalManager.SelectDecal(decal);
    }

    private void OnDecalSelected(DecalController decal)
    {
        SetActiveDecal(decal);
        _previewController.HighlightSelected(decal);
        _actionPanel?.UpdateDeleteButtonState(); // Обновляем состояние кнопки
    }

    private void OnDecalDeleted(DecalController decal)
    {
        _previewController.RemoveLayer(decal);

        if (_activeDecal == decal)
        {
            SetActiveDecal(null);
        }

        _actionPanel?.UpdateDeleteButtonState(); // Обновляем состояние кнопки
    }

    private void OnDecalTransformChanged(DecalController decal)
    {
        _previewController.UpdateLayerPosition(decal);
    }

    public void SetActiveDecal(DecalController decal)
    {
        if (_activeDecal == decal) return;

        if (_activeDecal != null)
            _activeDecal.SetSelected(false);

        _activeDecal = decal;

        if (_activeDecal != null)
            _activeDecal.SetSelected(true);

        UpdateUIFromSelectedDecal();
        _actionPanel?.UpdateDeleteButtonState(); // Обновляем состояние кнопки
    }

    private void UpdateUIFromSelectedDecal()
    {
        if (_activeDecal != null)
        {
            _transformControls?.ShowControls(true);

            if (_transformControls != null && _previewController != null)
            {
                var rect = _previewController.GetRectTransform();
                _transformControls.UpdateFromPreview(rect);
            }
        }
        else
        {
            _transformControls?.ShowControls(false);
        }
    }

    private void OnImageLoaded(Texture2D texture)
    {
        _decalManager?.CreateNewDecal(texture);
    }

    public void OnTransformChanged()
    {
        if (_activeDecal == null) return;

        RectTransform previewRect = _previewController.GetRectTransform();

        _decalManager?.UpdateEditingDecal(
            previewRect.anchoredPosition,
            previewRect.sizeDelta,
            previewRect.eulerAngles.z,
            _previewController.GetCanvasRect()
        );

        _previewController.UpdateLayerPosition(_activeDecal);
    }

    public RectTransform GetPreviewRect() => _previewController?.GetRectTransform();
    public DecalController GetActiveDecal() => _activeDecal;

    private void UnsubscribeFromEvents()
    {
        if (_previewController != null)
            _previewController.OnDecalLayerClicked -= OnDecalLayerClicked;

        if (_imageLoader != null)
            _imageLoader.OnImageLoaded -= OnImageLoaded;

        if (_actionPanel != null)
        {
            _actionPanel.OnDeleteSelectedClicked -= OnDeleteSelectedClicked;
            _actionPanel.OnClearAllClicked -= OnClearAllClicked;
            _actionPanel.OnSnapshotClicked -= OnSnapshotClicked;
        }

        if (_decalManager != null)
        {
            _decalManager.OnDecalCreated -= OnDecalCreated;
            _decalManager.OnDecalSelected -= OnDecalSelected;
            _decalManager.OnDecalDeleted -= OnDecalDeleted;
            _decalManager.OnDecalTransformChanged -= OnDecalTransformChanged;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
}