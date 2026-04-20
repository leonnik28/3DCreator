using System;
using System.Collections;
using System.IO;
using Fotocentr.AI;
using Fotocentr.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Fotocentr.AI
{
    /// <summary>
    /// Панель: ввод задания + скриншот сцены -> запрос в AI -> вывод ответа (с кнопкой Copy).
    /// </summary>
    public class AIVisionPromptPanel : UIPanelBase
    {
        [Header("UI")]
        [SerializeField] private TMP_InputField _promptInput;
        [SerializeField] private TMP_InputField _standardPromptInput;
        [SerializeField] private TMP_Text _outputText;
        [Tooltip("Опционально: viewport для скролла ответа. Если не задан — используется родитель OutputText.")]
        [SerializeField] private RectTransform _outputViewport;
        [Tooltip("Опционально: ScrollRect ответа. Если не задан — создаётся/ищется на viewport.")]
        [SerializeField] private ScrollRect _outputScrollRect;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Button _sendButton;
        [SerializeField] private Button _copyButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Toggle _includeScreenshotToggle;

        [Header("Dependencies")]
        [Tooltip("Источник скриншота. Можно назначить в инспекторе. Если пусто — найдётся SceneCaptureService в сцене.")]
        [SerializeField] private MonoBehaviour _captureServiceAsMono;

        [Header("AI Settings")]
        [SerializeField] private AIVisionApiKeyProvider _credentialsProvider;

        [Header("Request")]
        [Tooltip("Если включено — передаём в AI скриншот сцены. Если выключено — отправляем только текст.")]
        [SerializeField] private bool _includeScreenshot = true;

        [Header("Rate Limit Retry (429)")]
        [SerializeField] private bool _autoRetryOnRateLimit = true;
        [SerializeField] private int _rateLimitMaxRetries = 2;
        [SerializeField] private float _rateLimitBaseDelaySeconds = 2f;

        private ISceneCapture _sceneCapture;
        private OpenAIVisionClient _client;
        private bool _isBusy;
        public event Action OnCloseRequested;

        [Serializable]
        private class PersistedSettings
        {
            public string standardPrompt = "";
        }

        private const string SettingsFileName = "fotocentr_ai_settings.json";
        private PersistedSettings _persistedSettings = new PersistedSettings();

        private void Awake()
        {
            _client = new OpenAIVisionClient(this);

            if (_captureServiceAsMono != null)
                _sceneCapture = _captureServiceAsMono as ISceneCapture;
        }

        private void Start()
        {
            if (_sceneCapture == null)
                _sceneCapture = FindObjectOfType<SceneCaptureService>() as ISceneCapture;

            if (_credentialsProvider == null)
                _credentialsProvider = FindObjectOfType<AIVisionApiKeyProvider>();

            LoadSettings();
            SetupIncludeScreenshotToggle();
            SetupUi();
            SetBusy(false);
        }

        private void LoadSettings()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, SettingsFileName);
                if (!File.Exists(path))
                    return;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return;

                var loaded = JsonUtility.FromJson<PersistedSettings>(json);
                if (loaded != null)
                    _persistedSettings = loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AI] Failed to load settings: " + e.Message);
            }

            if (_standardPromptInput != null)
                _standardPromptInput.text = _persistedSettings.standardPrompt ?? "";
        }

        private void SaveSettingsFromUI()
        {
            try
            {
                if (_standardPromptInput != null)
                    _persistedSettings.standardPrompt = _standardPromptInput.text ?? "";

                string path = Path.Combine(Application.persistentDataPath, SettingsFileName);
                string json = JsonUtility.ToJson(_persistedSettings, true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AI] Failed to save settings: " + e.Message);
            }
        }

        private void SetupIncludeScreenshotToggle()
        {
            if (_includeScreenshotToggle == null)
                return;

            // Если toggle подключен, он должен управлять отправкой.
            _includeScreenshot = _includeScreenshotToggle.isOn;
            _includeScreenshotToggle.onValueChanged.AddListener(OnIncludeScreenshotToggled);
        }

        private void OnIncludeScreenshotToggled(bool value)
        {
            _includeScreenshot = value;
            Debug.Log("[AI] IncludeScreenshotToggle changed. includeScreenshot=" + _includeScreenshot);
        }

        private void SetupUi()
        {
            if (_promptInput != null)
                _promptInput.onSubmit.AddListener(_ => OnSendClicked());

            if (_standardPromptInput != null)
                _standardPromptInput.onEndEdit.AddListener(_ => SaveSettingsFromUI());

            if (_sendButton != null)
                _sendButton.onClick.AddListener(OnSendClicked);

            if (_copyButton != null)
                _copyButton.onClick.AddListener(OnCopyClicked);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(OnCloseClicked);

            if (_statusText != null && string.IsNullOrWhiteSpace(_statusText.text))
                _statusText.text = "";

            EnsureOutputScrollSetup();
        }

        private void EnsureOutputScrollSetup()
        {
            if (_outputText == null)
                return;

            RectTransform textRect = _outputText.rectTransform;
            RectTransform viewport = _outputViewport;
            if (viewport == null)
                viewport = textRect.parent as RectTransform;
            if (viewport == null)
                return;

            if (_outputScrollRect == null)
                _outputScrollRect = viewport.GetComponent<ScrollRect>();
            if (_outputScrollRect == null)
                _outputScrollRect = viewport.gameObject.AddComponent<ScrollRect>();

            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage == null)
            {
                viewportImage = viewport.gameObject.AddComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.002f);
            }

            Mask mask = viewport.GetComponent<Mask>();
            if (mask == null)
                mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            ContentSizeFitter fitter = textRect.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = textRect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            textRect.anchorMin = new Vector2(0f, 1f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(0f, textRect.sizeDelta.y);

            _outputText.enableWordWrapping = true;
            _outputText.overflowMode = TextOverflowModes.Overflow;

            _outputScrollRect.viewport = viewport;
            _outputScrollRect.content = textRect;
            _outputScrollRect.horizontal = false;
            _outputScrollRect.vertical = true;
            _outputScrollRect.movementType = ScrollRect.MovementType.Clamped;
            _outputScrollRect.scrollSensitivity = 24f;

            EventTrigger trigger = viewport.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = viewport.gameObject.AddComponent<EventTrigger>();
        }

        private void SetOutputText(string value)
        {
            if (_outputText == null)
                return;

            _outputText.text = value ?? string.Empty;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_outputText.rectTransform);

            if (_outputScrollRect != null)
                _outputScrollRect.verticalNormalizedPosition = 1f;
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;

            if (_sendButton != null)
                _sendButton.interactable = !busy;

            if (_copyButton != null)
                _copyButton.interactable = !busy;

            if (_includeScreenshotToggle != null)
                _includeScreenshotToggle.interactable = !busy;
        }

        private void SetStatus(string message, bool isError = false)
        {
            if (_statusText == null) return;
            _statusText.text = message;
            _statusText.color = isError ? Color.red : Color.white;
        }

        private void LogDev(string message, bool isError)
        {
            if (isError)
                Debug.LogError(message);
            else
                Debug.Log(message);
        }

        private void OnSendClicked()
        {
            if (_isBusy) return;

            if (_promptInput == null)
            {
                SetStatus("Prompt input field is not assigned.", true);
                LogDev("Prompt input field is null.", true);
                return;
            }

            string prompt = _promptInput.text?.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                SetStatus("Enter a prompt.", true);
                LogDev("Prompt is empty.", true);
                return;
            }

            if (_credentialsProvider == null)
            {
                SetStatus("AIVisionApiKeyProvider not found in scene.", true);
                LogDev("AIVisionApiKeyProvider reference is missing.", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(_credentialsProvider.ApiKey))
            {
                SetStatus("API key is empty in AIVisionApiKeyProvider.", true);
                LogDev("API key is empty in AIVisionApiKeyProvider.", true);
                return;
            }

            if (_client == null)
            {
                SetStatus("AI client is not ready.", true);
                LogDev("OpenAIVisionClient is null.", true);
                return;
            }

            SetBusy(true);
            SetOutputText(string.Empty);

            Debug.Log("[AI] Send clicked. includeScreenshot=" + _includeScreenshot + ", endpoint=" + _credentialsProvider.EndpointUrl + ", model=" + _credentialsProvider.Model);

            // Всегда добавляем стандартный префикс (который пользователь может менять),
            // а также сохраняем его в JSON между сессиями.
            SaveSettingsFromUI();
            string standardPrompt = _standardPromptInput != null
                ? (_standardPromptInput.text ?? "").Trim()
                : (_persistedSettings.standardPrompt ?? "").Trim();

            string promptToSend = string.IsNullOrWhiteSpace(standardPrompt)
                ? prompt
                : (standardPrompt + "\n\n" + prompt);

            Debug.Log(
                "[AI] Request prepared. includeScreenshot=" + _includeScreenshot +
                ", endpoint=" + _credentialsProvider.EndpointUrl +
                ", model=" + _credentialsProvider.Model);
            Debug.Log(
                "[AI] Standard prompt:\n" + TrimForLog(standardPrompt, 1500) +
                "\n[AI] User prompt:\n" + TrimForLog(prompt, 1500) +
                "\n[AI] promptToSend (final):\n" + TrimForLog(promptToSend, 2500)
            );

            if (_includeScreenshot)
            {
                if (_sceneCapture == null)
                {
                    SetStatus("SceneCaptureService not found.", true);
                    SetBusy(false);
                    LogDev("SceneCaptureService reference is null.", true);
                    return;
                }

                SetStatus("Capturing screenshot...", false);
                LogDev("Capturing screenshot...", false);
                _sceneCapture.CaptureScreenshotBytes(bytes =>
                {
                    if (_isBusy == false)
                        return;

                    Debug.Log("[AI] Screenshot captured. pngBytesLength=" + (bytes != null ? bytes.Length : 0));
                    string savedPath = TrySaveScreenshotDebug(bytes);
                    if (!string.IsNullOrWhiteSpace(savedPath))
                        Debug.Log("[AI] Screenshot saved for debug: " + savedPath);

                    SendImageAndPromptWithRetries(bytes, promptToSend, _credentialsProvider.Model, 0);
                });
            }
            else
            {
                SendPromptWithRetries(promptToSend, _credentialsProvider.Model, 0);
            }
        }

        private bool IsRateLimitError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return false;
            return error.IndexOf("429", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   error.IndexOf("rate-limited", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SendImageAndPromptWithRetries(byte[] screenshotBytes, string promptToSend, string model, int attempt)
        {
            if (screenshotBytes == null || screenshotBytes.Length == 0)
            {
                SetStatus("Screenshot bytes are empty.", true);
                if (_outputText != null) _outputText.text = "";
                SetBusy(false);
                LogDev("Screenshot bytes are empty.", true);
                return;
            }

            SetStatus("Sending to AI... (attempt " + (attempt + 1) + ")", false);
            LogDev("SendImageAndPrompt attempt=" + attempt + ", model=" + model, false);

            _client.SendImageAndPrompt(
                screenshotBytes,
                promptToSend,
                _credentialsProvider.ApiKey,
                _credentialsProvider.EndpointUrl,
                model,
                _credentialsProvider.SystemPrompt,
                onSuccess: response =>
                {
                    SetStatus("Done.", false);
                    SetOutputText(response);
                    SetBusy(false);
                    LogDev("Request succeeded and response parsed.", false);
                },
                onError: error =>
                {
                    LogDev("SendImageAndPrompt failed: " + error, true);

                    if (TryAutoRetryOnLocationError(screenshotBytes, promptToSend, error))
                        return;

                    if (_autoRetryOnRateLimit && IsRateLimitError(error) && attempt < _rateLimitMaxRetries)
                    {
                        int nextAttempt = attempt + 1;
                        float delay = _rateLimitBaseDelaySeconds * nextAttempt;

                        string nextModel = model;
                        // При rate-limit попробуем уйти на fallback-модель (если она отличается),
                        // чтобы не попадать в тот же upstream лимит.
                        if (attempt == 0 &&
                            !string.IsNullOrWhiteSpace(_credentialsProvider.FallbackModel) &&
                            !string.Equals(_credentialsProvider.FallbackModel, model, StringComparison.OrdinalIgnoreCase))
                        {
                            nextModel = _credentialsProvider.FallbackModel;
                        }

                        SetStatus("Rate limited. Retrying in " + delay.ToString("F1") + "s (" + nextAttempt + "/" + (_rateLimitMaxRetries + 1) + ")...", false);
                        StartCoroutine(RetryImageCoroutine(screenshotBytes, promptToSend, nextModel, nextAttempt, delay));
                        return;
                    }

                    SetStatus(error, true);
                    SetOutputText(string.Empty);
                    SetBusy(false);
                }
            );
        }

        private IEnumerator RetryImageCoroutine(byte[] screenshotBytes, string promptToSend, string model, int nextAttempt, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            if (_isBusy == false)
                yield break;
            SendImageAndPromptWithRetries(screenshotBytes, promptToSend, model, nextAttempt);
        }

        private void SendPromptWithRetries(string promptToSend, string model, int attempt)
        {
            SetStatus("Sending to AI... (attempt " + (attempt + 1) + ")", false);
            LogDev("SendPrompt attempt=" + attempt + ", model=" + model, false);

            _client.SendPrompt(
                promptToSend,
                _credentialsProvider.ApiKey,
                _credentialsProvider.EndpointUrl,
                model,
                _credentialsProvider.SystemPrompt,
                onSuccess: response =>
                {
                    SetStatus("Done.", false);
                    SetOutputText(response);
                    SetBusy(false);
                    LogDev("Request succeeded and response parsed.", false);
                },
                onError: error =>
                {
                    LogDev("SendPrompt failed: " + error, true);

                    if (TryAutoRetryOnLocationErrorText(promptToSend, error))
                        return;

                    if (_autoRetryOnRateLimit && IsRateLimitError(error) && attempt < _rateLimitMaxRetries)
                    {
                        int nextAttempt = attempt + 1;
                        float delay = _rateLimitBaseDelaySeconds * nextAttempt;

                        string nextModel = model;
                        if (attempt == 0 &&
                            !string.IsNullOrWhiteSpace(_credentialsProvider.FallbackModel) &&
                            !string.Equals(_credentialsProvider.FallbackModel, model, StringComparison.OrdinalIgnoreCase))
                        {
                            nextModel = _credentialsProvider.FallbackModel;
                        }

                        SetStatus("Rate limited. Retrying in " + delay.ToString("F1") + "s (" + nextAttempt + "/" + (_rateLimitMaxRetries + 1) + ")...", false);
                        StartCoroutine(RetryPromptCoroutine(promptToSend, nextModel, nextAttempt, delay));
                        return;
                    }

                    SetStatus(error, true);
                    SetOutputText(string.Empty);
                    SetBusy(false);
                }
            );
        }

        private IEnumerator RetryPromptCoroutine(string promptToSend, string model, int nextAttempt, float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);
            if (_isBusy == false)
                yield break;
            SendPromptWithRetries(promptToSend, model, nextAttempt);
        }

        private string TrySaveScreenshotDebug(byte[] pngBytes)
        {
            try
            {
                if (pngBytes == null || pngBytes.Length == 0)
                    return null;

                // Не спамим диском в релизе.
                if (!Debug.isDebugBuild)
                    return null;

                string dir = Path.Combine(Application.persistentDataPath, "ai_debug_screens");
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, "ai_screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                File.WriteAllBytes(path, pngBytes);
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[AI] Failed to save screenshot debug file: " + e.Message);
                return null;
            }
        }

        private static string TrimForLog(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= maxLen) return s;
            return s.Substring(0, maxLen) + "...(truncated)";
        }

        private bool TryAutoRetryOnLocationError(byte[] screenshotBytes, string promptToSend, string error)
        {
            if (_credentialsProvider == null) return false;
            if (!_credentialsProvider.AutoRetryOnLocationError) return false;
            if (string.IsNullOrWhiteSpace(error)) return false;

            if (error.IndexOf("User location is not supported", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (string.IsNullOrWhiteSpace(_credentialsProvider.FallbackModel) ||
                string.Equals(_credentialsProvider.FallbackModel, _credentialsProvider.Model, StringComparison.OrdinalIgnoreCase))
                return false;

            Debug.LogWarning("[AI] Location blocked. Retrying with fallbackModel=" + _credentialsProvider.FallbackModel);

            SetStatus("Retrying with fallback model...", false);
            SetBusy(true);

            _client.SendImageAndPrompt(
                screenshotBytes,
                promptToSend,
                _credentialsProvider.ApiKey,
                _credentialsProvider.EndpointUrl,
                _credentialsProvider.FallbackModel,
                _credentialsProvider.SystemPrompt,
                onSuccess: response =>
                {
                    SetStatus("Done.", false);
                    SetOutputText(response);
                    SetBusy(false);
                    LogDev("Retry succeeded.", false);
                },
                onError: retryError =>
                {
                    SetStatus(retryError, true);
                    SetOutputText(string.Empty);
                    SetBusy(false);
                    LogDev(retryError, true);
                }
            );
            return true;
        }

        private bool TryAutoRetryOnLocationErrorText(string promptToSend, string error)
        {
            if (_credentialsProvider == null) return false;
            if (!_credentialsProvider.AutoRetryOnLocationError) return false;
            if (string.IsNullOrWhiteSpace(error)) return false;

            if (error.IndexOf("User location is not supported", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            if (string.IsNullOrWhiteSpace(_credentialsProvider.FallbackModel) ||
                string.Equals(_credentialsProvider.FallbackModel, _credentialsProvider.Model, StringComparison.OrdinalIgnoreCase))
                return false;

            Debug.LogWarning("[AI] Location blocked. Retrying text-only with fallbackModel=" + _credentialsProvider.FallbackModel);

            SetStatus("Retrying with fallback model...", false);
            SetBusy(true);

            _client.SendPrompt(
                promptToSend,
                _credentialsProvider.ApiKey,
                _credentialsProvider.EndpointUrl,
                _credentialsProvider.FallbackModel,
                _credentialsProvider.SystemPrompt,
                onSuccess: response =>
                {
                    SetStatus("Done.", false);
                    SetOutputText(response);
                    SetBusy(false);
                    LogDev("Retry succeeded.", false);
                },
                onError: retryError =>
                {
                    SetStatus(retryError, true);
                    SetOutputText(string.Empty);
                    SetBusy(false);
                    LogDev(retryError, true);
                }
            );
            return true;
        }

        private void OnCopyClicked()
        {
            if (_outputText == null) return;
            if (string.IsNullOrWhiteSpace(_outputText.text)) return;
            GUIUtility.systemCopyBuffer = _outputText.text;
            SetStatus("Copied to clipboard.", false);
        }

        private void OnCloseClicked()
        {
            if (_isBusy)
                return;

            OnCloseRequested?.Invoke();
        }
    }
}

