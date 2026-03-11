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

    public void Initialize(IDecalEditor editor, DecalManager decalManager)
    {
        _editor = editor;
        _decalManager = decalManager;

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
        var rect = _editor.GetPreviewRect();
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
        var rect = _editor.GetPreviewRect();
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
        _editor.GetPreviewRect().eulerAngles = new Vector3(0, 0, value);
        _editor.OnTransformChanged();
    }

    public void ShowControls(bool show)
    {
        if (_handlesCanvasGroup != null)
        {
            _handlesCanvasGroup.alpha = show ? 1f : 0f;
            _handlesCanvasGroup.blocksRaycasts = show;
        }

        if (_topLeftHandle != null) _topLeftHandle.gameObject.SetActive(show);
        if (_topRightHandle != null) _topRightHandle.gameObject.SetActive(show);
        if (_bottomLeftHandle != null) _bottomLeftHandle.gameObject.SetActive(show);
        if (_bottomRightHandle != null) _bottomRightHandle.gameObject.SetActive(show);
        if (_rotateHandle != null) _rotateHandle.gameObject.SetActive(show);

        if (_widthSlider != null) _widthSlider.gameObject.SetActive(show);
        if (_heightSlider != null) _heightSlider.gameObject.SetActive(show);
        if (_rotationSlider != null) _rotationSlider.gameObject.SetActive(show);
        if (_lockAspectToggle != null) _lockAspectToggle.gameObject.SetActive(show);
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