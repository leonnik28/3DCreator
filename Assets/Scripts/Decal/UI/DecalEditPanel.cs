using UnityEngine;
using PreviewSystem.Services;
using PreviewSystem.Interfaces;
using Fotocentr.Core;

public class DecalEditPanel : MonoBehaviour, IDecalEditor, IDecalEditorDependencies
{
    [Header("Controllers")]
    [SerializeField] private PreviewWindowController _previewController;
    [SerializeField] private TransformControlsController _transformControls;
    [SerializeField] private ImageLoaderController _imageLoader;

    [Header("UI Panels")]
    [SerializeField] private UIDecalsActionPanel _actionPanel;

    [Header("Services (injected by CompositionRoot if empty)")]
    [SerializeField] private DecalManager _decalManager;
    [SerializeField] private SceneCaptureService _captureService;

    private DecalController _activeDecal;
    private IDecalRemovalService _removalService;
    private ISceneCapture _sceneCapture;

    public void Inject(DecalManager decalManager, ISceneCapture sceneCapture)
    {
        _decalManager = decalManager;
        _sceneCapture = sceneCapture;
    }

    private void Start()
    {
        if (_decalManager == null)
            _decalManager = FindObjectOfType<DecalManager>();

        if (_decalManager == null)
        {
            Debug.LogError("DecalManager not found! Assign in Inspector or add CompositionRoot.");
            return;
        }

        if (_sceneCapture == null && _captureService != null)
            _sceneCapture = _captureService;
        if (_sceneCapture == null)
            _sceneCapture = FindObjectOfType<SceneCaptureService>();

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

        // ?????????????? ?????? ???????? ? ???????? ????????
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

    // ??????????? ??? ?????? ????????
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
        _sceneCapture?.TakeScreenshot();
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
        _actionPanel?.UpdateDeleteButtonState(); // ????????? ????????? ??????
    }

    private void OnDecalDeleted(DecalController decal)
    {
        if (_activeDecal == decal)
        {
            SetActiveDecal(null);
        }

        _previewController.RemoveLayer(decal);
        _actionPanel?.UpdateDeleteButtonState(); // ????????? ????????? ??????
    }

    private void OnDecalTransformChanged(DecalController decal)
    {
        // ?? ?????????????? 2D ?? 3D ? ???????? ?????? ??? ?????????????? ??? 2D.
        // UpdateLayerPosition ??????? ?? layer.UpdateTransform ? ??????? ?? rotation/position.
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
        _actionPanel?.UpdateDeleteButtonState(); // ????????? ????????? ??????
    }

    private void UpdateUIFromSelectedDecal()
    {
        if (_activeDecal != null)
        {
            _transformControls?.ShowControls(true);

            if (_previewController != null)
                OnTransformChanged();
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

        RectTransform layerRect = _previewController.GetLayerRect(_activeDecal);
        Canvas canvas = _previewController.GetCanvas();

        _decalManager?.UpdateEditingDecal(layerRect, _previewController.GetRectTransform(), canvas, layerRect.eulerAngles.z);
    }

    public RectTransform GetPreviewRect() =>
        _activeDecal != null && _previewController != null
            ? _previewController.GetLayerRect(_activeDecal)
            : _previewController?.GetRectTransform();

    public bool GetLockAspectRatio() => _transformControls != null && _transformControls.LockAspect;
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