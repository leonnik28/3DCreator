using UnityEngine;
using UnityEngine.UI;

public class TransformControlsController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _handlesCanvasGroup;
    [SerializeField] private Slider _widthSlider;
    [SerializeField] private Slider _heightSlider;
    [SerializeField] private Slider _rotationSlider;
    [SerializeField] private Toggle _lockAspectToggle;

    [SerializeField] private DecalCornerHandle _topLeftHandle;
    [SerializeField] private DecalCornerHandle _topRightHandle;
    [SerializeField] private DecalCornerHandle _bottomLeftHandle;
    [SerializeField] private DecalCornerHandle _bottomRightHandle;
    [SerializeField] private DecalRotateHandle _rotateHandle;

    private IDecalEditor _editor;
    private DecalManager _decalManager;
    private bool _lockAspect = true;
    private Transform _handlesOriginalParent;
    private const float HandleSize = 24f;

    public bool LockAspect => _lockAspect;

    public void Initialize(IDecalEditor editor, DecalManager decalManager)
    {
        _editor = editor;
        _decalManager = decalManager;

        if (_handlesCanvasGroup != null)
            _handlesOriginalParent = _handlesCanvasGroup.transform.parent;

        SetupSliders();
        SetupHandles();
        ShowControls(false);
    }

    private void SetupSliders()
    {
        if (_widthSlider != null)
            _widthSlider.onValueChanged.AddListener(OnWidthChanged);

        if (_heightSlider != null)
            _heightSlider.onValueChanged.AddListener(OnHeightChanged);

        if (_rotationSlider != null)
            _rotationSlider.onValueChanged.AddListener(OnRotationChanged);

        if (_lockAspectToggle != null)
            _lockAspectToggle.onValueChanged.AddListener((value) => _lockAspect = value);
    }

    private void SetupHandles()
    {
        if (_topLeftHandle != null) _topLeftHandle.Initialize(_editor);
        if (_topRightHandle != null) _topRightHandle.Initialize(_editor);
        if (_bottomLeftHandle != null) _bottomLeftHandle.Initialize(_editor);
        if (_bottomRightHandle != null) _bottomRightHandle.Initialize(_editor);
        if (_rotateHandle != null) _rotateHandle.Initialize(_editor);
    }

    private void OnWidthChanged(float value)
    {
        var rect = _editor?.GetPreviewRect();
        if (rect == null) return;
        if (_lockAspect)
        {
            float aspect = rect.sizeDelta.x / rect.sizeDelta.y;
            rect.sizeDelta = new Vector2(value, value / aspect);
            _heightSlider.SetValueWithoutNotify(rect.sizeDelta.y);
        }
        else
        {
            rect.sizeDelta = new Vector2(value, rect.sizeDelta.y);
        }
        _editor.OnTransformChanged();
    }

    private void OnHeightChanged(float value)
    {
        var rect = _editor?.GetPreviewRect();
        if (rect == null) return;
        if (_lockAspect)
        {
            float aspect = rect.sizeDelta.x / rect.sizeDelta.y;
            rect.sizeDelta = new Vector2(value * aspect, value);
            _widthSlider.SetValueWithoutNotify(rect.sizeDelta.x);
        }
        else
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, value);
        }
        _editor.OnTransformChanged();
    }

    private void OnRotationChanged(float value)
    {
        var rect = _editor?.GetPreviewRect();
        if (rect == null) return;
        rect.eulerAngles = new Vector3(0, 0, value);
        _editor.OnTransformChanged();
    }

    public void ShowControls(bool show)
    {
        if (_handlesCanvasGroup != null)
        {
            _handlesCanvasGroup.alpha = show ? 1f : 0f;
            _handlesCanvasGroup.blocksRaycasts = show;

            if (show)
            {
                var layerRect = _editor?.GetPreviewRect() as RectTransform;
                if (layerRect != null)
                    PositionHandlesOnLayer(layerRect);
            }
            else if (_handlesOriginalParent != null)
            {
                _handlesCanvasGroup.transform.SetParent(_handlesOriginalParent);
            }
        }

        if (_topLeftHandle != null) _topLeftHandle.gameObject.SetActive(show);
        if (_topRightHandle != null) _topRightHandle.gameObject.SetActive(show);
        if (_bottomLeftHandle != null) _bottomLeftHandle.gameObject.SetActive(show);
        if (_bottomRightHandle != null) _bottomRightHandle.gameObject.SetActive(show);
        if (_rotateHandle != null) _rotateHandle.gameObject.SetActive(show);
        if (_centerDragZone != null) _centerDragZone.gameObject.SetActive(show);

        if (_widthSlider != null) _widthSlider.gameObject.SetActive(show);
        if (_heightSlider != null) _heightSlider.gameObject.SetActive(show);
        if (_rotationSlider != null) _rotationSlider.gameObject.SetActive(show);
        if (_lockAspectToggle != null)
        {
            _lockAspectToggle.gameObject.SetActive(show);
            _lockAspectToggle.SetIsOnWithoutNotify(_lockAspect);
        }
    }

    private void LateUpdate()
    {
        if (_handlesCanvasGroup != null && _handlesCanvasGroup.alpha > 0 && _editor != null)
        {
            var layerRect = _editor.GetPreviewRect() as RectTransform;
            if (layerRect != null && _handlesCanvasGroup.transform.parent == layerRect)
                PositionHandlesOnLayer(layerRect);
        }
    }

    [SerializeField] private DecalCenterDragZone _centerDragZone;

    private void PositionHandlesOnLayer(RectTransform layerRect)
    {
        if (layerRect == null) return;

        var handlesRect = _handlesCanvasGroup.GetComponent<RectTransform>();
        if (handlesRect == null) return;

        _handlesCanvasGroup.transform.SetParent(layerRect, false);
        _handlesCanvasGroup.transform.localRotation = Quaternion.identity;
        _handlesCanvasGroup.transform.localScale = Vector3.one;

        handlesRect.anchorMin = Vector2.zero;
        handlesRect.anchorMax = Vector2.one;
        handlesRect.offsetMin = Vector2.zero;
        handlesRect.offsetMax = Vector2.zero;

        float hw = HandleSize * 0.5f;
        PositionHandle(_topLeftHandle, 0, 1, hw, -hw);
        PositionHandle(_topRightHandle, 1, 1, -hw, -hw);
        PositionHandle(_bottomLeftHandle, 0, 0, hw, hw);
        PositionHandle(_bottomRightHandle, 1, 0, -hw, hw);

        if (_rotateHandle != null)
        {
            var rt = _rotateHandle.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, HandleSize * 1.2f);
                rt.sizeDelta = new Vector2(HandleSize, HandleSize);
            }
        }

        if (_centerDragZone != null)
        {
            _centerDragZone.transform.SetAsLastSibling();
            _centerDragZone.Initialize(layerRect, layerRect.parent as RectTransform, () => _editor?.OnTransformChanged());
        }
    }

    private void PositionHandle(DecalCornerHandle handle, float anchorX, float anchorY, float offX, float offY)
    {
        if (handle == null) return;
        var rt = handle.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(offX, offY);
        rt.sizeDelta = new Vector2(HandleSize, HandleSize);
    }

    public void UpdateFromPreview(RectTransform previewRect)
    {
        if (_widthSlider != null)
            _widthSlider.SetValueWithoutNotify(previewRect.sizeDelta.x);
        if (_heightSlider != null)
            _heightSlider.SetValueWithoutNotify(previewRect.sizeDelta.y);
        if (_rotationSlider != null)
            _rotationSlider.SetValueWithoutNotify(previewRect.eulerAngles.z);
    }
}