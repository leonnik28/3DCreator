using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Простой выбор цвета через R,G,B слайдеры.
/// </summary>
public class ColorPicker : MonoBehaviour
{
    [SerializeField] private Slider _rSlider;
    [SerializeField] private Slider _gSlider;
    [SerializeField] private Slider _bSlider;
    [SerializeField] private Image _previewImage;

    public System.Action<Color> OnColorChanged;

    private Color _color;
    private bool _ignoreUpdates;

    public Color Color
    {
        get => _color;
        set { _color = value; UpdateSliders(); UpdatePreview(); }
    }

    public void SetColor(Color c)
    {
        _color = c;
        UpdateSliders();
        UpdatePreview();
    }

    private void Start()
    {
        if (_rSlider != null) _rSlider.onValueChanged.AddListener(v => OnSliderChanged());
        if (_gSlider != null) _gSlider.onValueChanged.AddListener(v => OnSliderChanged());
        if (_bSlider != null) _bSlider.onValueChanged.AddListener(v => OnSliderChanged());
    }

    private void OnSliderChanged()
    {
        if (_ignoreUpdates) return;
        _color.r = _rSlider != null ? _rSlider.value : 1;
        _color.g = _gSlider != null ? _gSlider.value : 1;
        _color.b = _bSlider != null ? _bSlider.value : 1;
        _color.a = 1;
        UpdatePreview();
        OnColorChanged?.Invoke(_color);
    }

    private void UpdateSliders()
    {
        _ignoreUpdates = true;
        if (_rSlider != null) _rSlider.value = _color.r;
        if (_gSlider != null) _gSlider.value = _color.g;
        if (_bSlider != null) _bSlider.value = _color.b;
        _ignoreUpdates = false;
    }

    private void UpdatePreview()
    {
        if (_previewImage != null)
            _previewImage.color = _color;
    }
}
