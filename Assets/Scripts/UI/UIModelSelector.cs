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
