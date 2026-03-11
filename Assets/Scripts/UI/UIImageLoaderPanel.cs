using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

public class UIImageLoaderPanel : UIPanelBase
{
    [Header("UI Elements")]
    [SerializeField] private RawImage _previewImage;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private GameObject _loadingIndicator;

    [Header("Buttons")]
    [SerializeField] private Button _galleryButton;

    public event Action OnGalleryClicked;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (_galleryButton != null)
            _galleryButton.onClick.AddListener(() => OnGalleryClicked?.Invoke());
    }

    public void SetPreviewTexture(Texture2D texture)
    {
        if (_previewImage != null)
        {
            _previewImage.texture = texture;
            _previewImage.gameObject.SetActive(true);
        }
    }

    public void SetStatus(string message, bool isError = false)
    {
        if (_statusText != null)
        {
            _statusText.text = message;
            _statusText.color = isError ? Color.red : Color.white;
        }
    }

    public void SetLoading(bool isLoading)
    {
        if (_loadingIndicator != null)
            _loadingIndicator.SetActive(isLoading);

        SetButtonsInteractable(!isLoading);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_galleryButton != null) _galleryButton.interactable = interactable;
    }

    public void ClearPreview()
    {
        if (_previewImage != null)
        {
            _previewImage.texture = null;
            _previewImage.gameObject.SetActive(false);
        }
    }
}