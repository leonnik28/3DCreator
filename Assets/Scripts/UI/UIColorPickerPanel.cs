using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI выбора части модели и цвета для неё.
/// </summary>
public class UIColorPickerPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ModelColorizer _colorizer;
    [SerializeField] private RectTransform _partButtonsContainer;
    [SerializeField] private GameObject _partButtonPrefab;
    [SerializeField] private ColorPicker _colorPicker;
    [SerializeField] private GameObject _noPartsHint;

    [Header("Preset Colors")]
    [SerializeField] private Color[] _presetColors;
    [SerializeField] private Transform _presetContainer;

    private int _selectedPartIndex = -1;
    private readonly List<GameObject> _partButtons = new List<GameObject>();

    private void Start()
    {
        if (_colorizer == null) _colorizer = FindObjectOfType<ModelColorizer>();
        if (_colorPicker == null) _colorPicker = GetComponentInChildren<ColorPicker>();

        var mm = FindObjectOfType<ModelManager>();
        if (mm != null) mm.OnModelChanged += OnModelChanged;

        if (_presetColors == null || _presetColors.Length == 0)
            _presetColors = new[] { Color.red, Color.blue, Color.green, Color.yellow, Color.white, Color.black };

        if (_colorPicker != null)
            _colorPicker.OnColorChanged += ApplyColor;

        BuildPresetButtons();
        RefreshParts();
    }

    private void OnDestroy()
    {
        if (_colorPicker != null)
            _colorPicker.OnColorChanged -= ApplyColor;
    }

    private void OnModelChanged(GameObject model, int index) => RefreshParts();

    private void RefreshParts()
    {
        foreach (var b in _partButtons) Destroy(b);
        _partButtons.Clear();
        _selectedPartIndex = -1;

        var config = _colorizer?.GetCurrentConfig();
        if (config == null || config.PartCount == 0)
        {
            if (_noPartsHint != null) _noPartsHint.SetActive(true);
            if (_partButtonsContainer != null) _partButtonsContainer.gameObject.SetActive(false);
            return;
        }

        if (_noPartsHint != null) _noPartsHint.SetActive(false);
        if (_partButtonsContainer != null) _partButtonsContainer.gameObject.SetActive(true);

        for (int i = 0; i < config.PartCount; i++)
        {
            var entry = config.GetPart(i);
            if (entry == null) continue;

            GameObject btnObj = _partButtonPrefab != null && _partButtonsContainer != null
                ? Instantiate(_partButtonPrefab, _partButtonsContainer)
                : CreateDefaultButton(_partButtonsContainer);
            _partButtons.Add(btnObj);

            int idx = i;
            var button = btnObj.GetComponent<Button>();
            if (button != null)
                button.onClick.AddListener(() => SelectPart(idx));

            var text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = string.IsNullOrEmpty(entry.DisplayName) ? entry.PartId ?? $"Part {i}" : entry.DisplayName;
        }
    }

    private GameObject CreateDefaultButton(Transform parent)
    {
        var go = new GameObject("PartButton");
        go.transform.SetParent(parent);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 30);
        var btn = go.AddComponent<Button>();
        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(go.transform);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var txt = txtObj.AddComponent<TextMeshProUGUI>();
        txt.text = "Part";
        txt.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private void BuildPresetButtons()
    {
        if (_presetContainer == null) return;

        foreach (Transform t in _presetContainer)
            Destroy(t.gameObject);

        for (int i = 0; i < _presetColors.Length; i++)
        {
            int idx = i;
            var c = _presetColors[i];
            var go = new GameObject("Preset");
            go.transform.SetParent(_presetContainer);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(24, 24);
            var img = go.AddComponent<Image>();
            img.color = c;
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => ApplyColor(c));
        }
    }

    private void SelectPart(int index)
    {
        _selectedPartIndex = index;
        if (_colorPicker != null)
        {
            var config = _colorizer?.GetCurrentConfig();
            var entry = config?.GetPart(index);
            if (entry?.TargetRenderer != null)
            {
                var mat = entry.TargetRenderer.sharedMaterial;
                if (mat != null && mat.HasProperty("_Color"))
                    _colorPicker.SetColor(mat.color);
            }
        }
    }

    public void ApplyColor(Color color)
    {
        if (_selectedPartIndex >= 0 && _colorizer != null)
            _colorizer.SetPartColor(_selectedPartIndex, color);
    }
}
