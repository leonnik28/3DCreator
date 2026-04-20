using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using Fotocentr.Core;

#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

/// <summary>
/// Съёмка сцены: скриншот и видео.
/// В редакторе используется Unity Recorder (MP4), а в Play/билде — ffmpeg (MP4) при наличии ffmpeg.exe.
/// Если ffmpeg недоступен, используется fallback в PNG-последовательность.
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
    [SerializeField] private float _recorderFrameRate = 60f;
    [Tooltip("FPS для runtime-записи (последовательность PNG в папку).")]
    [SerializeField] private float _runtimeCaptureFrameRate = 60f;
    [Tooltip("Имя/путь к ffmpeg. Для билда можно положить ffmpeg.exe рядом с .exe и указать только имя.")]
    [SerializeField] private string _ffmpegExecutable = "ffmpeg.exe";
    [Tooltip("Битрейт MP4 в kbps для runtime-записи через ffmpeg.")]
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

    public bool IsRecording => _isRecording;

    private void Awake()
    {
        if (_captureCamera == null)
            _captureCamera = Camera.main;
    }

    public void TakeScreenshot()
    {
        StartCoroutine(CaptureScreenshotCoroutine(saveToDisk: true, onCaptured: null));
    }

    public void CaptureScreenshotBytes(System.Action<byte[]> onCaptured)
    {
        StartCoroutine(CaptureScreenshotCoroutine(saveToDisk: false, onCaptured));
    }

    private IEnumerator CaptureScreenshotCoroutine(bool saveToDisk, System.Action<byte[]> onCaptured)
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

        try
        {
            if (saveToDisk)
            {
                SaveScreenshot(tex);
            }
            else
            {
                // Encode PNG in memory to send it to an AI endpoint.
                var bytes = tex.EncodeToPNG();
                onCaptured?.Invoke(bytes);
                Destroy(tex);
            }
        }
        catch
        {
            // Ensure we never leave RenderTexture/Texture objects around.
            Destroy(tex);
            throw;
        }
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

        if (TryStartRecorderInEditor())
            return;
#endif
        StartRuntimeFrameCapture();
    }

    public void StopVideoRecording()
    {
        if (!_isRecording)
            return;

#if UNITY_EDITOR
        if (_recorderController != null)
        {
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
            return;
        }
#endif
        StopRuntimeFrameCapture();
    }

#if UNITY_EDITOR
    private bool TryStartRecorderInEditor()
    {
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
            return false;
        }

        if (!_recorderController.StartRecording())
        {
            Debug.LogError("Unity Recorder StartRecording вернул false (подробности в Console).");
            TearDownRecorder();
            return false;
        }

        _isRecording = true;
        return true;
    }
#endif

    private void StartRuntimeFrameCapture()
    {
        var recordingsDir = Path.Combine(Application.persistentDataPath, "Recordings");
        Directory.CreateDirectory(recordingsDir);
        var stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _runtimeOutputMp4Path = Path.Combine(recordingsDir, $"capture_{stamp}.mp4");

        _runtimeWritesMp4 = TryStartFfmpegRuntimeCapture(_runtimeOutputMp4Path);
        if (!_runtimeWritesMp4)
        {
            _runtimeFramesDirectory = Path.Combine(recordingsDir, $"capture_{stamp}_frames");
            Directory.CreateDirectory(_runtimeFramesDirectory);
        }
        else
        {
            _runtimeFramesDirectory = null;
        }

        _runtimeFrameIndex = 0;
        _isRecording = true;
        _runtimeCaptureCoroutine = StartCoroutine(RuntimeCaptureCoroutine());

        if (_runtimeWritesMp4)
            UnityEngine.Debug.Log($"Runtime запись MP4 запущена: {_runtimeOutputMp4Path}");
        else
            UnityEngine.Debug.Log($"Runtime запись запущена в PNG fallback. Кадры: {_runtimeFramesDirectory}");
    }

    private void StopRuntimeFrameCapture()
    {
        if (_runtimeCaptureCoroutine != null)
        {
            StopCoroutine(_runtimeCaptureCoroutine);
            _runtimeCaptureCoroutine = null;
        }

        StopFfmpegRuntimeCapture();
        _isRecording = false;

        if (_runtimeWritesMp4)
        {
            if (File.Exists(_runtimeOutputMp4Path))
                UnityEngine.Debug.Log($"Runtime MP4 сохранен: {_runtimeOutputMp4Path}");
            else
                UnityEngine.Debug.LogWarning($"Runtime MP4 не найден по пути: {_runtimeOutputMp4Path}");
        }
        else
        {
            WriteRuntimeCaptureInfo();
            UnityEngine.Debug.Log($"Runtime запись остановлена. Сохранено кадров: {_runtimeFrameIndex}. Папка: {_runtimeFramesDirectory}");
        }
    }

    private IEnumerator RuntimeCaptureCoroutine()
    {
        float fps = Mathf.Max(1f, _runtimeCaptureFrameRate);
        var wait = new WaitForSeconds(1f / fps);

        while (_isRecording)
        {
            yield return new WaitForEndOfFrame();
            if (_runtimeWritesMp4)
                CaptureRuntimeFrameToFfmpeg();
            else
                CaptureRuntimeFrameToPng();
            yield return wait;
        }
    }

    private void CaptureRuntimeFrameToPng()
    {
        if (_captureCamera == null)
            return;

        var rt = new RenderTexture(_width, _height, 24);
        var prevTarget = _captureCamera.targetTexture;
        _captureCamera.targetTexture = rt;
        _captureCamera.Render();
        _captureCamera.targetTexture = prevTarget;

        int outWidth = _width;
        int outHeight = _height;

        var tex = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        var filePath = Path.Combine(_runtimeFramesDirectory, $"frame_{_runtimeFrameIndex:D06}.png");
        File.WriteAllBytes(filePath, tex.EncodeToPNG());
        _runtimeFrameIndex++;

        Destroy(rt);
        Destroy(tex);
    }

    private void CaptureRuntimeFrameToFfmpeg()
    {
        if (_captureCamera == null || _ffmpegInputStream == null)
            return;

        var rt = new RenderTexture(_width, _height, 24);
        var prevTarget = _captureCamera.targetTexture;
        _captureCamera.targetTexture = rt;
        _captureCamera.Render();
        _captureCamera.targetTexture = prevTarget;

        int outWidth = _width;
        int outHeight = _height;

        var tex = new Texture2D(outWidth, outHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        var data = tex.GetRawTextureData();
        _ffmpegInputStream.Write(data, 0, data.Length);
        _runtimeFrameIndex++;

        Destroy(rt);
        Destroy(tex);
    }

    private bool TryStartFfmpegRuntimeCapture(string outputPath)
    {
        int outWidth = _width;
        int outHeight = _height;
        int bitrateKbps = Mathf.Max(500, _runtimeVideoBitrateKbps);
        float fps = Mathf.Max(1f, _runtimeCaptureFrameRate);

        string ffmpegPath = ResolveFfmpegPath();
        if (string.IsNullOrEmpty(ffmpegPath) || !File.Exists(ffmpegPath))
        {
            UnityEngine.Debug.LogWarning("ffmpeg.exe не найден, включается PNG fallback для runtime-записи.");
            return false;
        }

        string args =
            $"-y -f rawvideo -pix_fmt rgb24 -s {outWidth}x{outHeight} -r {fps.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
            "-i - -an -c:v libx264 -preset veryfast -pix_fmt yuv420p -vf vflip " +
            $"-b:v {bitrateKbps}k \"{outputPath}\"";

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true
        };

        try
        {
            _ffmpegProcess = new System.Diagnostics.Process { StartInfo = psi };
            _ffmpegProcess.Start();
            _ffmpegInputStream = _ffmpegProcess.StandardInput.BaseStream;
            return true;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogWarning($"Не удалось запустить ffmpeg ({ffmpegPath}): {e.Message}. Используется PNG fallback.");
            _ffmpegProcess = null;
            _ffmpegInputStream = null;
            return false;
        }
    }

    private void StopFfmpegRuntimeCapture()
    {
        if (_ffmpegInputStream != null)
        {
            try
            {
                _ffmpegInputStream.Flush();
                _ffmpegInputStream.Close();
            }
            catch { /* ignored */ }
            _ffmpegInputStream = null;
        }

        if (_ffmpegProcess != null)
        {
            try
            {
                if (!_ffmpegProcess.HasExited)
                {
                    _ffmpegProcess.WaitForExit(5000);
                    if (!_ffmpegProcess.HasExited)
                        _ffmpegProcess.Kill();
                }
            }
            catch { /* ignored */ }
            finally
            {
                _ffmpegProcess.Dispose();
                _ffmpegProcess = null;
            }
        }
    }

    private string ResolveFfmpegPath()
    {
        if (string.IsNullOrWhiteSpace(_ffmpegExecutable))
            return null;

        if (Path.IsPathRooted(_ffmpegExecutable))
            return _ffmpegExecutable;

        string appDir = Path.GetDirectoryName(Application.dataPath);
        string nearExecutable = Path.Combine(appDir, _ffmpegExecutable);
        if (File.Exists(nearExecutable))
            return nearExecutable;

        return _ffmpegExecutable;
    }

    private void WriteRuntimeCaptureInfo()
    {
        if (string.IsNullOrEmpty(_runtimeFramesDirectory))
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Runtime capture export");
        sb.AppendLine($"created_utc={System.DateTime.UtcNow:O}");
        sb.AppendLine($"fps={Mathf.Max(1f, _runtimeCaptureFrameRate):F3}");
        sb.AppendLine($"frames={_runtimeFrameIndex}");
        sb.AppendLine($"size={_width}x{_height}");
        sb.AppendLine();
        sb.AppendLine("ffmpeg example:");
        sb.AppendLine("ffmpeg -framerate 30 -i frame_%06d.png -c:v libx264 -pix_fmt yuv420p output.mp4");

        File.WriteAllText(Path.Combine(_runtimeFramesDirectory, "_capture_info.txt"), sb.ToString());
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

    private void OnDisable()
    {
        if (_isRecording && _runtimeCaptureCoroutine != null)
            StopRuntimeFrameCapture();
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

}
