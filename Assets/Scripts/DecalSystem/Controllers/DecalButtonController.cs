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

            // Сохраняем пропорции изображения
            float aspectRatio = (float)Decal.GetTexture().width / Decal.GetTexture().height;
            var rectTransform = _previewImage.GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                // Подгоняем под размер контейнера с сохранением пропорций
                FitImageToContainer(rectTransform, aspectRatio);
            }
        }
    }

    private void FitImageToContainer(RectTransform imageRect, float aspectRatio)
    {
        // Получаем размер родительского контейнера
        var parent = imageRect.parent as RectTransform;
        if (parent == null) return;

        float parentWidth = parent.rect.width;
        float parentHeight = parent.rect.height;
        float parentAspect = parentWidth / parentHeight;

        if (aspectRatio > parentAspect)
        {
            // Подгоняем по ширине
            imageRect.sizeDelta = new Vector2(parentWidth, parentWidth / aspectRatio);
        }
        else
        {
            // Подгоняем по высоте
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

    // Обновить превью (если текстура изменилась)
    public void RefreshPreview()
    {
        UpdatePreview();
    }

    // Обновить индекс (если порядок изменился)
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