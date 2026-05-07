using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System;
using Fotocentr.Core; // Убедитесь, что это пространство имен добавлено

#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// Съёмка сцены: скриншот и видео.
/// Реализует ISceneCapture для интеграции через CompositionRoot.
/// </summary>
public class SceneCaptureService : MonoBehaviour, ISceneCapture
{
    [Header("References")]
    [SerializeField] private Camera _captureCamera;
    [SerializeField] private GameObject[] _uiRootsToHide;

    [Header("Settings")]
    [SerializeField] private int _width = 1920;
    [SerializeField] private int _height = 1080;

#if UNITY_EDITOR
    [Tooltip("Целевой FPS записи для Unity Recorder.")]
    [SerializeField] private float _recorderFrameRate = 60f;
#endif

    [Tooltip("FPS для записи через ffmpeg.")]
    [SerializeField] private float _runtimeCaptureFrameRate = 30f;

    [Tooltip("Имя исполняемого файла ffmpeg в StreamingAssets.")]
    [SerializeField] private string _ffmpegExecutable = "ffmpeg.exe";

    [SerializeField] private int _runtimeVideoBitrateKbps = 12000;

    private bool _isRecording;
    private Coroutine _runtimeCaptureCoroutine;
    private bool _runtimeWritesMp4;
    private string _runtimeFramesDirectory;
    private string _runtimeOutputMp4Path;
    private int _runtimeFrameIndex;
    private System.Diagnostics.Process _ffmpegProcess;
    private Stream _ffmpegInputStream;

#if UNITY_EDITOR
    private RecorderController _recorderController;
    private RecorderControllerSettings _recorderControllerSettings;
    private MovieRecorderSettings _movieRecorderSettings;
    private string _recorderOutputBasePath;
#endif

    // Реализация свойства из интерфейса ISceneCapture
    public bool IsRecording => _isRecording;

    private void Awake()
    {
        if (_captureCamera == null)
            _captureCamera = Camera.main;

        // Кодек H.264 требует четных размеров
        _width = (_width / 2) * 2;
        _height = (_height / 2) * 2;
    }

    private string GetRecordingsPath()
    {
        string baseDir = Path.GetDirectoryName(Application.dataPath);
        string path = Path.Combine(baseDir, "Recordings");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    // --- Методы интерфейса ISceneCapture ---

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureScreenshotCoroutine(saveToDisk: true, onCaptured: null));
    }

    public void CaptureScreenshotBytes(Action<byte[]> onCaptured)
    {
        StartCoroutine(CaptureScreenshotCoroutine(saveToDisk: false, onCaptured));
    }

    public void StartVideoRecording()
    {
        if (_isRecording) return;

#if UNITY_EDITOR
        if (TryStartRecorderInEditor()) return;
#endif
        StartRuntimeFrameCapture();
    }

    public void StopVideoRecording()
    {
        if (!_isRecording) return;
        _isRecording = false;

#if UNITY_EDITOR
        if (_recorderController != null)
        {
            _recorderController.StopRecording();
            _recorderController = null;
            return;
        }
#endif

        if (_runtimeCaptureCoroutine != null) StopCoroutine(_runtimeCaptureCoroutine);

        if (_ffmpegInputStream != null)
        {
            try { _ffmpegInputStream.Close(); } catch { }
            _ffmpegInputStream = null;
        }

        if (_ffmpegProcess != null)
        {
            if (!_ffmpegProcess.HasExited) _ffmpegProcess.WaitForExit(3000);
            _ffmpegProcess.Dispose();
            _ffmpegProcess = null;
        }

        Debug.Log($"Запись завершена. Файл: {_runtimeOutputMp4Path}");
    }

    // --- Внутренняя логика (Корутины и захват) ---

    private IEnumerator CaptureScreenshotCoroutine(bool saveToDisk, Action<byte[]> onCaptured)
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

        byte[] pngBytes = tex.EncodeToPNG();

        if (saveToDisk)
        {
            string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            string fullPath = Path.Combine(GetRecordingsPath(), fileName);
            File.WriteAllBytes(fullPath, pngBytes);
            Debug.Log($"Скриншот сохранен: {fullPath}");
        }

        onCaptured?.Invoke(pngBytes);
        Destroy(tex);
    }

    private void StartRuntimeFrameCapture()
    {
        string recordingsDir = GetRecordingsPath();
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _runtimeOutputMp4Path = Path.Combine(recordingsDir, $"capture_{stamp}.mp4");

        _runtimeWritesMp4 = TryStartFfmpegRuntimeCapture(_runtimeOutputMp4Path);

        if (!_runtimeWritesMp4)
        {
            _runtimeFramesDirectory = Path.Combine(recordingsDir, $"capture_{stamp}_frames");
            Directory.CreateDirectory(_runtimeFramesDirectory);
        }

        _runtimeFrameIndex = 0;
        _isRecording = true;
        _runtimeCaptureCoroutine = StartCoroutine(RuntimeCaptureCoroutine());
    }

    private IEnumerator RuntimeCaptureCoroutine()
    {
        float frameDelay = 1f / _runtimeCaptureFrameRate;
        var waitForEndOfFrame = new WaitForEndOfFrame();

        while (_isRecording)
        {
            yield return waitForEndOfFrame;

            if (_runtimeWritesMp4)
                CaptureRuntimeFrameToFfmpeg();
            else
                CaptureRuntimeFrameToPng();

            yield return new WaitForSeconds(frameDelay);
        }
    }

    private void CaptureRuntimeFrameToFfmpeg()
    {
        if (_captureCamera == null || _ffmpegInputStream == null) return;

        RenderTexture rt = RenderTexture.GetTemporary(_width, _height, 24);
        var prevTarget = _captureCamera.targetTexture;
        _captureCamera.targetTexture = rt;
        _captureCamera.Render();
        _captureCamera.targetTexture = prevTarget;

        Texture2D tex = new Texture2D(_width, _height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] data = tex.GetRawTextureData();

        try
        {
            _ffmpegInputStream.Write(data, 0, data.Length);
            _ffmpegInputStream.Flush();
            _runtimeFrameIndex++;
        }
        catch (Exception e)
        {
            Debug.LogError("Ошибка ffmpeg: " + e.Message);
            StopVideoRecording();
        }

        RenderTexture.ReleaseTemporary(rt);
        Destroy(tex);
    }

    private bool TryStartFfmpegRuntimeCapture(string outputPath)
    {
        string ffmpegPath = ResolveFfmpegPath();
        if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath)) return false;

        string args = $"-y -f rawvideo -pix_fmt rgb24 -s {_width}x{_height} " +
                      $"-r {_runtimeCaptureFrameRate.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
                      $"-i - -c:v libx264 -preset ultrafast -pix_fmt yuv420p -vf vflip " +
                      $"-b:v {_runtimeVideoBitrateKbps}k \"{outputPath}\"";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true
        };

        try
        {
            _ffmpegProcess = System.Diagnostics.Process.Start(psi);
            _ffmpegInputStream = _ffmpegProcess.StandardInput.BaseStream;
            return true;
        }
        catch { return false; }
    }

    private string ResolveFfmpegPath()
    {
        string streamingPath = Path.Combine(Application.streamingAssetsPath, _ffmpegExecutable);
        if (File.Exists(streamingPath)) return streamingPath;

        string rootPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), _ffmpegExecutable);
        if (File.Exists(rootPath)) return rootPath;

        return _ffmpegExecutable;
    }

    private void CaptureRuntimeFrameToPng()
    {
        // Если ffmpeg не завелся, пишем просто картинки
        StartCoroutine(CaptureScreenshotCoroutine(true, null));
    }

    private void SetUIVisible(bool visible)
    {
        if (_uiRootsToHide == null) return;
        foreach (var root in _uiRootsToHide) if (root != null) root.SetActive(visible);
    }

#if UNITY_EDITOR
    private bool TryStartRecorderInEditor()
    {
        TearDownRecorder();
        _recorderOutputBasePath = Path.Combine(GetRecordingsPath(), $"editor_{DateTime.Now:yyyyMMdd_HHmmss}");

        _recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        _recorderControllerSettings.SetRecordModeToManual();
        _recorderControllerSettings.FrameRate = _recorderFrameRate;

        _movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        _movieRecorderSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        _movieRecorderSettings.OutputFile = _recorderOutputBasePath;

        _movieRecorderSettings.ImageInputSettings = new CameraInputSettings
        {
            Source = ImageSource.MainCamera,
            OutputWidth = _width,
            OutputHeight = _height,
            CaptureUI = false
        };

        _recorderControllerSettings.AddRecorderSettings(_movieRecorderSettings);
        _recorderController = new RecorderController(_recorderControllerSettings);
        _recorderController.PrepareRecording();
        return _recorderController.StartRecording();
    }

    private void TearDownRecorder()
    {
        if (_movieRecorderSettings != null) DestroyImmediate(_movieRecorderSettings);
        if (_recorderControllerSettings != null) DestroyImmediate(_recorderControllerSettings);
    }
#endif
}