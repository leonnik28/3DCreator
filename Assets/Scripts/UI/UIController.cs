using UnityEngine;
using System.Collections;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private UIMainMenu _mainMenu;
    [SerializeField] private DecalEditPanel _decalEditPanel;

    [Header("Services")]
    [SerializeField] private DecalManager _decalManager;
    [SerializeField] private Camera _renderCamera;

    private void Start()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        if (_mainMenu != null)
        {
            _mainMenu.OnClearAllClicked += () => _decalManager?.ClearAllDecals();
            _mainMenu.OnSnapshotClicked += () => StartCoroutine(TakeSnapshot());
        }

        // DecalEditPanel сам обрабатывает загрузку изображений
    }

    private IEnumerator TakeSnapshot()
    {
        yield return new WaitForEndOfFrame();

        RenderTexture rt = new RenderTexture(1024, 1024, 24);
        _renderCamera.targetTexture = rt;

        Texture2D screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        _renderCamera.Render();

        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenshot.Apply();

        _renderCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        SaveScreenshot(screenshot);
    }

    private void SaveScreenshot(Texture2D screenshot)
    {
#if UNITY_EDITOR
        System.IO.File.WriteAllBytes("screenshot.png", screenshot.EncodeToPNG());
        Debug.Log("Screenshot saved: screenshot.png");
#else
        NativeGallery.SaveImageToGallery(screenshot, "Decals", 
            $"decal_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
#endif
    }
}