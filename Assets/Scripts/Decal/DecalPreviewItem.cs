using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DecalPreviewItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RawImage _previewImage;
    [SerializeField] private TextMeshProUGUI _indexText;
    [SerializeField] private Image _selectionBorder;
    [SerializeField] private Color _selectedColor = Color.yellow;
    [SerializeField] private Color _normalColor = Color.gray;

    public DecalController Decal { get; private set; }
    public event Action<DecalPreviewItem> OnClicked;

    private int _index;

    public void Initialize(DecalController decal, int index)
    {
        Decal = decal;
        _index = index;

        if (_previewImage != null)
            _previewImage.texture = decal.GetTexture();

        if (_indexText != null)
            _indexText.text = index.ToString();

        SetSelected(false);
    }

    public void UpdatePreview(int newIndex)
    {
        _index = newIndex;
        if (_indexText != null)
            _indexText.text = newIndex.ToString();
    }

    public void SetSelected(bool selected)
    {
        if (_selectionBorder != null)
        {
            _selectionBorder.color = selected ? _selectedColor : _normalColor;
            _selectionBorder.gameObject.SetActive(selected);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }
}