using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EscapeMenuController : MonoBehaviour
{
    private static EscapeMenuController _instance;

    private GameObject _overlayRoot;
    private Button _continueButton;
    private Button _quitButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Resources.FindObjectsOfTypeAll<EscapeMenuController>().Length > 0)
            return;

        var go = new GameObject(nameof(EscapeMenuController));
        DontDestroyOnLoad(go);
        go.AddComponent<EscapeMenuController>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureEventSystem();
        BuildUi();
        SetMenuVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetMenuVisible(!_overlayRoot.activeSelf);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject(
            "EscapeMenuCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10_000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        _overlayRoot = new GameObject("EscapeMenuOverlay", typeof(RectTransform), typeof(Image));
        _overlayRoot.transform.SetParent(canvasObject.transform, false);

        var overlayRect = (RectTransform)_overlayRoot.transform;
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayImage = _overlayRoot.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.7f);

        var panel = new GameObject(
            "EscapeMenuPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        panel.transform.SetParent(_overlayRoot.transform, false);

        var panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(420f, 0f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.12f, 0.12f, 0.12f, 0.96f);

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 32, 32);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateLabel(panel.transform, "Пауза", 34, FontStyle.Bold);
        CreateLabel(panel.transform, "Нажмите Esc, чтобы продолжить", 20, FontStyle.Normal);

        _continueButton = CreateButton(panel.transform, "Продолжить", new Color(0.2f, 0.58f, 0.94f, 1f));
        _continueButton.onClick.AddListener(() => SetMenuVisible(false));

        _quitButton = CreateButton(panel.transform, "Выйти", new Color(0.82f, 0.26f, 0.26f, 1f));
        _quitButton.onClick.AddListener(QuitApplication);
    }

    private Text CreateLabel(Transform parent, string text, int fontSize, FontStyle fontStyle)
    {
        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        labelObject.transform.SetParent(parent, false);

        var layoutElement = labelObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = fontSize + 12f;

        var label = labelObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;

        return label;
    }

    private Button CreateButton(Transform parent, string text, Color backgroundColor)
    {
        var buttonObject = new GameObject(
            text + "Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        var layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = 64f;
        layoutElement.preferredHeight = 64f;

        var image = buttonObject.GetComponent<Image>();
        image.color = backgroundColor;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = backgroundColor * 1.1f;
        colors.pressedColor = backgroundColor * 0.9f;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0.5f);
        button.colors = colors;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var label = textObject.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 24;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;

        return button;
    }

    private void SetMenuVisible(bool visible)
    {
        if (_overlayRoot == null)
            return;

        _overlayRoot.SetActive(visible);

        if (visible)
            _continueButton?.Select();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
