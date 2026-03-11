using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class UIDecalEditorPanel : UIPanelBase, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("Preview")]
    [SerializeField] private RectTransform _previewRect;
    [SerializeField] private RawImage _previewImage;
    [SerializeField] private RectTransform _selectionFrame;

    [Header("Controls")]
    [SerializeField] private Slider _widthSlider;
    [SerializeField] private Slider _heightSlider;
    [SerializeField] private Slider _rotationSlider;
    [SerializeField] private Toggle _lockAspectToggle;

    [Header("Buttons")]
    [SerializeField] private Button _applyButton;
    [SerializeField] private Button _cancelButton;

    [Header("Settings")]
    [SerializeField] private float _minSize = 50f;
    [SerializeField] private float _maxSize = 500f;

    // События
    public event Action<Vector2, Vector2, float> OnTransformChanged;
    public event Action OnApplyClicked;
    public event Action OnCancelClicked;

    private bool _isDragging = false;
    private bool _lockAspect = true;
    private Vector2 _dragStartPoint;
    private Vector2 _originalPosition;

    protected override void OnShow()
    {
        SetupControls();
        SetupButtons();
    }

    private void SetupControls()
    {
        if (_widthSlider != null)
        {
            _widthSlider.minValue = _minSize;
            _widthSlider.maxValue = _maxSize;
            _widthSlider.onValueChanged.AddListener(OnWidthChanged);
        }

        if (_heightSlider != null)
        {
            _heightSlider.minValue = _minSize;
            _heightSlider.maxValue = _maxSize;
            _heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }

        if (_rotationSlider != null)
        {
            _rotationSlider.minValue = 0f;
            _rotationSlider.maxValue = 360f;
            _rotationSlider.onValueChanged.AddListener(OnRotationChanged);
        }

        if (_lockAspectToggle != null)
        {
            _lockAspectToggle.isOn = _lockAspect;
            _lockAspectToggle.onValueChanged.AddListener((value) => _lockAspect = value);
        }
    }

    private void SetupButtons()
    {
        if (_applyButton != null)
            _applyButton.onClick.AddListener(() => OnApplyClicked?.Invoke());

        if (_cancelButton != null)
            _cancelButton.onClick.AddListener(() => OnCancelClicked?.Invoke());
    }

    public void SetTexture(Texture2D texture)
    {
        if (_previewImage != null && texture != null)
        {
            _previewImage.texture = texture;
            _previewImage.gameObject.SetActive(true);

            // Автоматический размер
            float aspect = (float)texture.width / texture.height;
            float startWidth = Mathf.Clamp(200 * aspect, _minSize, _maxSize);
            float startHeight = Mathf.Clamp(200, _minSize, _maxSize);

            _previewRect.sizeDelta = new Vector2(startWidth, startHeight);

            if (_widthSlider != null) _widthSlider.value = startWidth;
            if (_heightSlider != null) _heightSlider.value = startHeight;

            _previewRect.anchoredPosition = Vector2.zero;
        }
    }

    public void SetTransform(Vector2 position, Vector2 size, float rotation)
    {
        _previewRect.anchoredPosition = position;
        _previewRect.sizeDelta = size;
        _previewRect.eulerAngles = new Vector3(0, 0, rotation);

        if (_widthSlider != null) _widthSlider.value = size.x;
        if (_heightSlider != null) _heightSlider.value = size.y;
        if (_rotationSlider != null) _rotationSlider.value = rotation;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector2 delta = eventData.position - _dragStartPoint;
        _previewRect.anchoredPosition = _originalPosition + delta;

        NotifyTransformChanged();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _isDragging = true;
        _dragStartPoint = eventData.position;
        _originalPosition = _previewRect.anchoredPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
    }

    private void OnWidthChanged(float value)
    {
        if (_lockAspect && _heightSlider != null)
        {
            float aspect = _previewRect.sizeDelta.x / _previewRect.sizeDelta.y;
            float newHeight = value / aspect;
            _previewRect.sizeDelta = new Vector2(value, newHeight);
            _heightSlider.value = newHeight;
        }
        else
        {
            _previewRect.sizeDelta = new Vector2(value, _previewRect.sizeDelta.y);
        }

        NotifyTransformChanged();
    }

    private void OnHeightChanged(float value)
    {
        if (_lockAspect && _widthSlider != null)
        {
            float aspect = _previewRect.sizeDelta.x / _previewRect.sizeDelta.y;
            float newWidth = value * aspect;
            _previewRect.sizeDelta = new Vector2(newWidth, value);
            _widthSlider.value = newWidth;
        }
        else
        {
            _previewRect.sizeDelta = new Vector2(_previewRect.sizeDelta.x, value);
        }

        NotifyTransformChanged();
    }

    private void OnRotationChanged(float value)
    {
        _previewRect.eulerAngles = new Vector3(0, 0, value);
        NotifyTransformChanged();
    }

    private void NotifyTransformChanged()
    {
        OnTransformChanged?.Invoke(
            _previewRect.anchoredPosition,
            _previewRect.sizeDelta,
            _previewRect.eulerAngles.z
        );
    }
}