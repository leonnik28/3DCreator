using UnityEngine;
using System.Collections;
using System.IO;
using Fotocentr.Core;

/// <summary>
/// Съёмка сцены без UI: скриншот и видео.
/// </summary>
public class SceneCaptureService : MonoBehaviour, ISceneCapture
{
    [Header("References")]
    [SerializeField] private Camera _captureCamera;
    [SerializeField] private GameObject[] _uiRootsToHide;

    [Header("Settings")]
    [SerializeField] private int _width = 1920;
    [SerializeField] private int _height = 1080;

    private bool _isRecording;

    public bool IsRecording => _isRecording;

    private void Awake()
    {
        if (_captureCamera == null)
            _captureCamera = Camera.main;
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureScreenshotCoroutine());
    }

    private IEnumerator CaptureScreenshotCoroutine()
    {
        SetUIVisible(false);
        yield return new WaitForEndOfFrame();

        var rt = new RenderTexture(_width, _height, 24);
        var prevTarget = _captureCamera.targetTexture;
        _captureCamera.targetTexture = rt;
        _captureCamera.Render();
        _captureCamera.targetTexture = prevTarget;

        var tex = new Texture2D(_width, _height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        Destroy(rt);

        SetUIVisible(true);

        SaveScreenshot(tex);
    }

    private void SaveScreenshot(Texture2D tex)
    {
#if UNITY_EDITOR
        var path = Path.Combine(Application.dataPath, "..", $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Debug.Log($"Screenshot saved: {path}");
#else
        NativeGallery.SaveImageToGallery(tex, "Fotocentr", $"screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
#endif
        Destroy(tex);
    }

    public void StartVideoRecording()
    {
        if (_isRecording) return;
        StartCoroutine(RecordVideoCoroutine());
    }

    public void StopVideoRecording()
    {
        _isRecording = false;
    }

    private IEnumerator RecordVideoCoroutine()
    {
        _isRecording = true;
        SetUIVisible(false);
        yield return RecordAsImageSequence();
        SetUIVisible(true);
        _isRecording = false;
    }

    private IEnumerator RecordAsImageSequence()
    {
        var folder = Path.Combine(Application.dataPath, "..", "Recordings");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var subFolder = Path.Combine(folder, timestamp);
        Directory.CreateDirectory(subFolder);

        var rt = new RenderTexture(_width, _height, 24);
        var tex = new Texture2D(_width, _height, TextureFormat.RGB24, false);
        int frame = 0;
        float duration = 5f;
        float elapsed = 0f;

        while (_isRecording && elapsed < duration)
        {
            _captureCamera.targetTexture = rt;
            _captureCamera.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(subFolder, $"frame_{frame:D5}.png"), tex.EncodeToPNG());
            frame++;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(tex);
        Debug.Log($"Recorded {frame} frames to {subFolder}. Use FFmpeg: ffmpeg -framerate 30 -i frame_%05d.png -c:v libx264 output.mp4");
    }

    private void SetUIVisible(bool visible)
    {
        if (_uiRootsToHide == null) return;
        foreach (var root in _uiRootsToHide)
        {
            if (root != null)
                root.SetActive(visible);
        }
    }

    public void SetCaptureCamera(Camera cam) => _captureCamera = cam;
    public void SetUIRoots(GameObject[] roots) => _uiRootsToHide = roots;
}
