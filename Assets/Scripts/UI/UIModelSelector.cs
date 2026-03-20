using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    private readonly List<GameObject> _items = new List<GameObject>();

    private void Start()
    {
        if (_modelManager == null)
            _modelManager = FindObjectOfType<ModelManager>();

        if (_modelManager == null || _modelManager.Database == null)
        {
            gameObject.SetActive(false);
            return;
        }

        EnsureHorizontalScrollArea();

        if (_prevButton != null) _prevButton.onClick.AddListener(() => _modelManager.PreviousModel());
        if (_nextButton != null) _nextButton.onClick.AddListener(() => _modelManager.NextModel());

        BuildItems();
        _modelManager.OnModelChanged += OnModelChanged;
        UpdateSelectionVisual();
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
        if (_content.GetComponentInParent<ScrollRect>() != null)
            return;

        var panelRt = _content.parent as RectTransform;
        if (panelRt == null) return;

        if (panelRt.rect.width < 240f)
            panelRt.sizeDelta = new Vector2(Mathf.Max(panelRt.sizeDelta.x, 480f), Mathf.Max(panelRt.sizeDelta.y, 128f));

        var scrollGo = new GameObject("ModelScrollArea", typeof(RectTransform));
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.SetParent(panelRt, false);
        scrollRt.SetSiblingIndex(_content.GetSiblingIndex());
        scrollRt.anchorMin = new Vector2(0.04f, 0.14f);
        scrollRt.anchorMax = new Vector2(0.96f, 0.58f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
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

        var hlg = _content.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null)
            hlg = _content.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(4, 4, 4, 4);
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var fitter = _content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
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

            var button = go.GetComponent<Button>() ?? go.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(() => _modelManager.SetModel(index));

            var image = go.GetComponentInChildren<Image>();
            if (image != null && entry.Thumbnail != null)
                image.sprite = entry.Thumbnail;

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = string.IsNullOrEmpty(entry.DisplayName) ? entry.Prefab?.name ?? "?" : entry.DisplayName;
        }

        Canvas.ForceUpdateCanvases();
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
            var canvasGroup = item.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = i == idx ? 1f : 0.6f;
        }

        if (_label != null && _modelManager.Database != null && idx >= 0 && idx < _modelManager.Database.Count)
        {
            var entry = _modelManager.Database[idx];
            _label.text = string.IsNullOrEmpty(entry.DisplayName) ? entry.Prefab?.name ?? "" : entry.DisplayName;
        }
    }
}
