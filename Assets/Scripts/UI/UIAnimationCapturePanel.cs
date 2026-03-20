using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI панель: анимация модели (вкл/выкл, ось, скорость) + скриншот и запись видео.
/// </summary>
public class UIAnimationCapturePanel : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Toggle _animateToggle;
    [SerializeField] private Toggle _axisToggle;
    [SerializeField] private Slider _speedSlider;
    [SerializeField] private TextMeshProUGUI _speedLabel;
    [SerializeField] private Toggle _reverseToggle;

    [Header("Capture (Snapshot в UIDecalsActionPanel)")]
    [SerializeField] private Button _recordButton;
    [Tooltip("Необязательно: текст на кнопке Record — переключается Record / Stop при записи.")]
    [SerializeField] private TextMeshProUGUI _recordButtonLabel;

    [Header("References")]
    [SerializeField] private ModelRotator _modelRotator;
    [SerializeField] private SceneCaptureService _captureService;

    private void Start()
    {
        if (_modelRotator == null) _modelRotator = FindObjectOfType<ModelRotator>();
        if (_captureService == null) _captureService = FindObjectOfType<SceneCaptureService>();

        if (_animateToggle != null) _animateToggle.onValueChanged.AddListener(OnAnimateToggled);
        if (_axisToggle != null) _axisToggle.onValueChanged.AddListener(OnAxisToggled);
        if (_speedSlider != null)
        {
            _speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            if (_speedSlider.minValue < 10f) _speedSlider.minValue = 10f;
            if (_speedSlider.maxValue < 360f) _speedSlider.maxValue = 360f;
            if (_speedSlider.value < 60f) _speedSlider.value = 120f;
        }
        if (_reverseToggle != null) _reverseToggle.onValueChanged.AddListener(OnReverseToggled);
        if (_recordButton != null) _recordButton.onClick.AddListener(OnRecordClicked);

        if (_speedSlider != null && _speedLabel != null)
            _speedLabel.text = $"{_speedSlider.value:F0}";

        RefreshRecordButtonLabel();
    }

    private void OnAnimateToggled(bool value)
    {
        if (_modelRotator != null)
            _modelRotator.SetAnimating(value);
    }

    private void OnAxisToggled(bool value)
    {
        if (_modelRotator != null)
            _modelRotator.SetAxis(value ? ModelRotator.Axis.Vertical : ModelRotator.Axis.Horizontal);
    }

    private void OnSpeedChanged(float value)
    {
        if (_modelRotator != null)
            _modelRotator.Speed = Mathf.Max(10f, value);
        if (_speedLabel != null)
            _speedLabel.text = $"{value:F0}";
    }

    private void OnReverseToggled(bool value)
    {
        if (_modelRotator != null)
            _modelRotator.SetReverse(value);
    }

    private void OnRecordClicked()
    {
        if (_captureService == null) return;

        if (_captureService.IsRecording)
            _captureService.StopVideoRecording();
        else
            _captureService.StartVideoRecording();

        RefreshRecordButtonLabel();
    }

    private void RefreshRecordButtonLabel()
    {
        if (_recordButtonLabel == null) return;
        _recordButtonLabel.text = _captureService != null && _captureService.IsRecording ? "Stop" : "Record";
    }
}
