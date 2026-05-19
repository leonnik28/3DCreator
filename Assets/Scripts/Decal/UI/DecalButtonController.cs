using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class DecalButtonController : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RawImage _previewImage;
    [SerializeField] private TextMeshProUGUI _indexText;
    [SerializeField] private Image _selectionBorder;
    [SerializeField] private Color _selectedColor = Color.yellow;
    [SerializeField] private Color _normalColor = Color.gray;

    public DecalController Decal { get; private set; }
    public event Action<DecalButtonController> OnClicked;

    private int _index;

    public void Initialize(DecalController decal, int index)
    {
        Decal = decal ?? throw new ArgumentNullException(nameof(decal));
        _index = index;

        UpdatePreview();
        UpdateIndex();
        SetSelected(false);
    }

    private void UpdatePreview()
    {
        if (_previewImage != null && Decal.GetTexture() != null)
        {
            _previewImage.texture = Decal.GetTexture();

            float aspectRatio = (float)Decal.GetTexture().width / Decal.GetTexture().height;
            var rectTransform = _previewImage.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                FitImageToContainer(rectTransform, aspectRatio);
            }
        }
    }

    private void FitImageToContainer(RectTransform imageRect, float aspectRatio)
    {
        var parent = imageRect.parent as RectTransform;
        if (parent == null) return;

        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        float parentAspect = parentWidth / parentHeight;

        if (aspectRatio > parentAspect)
        {
            imageRect.sizeDelta = new Vector2(parentWidth, parentWidth / aspectRatio);
        }
        else
        {
            imageRect.sizeDelta = new Vector2(parentHeight * aspectRatio, parentHeight);
        }
    }

    private void UpdateIndex()
    {
        if (_indexText != null)
            _indexText.text = _index.ToString();
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

    public void RefreshPreview()
    {
        UpdatePreview();
    }

    public void UpdateIndex(int newIndex)
    {
        _index = newIndex;
        UpdateIndex();
    }

    private void OnDestroy()
    {
        OnClicked = null;
    }
}