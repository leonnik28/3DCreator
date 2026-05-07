using UnityEngine;
using UnityEngine.UI;
using System;

public class ImageLoaderController : MonoBehaviour
{
    [SerializeField] private Button _galleryButton;
    [SerializeField] private GameObject _loadingIndicator;

    private TextureLoadService _textureLoadService;

    public event Action<Texture2D> OnImageLoaded;

    public void Initialize()
    {
        _textureLoadService = new TextureLoadService(this);

        if (_galleryButton != null)
        {
            _galleryButton.onClick.RemoveAllListeners();
            _galleryButton.onClick.AddListener(OnGalleryClicked);
        }
    }

    private void OnGalleryClicked()
    {
        SetLoading(true);
        _textureLoadService.LoadFromGallery(OnTextureLoaded, OnTextureLoadError);
    }

    private void OnTextureLoaded(Texture2D texture)
    {
        SetLoading(false);
        OnImageLoaded?.Invoke(texture);
    }

    private void OnTextureLoadError(string error)
    {
        SetLoading(false);

        if (string.IsNullOrEmpty(error) || error == TextureLoadService.UserCancelledError)
            return;

        Debug.LogError($"Failed to load texture: {error}");
    }

    private void SetLoading(bool isLoading)
    {
        if (_loadingIndicator != null)
            _loadingIndicator.SetActive(isLoading);
        if (_galleryButton != null)
            _galleryButton.interactable = !isLoading;
    }
}
