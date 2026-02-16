using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System;

public class TextureLoader : MonoBehaviour
{
    [Header("UI References")]
    public RawImage previewImage;
    public Button loadButton;
    public Button loadUrlButton;
    public InputField urlInput;
    public Text statusText;
    public GameObject loadingIndicator;

    [Header("Настройки")]
    public int maxTextureSize = 2048;
    public bool preserveAspectRatio = true;

    public Texture2D lastLoadedTexture { get; private set; }
    public event Action<Texture2D> OnTextureLoaded;
    public event Action<string> OnError;

    private bool isLoading = false;

    void Start()
    {
        if (loadButton != null)
            loadButton.onClick.AddListener(OpenFilePicker);

        if (loadUrlButton != null)
            loadUrlButton.onClick.AddListener(LoadFromUrlInput);

        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }

    void OpenFilePicker()
    {
        if (isLoading) return;

#if UNITY_EDITOR
        // В редакторе Unity
        string path = UnityEditor.EditorUtility.OpenFilePanel(
            "Выберите изображение",
            "",
            "png,jpg,jpeg,bmp,gif");

        if (!string.IsNullOrEmpty(path))
        {
            StartCoroutine(LoadTextureFromFile(path));
        }
#elif UNITY_ANDROID || UNITY_IOS
        // Для мобильных устройств
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                StartCoroutine(LoadTextureFromFile(path));
            }
        }, "Выберите изображение");
        
        if (permission == NativeGallery.Permission.Denied)
        {
            ShowError("Нет разрешения на доступ к галерее");
        }
#elif UNITY_WEBGL
        // Для WebGL
        WebGLFilePicker();
#else
        // Для Windows/Mac
        System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
        dialog.Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All files (*.*)|*.*";
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            StartCoroutine(LoadTextureFromFile(dialog.FileName));
        }
#endif
    }

#if UNITY_WEBGL
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter);
    
    void WebGLFilePicker()
    {
        UploadFile(gameObject.name, "OnFileUpload", ".png,.jpg,.jpeg,.bmp");
    }
    
    public void OnFileUpload(string url)
    {
        StartCoroutine(LoadTextureFromURL(url));
    }
#endif

    void LoadFromUrlInput()
    {
        if (isLoading) return;

        if (urlInput != null && !string.IsNullOrEmpty(urlInput.text))
        {
            StartCoroutine(LoadTextureFromURL(urlInput.text));
        }
        else
        {
            ShowError("Введите URL изображения");
        }
    }

    IEnumerator LoadTextureFromFile(string path)
    {
        SetLoadingState(true, "Загрузка из файла...");

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture("file://" + path))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                ShowError("Ошибка загрузки: " + uwr.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                ProcessLoadedTexture(texture, path);
            }
        }

        SetLoadingState(false);
    }

    public IEnumerator LoadTextureFromURL(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            ShowError("URL не может быть пустым");
            yield break;
        }

        SetLoadingState(true, "Загрузка из интернета...");

        // Добавляем протокол, если его нет
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            // Устанавливаем таймаут
            uwr.timeout = 10;

            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                ShowError("Ошибка загрузки: " + uwr.error);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                ProcessLoadedTexture(texture, url);
            }
        }

        SetLoadingState(false);
    }

    void ProcessLoadedTexture(Texture2D texture, string source = "")
    {
        try
        {
            // Оптимизируем текстуру
            Texture2D optimizedTexture = OptimizeTexture(texture);

            // Сохраняем текстуру
            lastLoadedTexture = optimizedTexture;

            // Показываем превью
            if (previewImage != null)
            {
                previewImage.texture = lastLoadedTexture;
                previewImage.gameObject.SetActive(true);

                // Настраиваем RawImage для правильного отображения
                previewImage.uvRect = new Rect(0, 0, 1, 1);
                previewImage.SetNativeSize();
            }

            UpdateStatus("Изображение загружено! Кликните по модели для размещения");

            // Вызываем событие
            OnTextureLoaded?.Invoke(lastLoadedTexture);

            Debug.Log($"Текстура успешно загружена: {source}");
        }
        catch (Exception e)
        {
            ShowError("Ошибка обработки текстуры: " + e.Message);
        }
    }

    Texture2D OptimizeTexture(Texture2D original)
    {
        // Проверяем размер текстуры
        int width = original.width;
        int height = original.height;

        // Уменьшаем если слишком большая
        if (width > maxTextureSize || height > maxTextureSize)
        {
            if (preserveAspectRatio)
            {
                float ratio = (float)width / height;
                if (width > height)
                {
                    width = maxTextureSize;
                    height = Mathf.RoundToInt(width / ratio);
                }
                else
                {
                    height = maxTextureSize;
                    width = Mathf.RoundToInt(height * ratio);
                }
            }
            else
            {
                width = Mathf.Min(width, maxTextureSize);
                height = Mathf.Min(height, maxTextureSize);
            }

            // Создаем уменьшенную версию
            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            rt.filterMode = FilterMode.Trilinear;

            Graphics.Blit(original, rt);

            RenderTexture.active = rt;
            Texture2D resized = new Texture2D(width, height, TextureFormat.RGBA32, false);
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            Destroy(original);
            return resized;
        }

        // Копируем с правильным форматом
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels(original.GetPixels());
        result.Apply();

        Destroy(original);
        return result;
    }

    void SetLoadingState(bool loading, string message = "")
    {
        isLoading = loading;

        if (loadingIndicator != null)
            loadingIndicator.SetActive(loading);

        if (statusText != null && !string.IsNullOrEmpty(message))
            statusText.text = message;

        if (loadButton != null)
            loadButton.interactable = !loading;

        if (loadUrlButton != null)
            loadUrlButton.interactable = !loading;

        if (urlInput != null)
            urlInput.interactable = !loading;
    }

    void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    void ShowError(string error)
    {
        Debug.LogError(error);
        UpdateStatus("Ошибка: " + error);
        OnError?.Invoke(error);
        SetLoadingState(false);
    }

    // Очистка загруженной текстуры
    public void ClearTexture()
    {
        if (lastLoadedTexture != null)
        {
            Destroy(lastLoadedTexture);
            lastLoadedTexture = null;
        }

        if (previewImage != null)
        {
            previewImage.texture = null;
            previewImage.gameObject.SetActive(false);
        }

        UpdateStatus("Текстура очищена");
    }

    void OnDestroy()
    {
        if (lastLoadedTexture != null)
        {
            Destroy(lastLoadedTexture);
        }
    }
}