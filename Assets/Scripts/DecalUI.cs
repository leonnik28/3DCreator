using UnityEngine;
using UnityEngine.UI;

public class DecalUI : MonoBehaviour
{
    [Header("Кнопки управления")]
    public Button addTextButton;
    public Button addImageButton;
    public Button clearAllButton;
    public Slider sizeSlider;
    public Toggle gridToggle;
    public Button snapshotButton;

    [Header("Настройки")]
    public DecalManager decalManager;
    public Camera renderCamera;

    void Start()
    {
        if (addImageButton != null)
            addImageButton.onClick.AddListener(OnAddImageClicked);

        if (clearAllButton != null)
            clearAllButton.onClick.AddListener(OnClearAllClicked);

        if (sizeSlider != null)
        {
            sizeSlider.onValueChanged.AddListener(OnSizeChanged);
            sizeSlider.gameObject.SetActive(false);
        }

        if (snapshotButton != null)
            snapshotButton.onClick.AddListener(TakeSnapshot);
    }

    void OnAddImageClicked()
    {
        // Активируем режим добавления изображения
        Debug.Log("Выберите изображение для добавления");
    }

    void OnClearAllClicked()
    {
        if (decalManager != null)
            decalManager.ClearAllDecals();
    }

    void OnSizeChanged(float value)
    {
        // Изменение размера выбранного декаля
        // Логика в DecalManager
    }

    void TakeSnapshot()
    {
        // Сохраняем 3D сцену как изображение
        StartCoroutine(CaptureScreenshot());
    }

    System.Collections.IEnumerator CaptureScreenshot()
    {
        yield return new WaitForEndOfFrame();

        RenderTexture rt = new RenderTexture(1024, 1024, 24);
        renderCamera.targetTexture = rt;

        Texture2D screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        renderCamera.Render();

        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenshot.Apply();

        renderCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // Сохраняем в галерею
#if UNITY_EDITOR
        System.IO.File.WriteAllBytes("screenshot.png", screenshot.EncodeToPNG());
#else
        NativeGallery.SaveImageToGallery(screenshot, "3DSnapshots", $"snapshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
#endif
    }
}