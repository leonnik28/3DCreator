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
    [Tooltip("Если задана кнопка, цвет применяется только по нажатию. Если не задана — создастся автоматически.")]
    [SerializeField] private Button _applyColorButton;

    [Header("Preset Colors")]
    [SerializeField] private Color[] _presetColors;
    [SerializeField] private Transform _presetContainer;

    private int _selectedPartIndex = -1;
    private readonly List<GameObject> _partButtons = new List<GameObject>();
    private readonly List<Outline> _partOutlines = new List<Outline>();
    private Color _pendingColor = Color.white;

    private void Start()
    {
        if (_colorizer == null) _colorizer = FindObjectOfType<ModelColorizer>();
        if (_colorPicker == null) _colorPicker = GetComponentInChildren<ColorPicker>();

        var mm = FindObjectOfType<ModelManager>();
        if (mm != null) mm.OnModelChanged += OnModelChanged;

        if (_presetColors == null || _presetColors.Length == 0)
            _presetColors = new[] { Color.red, Color.blue, Color.green, Color.yellow, Color.white, Color.black };

        if (_colorPicker != null)
            _colorPicker.OnColorChanged += OnColorChanged;

        BuildPresetButtons();
        EnsureApplyButton();
        HookApplyButtonIfNeeded();
        RefreshParts();
        // Модель спавнится в Awake, а config в ModelColorizer собирается в Start.
        // Поэтому делаем ещё один Refresh на следующий кадр.
        StartCoroutine(RefreshPartsNextFrame());
    }

    private void HookApplyButtonIfNeeded()
    {
        if (_applyColorButton == null) return;
        _applyColorButton.onClick.RemoveListener(ApplyPendingColor);
        _applyColorButton.onClick.AddListener(ApplyPendingColor);
    }

    private System.Collections.IEnumerator RefreshPartsNextFrame()
    {
        yield return new WaitForEndOfFrame();
        RefreshParts();
    }

    private void OnDestroy()
    {
        if (_colorPicker != null)
            _colorPicker.OnColorChanged -= OnColorChanged;
    }

    private void OnModelChanged(GameObject model, int index)
    {
        // Важный момент: ModelColorizer обновляет конфиг частей в своём обработчике OnModelChanged.
        // Если мы пересоберём UI сразу, можем прочитать "старый" конфиг.
        // Поэтому делаем Refresh на следующий кадр.
        StartCoroutine(RefreshPartsNextFrame());
    }

    private void RefreshParts()
    {
        foreach (var b in _partButtons) Destroy(b);
        _partButtons.Clear();
        for (int i = 0; i < _partOutlines.Count; i++)
        {
            if (_partOutlines[i] != null)
                Destroy(_partOutlines[i]);
        }
        _partOutlines.Clear();
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

            // Визуальный индикатор выбранной части: обводка вокруг Image или текста.
            Outline outline = btnObj.GetComponent<Outline>() ?? btnObj.GetComponentInChildren<Outline>();
            if (outline == null)
            {
                var img = btnObj.GetComponent<Image>() ?? btnObj.GetComponentInChildren<Image>();
                var tmpText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (img != null)
                    outline = img.gameObject.AddComponent<Outline>();
                else if (tmpText != null)
                    outline = tmpText.gameObject.AddComponent<Outline>();
            }

            if (outline != null)
            {
                outline.effectDistance = new Vector2(2f, 2f);
                outline.effectColor = new Color(1f, 0.95f, 0.2f, 1f);
                outline.enabled = false;
            }
            _partOutlines.Add(outline);
        }

        SetApplyButtonInteractable(_selectedPartIndex >= 0);

        // Чтобы было сразу понятно, какая часть выбрана.
        if (config.PartCount > 0)
            SelectPart(0);
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
            btn.onClick.AddListener(() =>
            {
                _pendingColor = c;
                if (_colorPicker != null)
                    _colorPicker.SetColor(c); // только обновляет UI-предпросмотр
                SetApplyButtonInteractable(_selectedPartIndex >= 0);
            });
        }
    }

    private void SelectPart(int index)
    {
        _selectedPartIndex = index;

        for (int i = 0; i < _partOutlines.Count; i++)
        {
            if (_partOutlines[i] == null) continue;
            _partOutlines[i].enabled = i == index;
        }

        if (_colorPicker != null)
        {
            var config = _colorizer?.GetCurrentConfig();
            var entry = config?.GetPart(index);
            if (entry?.TargetRenderer != null)
            {
                if (_colorizer != null && _colorizer.TryGetPartColor(index, out var cachedColor))
                {
                    _pendingColor = cachedColor;
                    _colorPicker.SetColor(cachedColor);
                    SetApplyButtonInteractable(true);
                    return;
                }

                // Fallback: берём цвет из sharedMaterial по MaterialIndex (если ещё не было Apply).
                var mats = entry.TargetRenderer.sharedMaterials;
                int matIndex = entry.MaterialIndex >= 0 ? entry.MaterialIndex : 0;
                if (mats != null && matIndex >= 0 && matIndex < mats.Length)
                {
                    var mat = mats[matIndex];
                    if (mat != null && mat.HasProperty("_Color"))
                    {
                        _pendingColor = mat.color;
                        _colorPicker.SetColor(mat.color);
                    }
                }
            }
        }

        SetApplyButtonInteractable(_selectedPartIndex >= 0);
    }

    private void OnColorChanged(Color color)
    {
        _pendingColor = color;
        // Применяем цвет только по нажатию кнопки Apply.
        SetApplyButtonInteractable(_selectedPartIndex >= 0);
    }

    private void ApplyPendingColor()
    {
        if (_selectedPartIndex < 0 || _colorizer == null)
            return;

        var color = _colorPicker != null ? _colorPicker.Color : _pendingColor;
        _colorizer.SetPartColor(_selectedPartIndex, color);
    }

    private void EnsureApplyButton()
    {
        if (_applyColorButton != null)
        {
            HookApplyButtonIfNeeded();
            return;
        }

        // Создаём кнопку на лету, если не настроена в сцене.
        var panelRt = GetComponent<RectTransform>();
        if (panelRt == null)
            return;

        var btnGo = new GameObject("ApplyColorButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(panelRt, false);

        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(160, 32);
        rt.anchoredPosition = new Vector2(0f, Mathf.Min(10f, panelRt.rect.height * 0.25f));

        var img = btnGo.GetComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "Apply";
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        if (tmp.font == null)
        {
            // Подхватываем один из стандартных шрифтов TMP из папки Resources проекта.
            // Если не найден — Unity оставит дефолт, и кнопка может отображать только фон/бордер.
            tmp.font = Resources.Load<TMPro.TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
            if (tmp.font != null)
                tmp.fontSize = 18;
        }

        _applyColorButton = btnGo.GetComponent<Button>();
        // Гарантируем, что кнопка знает, какое графическое полотно подсвечивать/использовать.
        _applyColorButton.targetGraphic = img;
        HookApplyButtonIfNeeded();

        SetApplyButtonInteractable(_selectedPartIndex >= 0);
    }

    private void SetApplyButtonInteractable(bool enabled)
    {
        if (_applyColorButton == null)
            return;

        _applyColorButton.interactable = enabled;
    }
}
