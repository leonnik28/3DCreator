using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// UI выбора модели: горизонтальный скролл с превью.
/// </summary>
public class UIModelSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ModelManager _modelManager;
    [SerializeField] private RectTransform _content;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private Button _prevButton;
    [SerializeField] private Button _nextButton;
    [SerializeField] private TextMeshProUGUI _label;

    [Header("Placement")]
    [Tooltip("Если задано — скролл-зона со списком моделей будет создана внутри этого RectTransform (вместо родителя _content).")]
    [SerializeField] private RectTransform _scrollPlacementRoot;
    [Tooltip("Только если Placement не задан: отступы скролл-зоны относительно родителя _content (как раньше). Если задан Placement — скролл на весь Placement (якоря 0..1).")]
    [SerializeField] private Vector2 _scrollAnchorMin = new Vector2(0.04f, 0.14f);
    [Tooltip("Только если Placement не задан: отступы скролл-зоны относительно родителя _content.")]
    [SerializeField] private Vector2 _scrollAnchorMax = new Vector2(0.96f, 0.58f);
    [Tooltip("Если включено — создаём ScrollRect/Viewport автоматически. Если выключено — ожидается, что они уже собраны в сцене.")]
    [SerializeField] private bool _autoBuildScrollArea = true;
    [Tooltip("Выравнивание превью в строке. MiddleLeft — как раньше; MiddleCenter — если мало моделей и полоса не заполняет ширину, не «прилипает» к левому краю.")]
    [SerializeField] private TextAnchor _itemsAlignment = TextAnchor.MiddleLeft;

    [Header("Selection Visuals")]
    [SerializeField, Range(0.1f, 1f)] private float _inactiveAlpha = 0.6f;

    private readonly List<GameObject> _items = new List<GameObject>();

    private IEnumerator Start()
    {
        if (_modelManager == null)
            _modelManager = FindObjectOfType<ModelManager>();

        if (_modelManager == null || _modelManager.Database == null)
        {
            gameObject.SetActive(false);
            yield break;
        }

        EnsureHorizontalScrollArea();
        EnsureContentLayout();

        if (_prevButton != null) _prevButton.onClick.AddListener(() => _modelManager.PreviousModel());
        if (_nextButton != null) _nextButton.onClick.AddListener(() => _modelManager.NextModel());

        BuildItems();
        _modelManager.OnModelChanged += OnModelChanged;
        UpdateSelectionVisual();

        // После первого кадра лейаут и ScrollRect стабилизируются (иначе content часто остаётся по ширине viewport).
        yield return null;
        ApplyContentWidthFromChildren();
        Canvas.ForceUpdateCanvases();
        if (_content != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        var scroll = _content != null ? _content.GetComponentInParent<ScrollRect>() : null;
        if (scroll != null)
            scroll.horizontalNormalizedPosition = 0f;
    }

    private void OnDestroy()
    {
        if (_modelManager != null)
            _modelManager.OnModelChanged -= OnModelChanged;
    }

    /// <summary>
    /// Content часто вешают прямо на панель без Viewport/Mask — превью уезжают за край. Собираем ScrollRect + RectMask2D.
    /// </summary>
    private void EnsureHorizontalScrollArea()
    {
        if (_content == null) return;
        if (!_autoBuildScrollArea)
            return;

        if (_content.GetComponentInParent<ScrollRect>() != null)
            return;

        var panelRt = (_scrollPlacementRoot != null ? _scrollPlacementRoot : (_content.parent as RectTransform));
        if (panelRt == null) return;

        bool usePlacementSlot = _scrollPlacementRoot != null;

        // Раньше подгоняли ширину родителя _content — для отдельного Placement это ломает растягивающиеся RectTransform и смещает UI.
        if (!usePlacementSlot && panelRt.rect.width < 240f)
            panelRt.sizeDelta = new Vector2(Mathf.Max(panelRt.sizeDelta.x, 480f), Mathf.Max(panelRt.sizeDelta.y, 128f));

        var scrollGo = new GameObject("ModelScrollArea", typeof(RectTransform));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);

        // Индекс sibling от _content брался из другого родителя — при Placement скролл оказывался не там / сбоку.
        if (usePlacementSlot)
            scrollRt.SetAsLastSibling();
        else
            scrollRt.SetSiblingIndex(_content.GetSiblingIndex());

        if (usePlacementSlot)
        {
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
        }
        else
        {
            scrollRt.anchorMin = _scrollAnchorMin;
            scrollRt.anchorMax = _scrollAnchorMax;
        }

        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        scrollRt.anchoredPosition = Vector2.zero;
        scrollRt.localScale = Vector3.one;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.SetParent(scrollRt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;
        var vimg = viewportGo.GetComponent<Image>();
        vimg.color = new Color(1f, 1f, 1f, 0f);
        vimg.raycastTarget = true;

        _content.SetParent(viewportRt, false);
        _content.anchorMin = new Vector2(0f, 0f);
        _content.anchorMax = new Vector2(0f, 1f);
        _content.pivot = new Vector2(0f, 0.5f);
        _content.anchoredPosition = Vector2.zero;
        _content.offsetMin = new Vector2(0f, 0f);
        _content.offsetMax = new Vector2(0f, 0f);

        var scr = scrollGo.AddComponent<ScrollRect>();
        scr.viewport = viewportRt;
        scr.content = _content;
        scr.horizontal = true;
        scr.vertical = false;
        scr.movementType = ScrollRect.MovementType.Clamped;
        scr.scrollSensitivity = 35f;

        EnsureContentLayout();
    }

    private void EnsureContentLayout()
    {
        if (_content == null)
            return;

        var hlg = _content.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
            hlg = _content.gameObject.AddComponent<HorizontalLayoutGroup>();

        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.spacing = 8f;
        hlg.childAlignment = _itemsAlignment;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        // ContentSizeFitter + HorizontalLayoutGroup на одном Rect часто даёт ширину = viewport → видна одна карточка.
        // Ширину считаем вручную в ApplyContentWidthFromChildren после спавна элементов.
        var fitter = _content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            Destroy(fitter);
    }

    private void ApplyContentWidthFromChildren()
    {
        if (_content == null) return;

        var hlg = _content.GetComponent<HorizontalLayoutGroup>();
        float padL = hlg != null ? hlg.padding.left : 0;
        float padR = hlg != null ? hlg.padding.right : 0;
        float spacing = hlg != null ? hlg.spacing : 8f;

        int n = _content.childCount;
        float total = padL + padR;
        for (int i = 0; i < n; i++)
        {
            var child = _content.GetChild(i);
            var le = child.GetComponent<LayoutElement>();
            float w = 96f;
            if (le != null && le.preferredWidth > 0f)
                w = le.preferredWidth;
            else
            {
                var crt = child as RectTransform;
                if (crt != null && crt.rect.width > 1f)
                    w = crt.rect.width;
            }

            total += w;
            if (i < n - 1)
                total += spacing;
        }

        _content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(total, 1f));
    }

    private void BuildItems()
    {
        foreach (var item in _items)
            Destroy(item);
        _items.Clear();

        var db = _modelManager.Database;
        if (_itemPrefab == null || _content == null || db.Count == 0)
            return;

        for (int i = 0; i < db.Count; i++)
        {
            int index = i;
            var entry = db[i];
            var go = Instantiate(_itemPrefab, _content);
            _items.Add(go);

            var le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            if (le.preferredWidth <= 0) le.preferredWidth = 96f;
            if (le.preferredHeight <= 0) le.preferredHeight = 96f;

            // Префаб с stretch-якорями ломает горизонтальный ряд: элементы накладываются / занимают всю ширину content.
            var itemRt = go.GetComponent<RectTransform>();
            if (itemRt != null)
            {
                itemRt.anchorMin = new Vector2(0f, 0.5f);
                itemRt.anchorMax = new Vector2(0f, 0.5f);
                itemRt.pivot = new Vector2(0.5f, 0.5f);
                itemRt.sizeDelta = new Vector2(le.preferredWidth, le.preferredHeight);
            }

            var button = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(() => _modelManager.SetModel(index));

            var image = go.GetComponentInChildren<Image>();
            if (image != null && entry.Thumbnail != null)
                image.sprite = entry.Thumbnail;

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = string.IsNullOrEmpty(entry.DisplayName) ? entry.Prefab?.name ?? "?" : entry.DisplayName;

            EnsureCanvasGroup(go);
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        ApplyContentWidthFromChildren();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private void OnModelChanged(GameObject model, int index)
    {
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        int idx = _modelManager.CurrentIndex;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            bool isSelected = i == idx;

            var canvasGroup = item.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = isSelected ? 1f : _inactiveAlpha;
        }

        if (_label != null && _modelManager.Database != null && idx >= 0 && idx < _modelManager.Database.Count)
        {
            var entry = _modelManager.Database[idx];
            _label.text = string.IsNullOrEmpty(entry.DisplayName) ? entry.Prefab?.name ?? "" : entry.DisplayName;
        }
    }

    private void EnsureCanvasGroup(GameObject item)
    {
        if (item == null)
            return;

        if (item.GetComponent<CanvasGroup>() == null)
            item.AddComponent<CanvasGroup>();
    }
}
