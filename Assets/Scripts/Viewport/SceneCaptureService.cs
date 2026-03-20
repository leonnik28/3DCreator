using UnityEngine;
using System.Collections;
using System.IO;
using Fotocentr.Core;

#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// Съёмка сцены: скриншот (UI на кадре скрывается) и видео (UI остаётся для управления; в ролик не попадает при CaptureUI = false).
/// Видео в редакторе — Unity Recorder (MP4). В билде запись недоступна.
/// </summary>
public class SceneCaptureService : MonoBehaviour, ISceneCapture
{
    [Header("References")]
    [SerializeField] private Camera _captureCamera;
    [Tooltip("Скрываются только на момент скриншота. При записи видео UI не трогаем, чтобы можно было нажать Stop и крутить настройки.")]
    [SerializeField] private GameObject[] _uiRootsToHide;

    [Header("Settings")]
    [SerializeField] private int _width = 1920;
    [SerializeField] private int _height = 1080;
    [Tooltip("Целевой FPS записи (Unity Recorder, Constant).")]
    [SerializeField] private float _recorderFrameRate = 30f;

    private bool _isRecording;

#if UNITY_EDITOR
    private RecorderController _recorderController;
    private RecorderControllerSettings _recorderControllerSettings;
    private MovieRecorderSettings _movieRecorderSettings;
    private string _recorderOutputBasePath;
#endif

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
#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;

        TearDownRecorder();

        var recordingsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));
        Directory.CreateDirectory(recordingsDir);
        var stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _recorderOutputBasePath = Path.Combine(recordingsDir, $"capture_{stamp}").Replace('\\', '/');

        _recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        _recorderControllerSettings.SetRecordModeToManual();
        _recorderControllerSettings.FrameRate = Mathf.Max(1f, _recorderFrameRate);
        _recorderControllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
        _recorderControllerSettings.CapFrameRate = true;

        _movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        _movieRecorderSettings.name = "Fotocentr Movie";
        _movieRecorderSettings.Enabled = true;
        _movieRecorderSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        _movieRecorderSettings.OutputFile = _recorderOutputBasePath;
        _movieRecorderSettings.ImageInputSettings = BuildRecorderCameraInput();

        _recorderControllerSettings.AddRecorderSettings(_movieRecorderSettings);
        _recorderController = new RecorderController(_recorderControllerSettings);

        try
        {
            _recorderController.PrepareRecording();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unity Recorder PrepareRecording: {e.Message}");
            TearDownRecorder();
            return;
        }

        if (!_recorderController.StartRecording())
        {
            Debug.LogError("Unity Recorder StartRecording вернул false (подробности в Console).");
            TearDownRecorder();
            return;
        }

        _isRecording = true;
#else
        Debug.LogWarning("Запись видео через Unity Recorder доступна только в Unity Editor (Play Mode), не в билде.");
#endif
    }

    public void StopVideoRecording()
    {
#if UNITY_EDITOR
        if (!_isRecording || _recorderController == null)
            return;

        try
        {
            _recorderController.StopRecording();
        }
        finally
        {
            _isRecording = false;
            _recorderController = null;
            TearDownRecorder();

            var mp4 = _recorderOutputBasePath + ".mp4";
            if (File.Exists(mp4))
                Debug.Log($"Видео сохранено (Unity Recorder): {mp4}");
            else
                Debug.Log($"Запись остановлена. Ожидаемый файл: {mp4}");
        }
#endif
    }

#if UNITY_EDITOR
    private CameraInputSettings BuildRecorderCameraInput()
    {
        var input = new CameraInputSettings
        {
            OutputWidth = _width,
            OutputHeight = _height,
            CaptureUI = false,
            FlipFinalOutput = false,
        };

        if (_captureCamera != null && _captureCamera.CompareTag("MainCamera"))
            input.Source = ImageSource.MainCamera;
        else if (_captureCamera != null && !string.IsNullOrEmpty(_captureCamera.tag) && _captureCamera.tag != "Untagged")
        {
            input.Source = ImageSource.TaggedCamera;
            input.CameraTag = _captureCamera.tag;
        }
        else
        {
            input.Source = ImageSource.MainCamera;
            if (_captureCamera != null && _captureCamera.tag == "Untagged")
                Debug.LogWarning(
                    "Камера съёмки с тегом Untagged: Recorder использует Main Camera. " +
                    "Задайте уникальный тег камере, если съёмка не с главной камеры.");
        }

        return input;
    }

    private void TearDownRecorder()
    {
        if (_movieRecorderSettings != null)
        {
            DestroyImmediate(_movieRecorderSettings);
            _movieRecorderSettings = null;
        }

        if (_recorderControllerSettings != null)
        {
            DestroyImmediate(_recorderControllerSettings);
            _recorderControllerSettings = null;
        }
    }

    private void OnDestroy()
    {
        if (_isRecording && _recorderController != null)
        {
            try { _recorderController.StopRecording(); } catch { /* ignored */ }
            _isRecording = false;
            _recorderController = null;
            TearDownRecorder();
        }
    }
#endif

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
