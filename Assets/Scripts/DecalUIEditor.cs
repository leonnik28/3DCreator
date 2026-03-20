using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class DecalUIEditor : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("UI Компоненты")]
    public RectTransform previewRect;
    public RawImage previewImage;
    public RectTransform selectionFrame;

    [Header("Элементы управления")]
    public Slider widthSlider;
    public Slider heightSlider;
    public Slider rotationSlider;
    public Toggle lockAspectToggle;
    public Button applyButton;
    public Button cancelButton;

    [Header("Настройки")]
    public float minSize = 50f;
    public float maxSize = 500f;
    public float handleSize = 20f;

    // События
    public event Action<Vector2, Vector2, float> OnTransformChanged; // позиция, размер, поворот
    public event Action OnApply;
    public event Action OnCancel;

    private bool isDragging = false;
    private bool isResizing = false;
    private Vector2 dragStartPoint;
    private Vector2 originalPosition;
    private Vector2 originalSize;
    private float originalRotation;
    private bool lockAspect = true;
    private float originalAspect;

    // Режимы изменения размера
    private enum ResizeMode
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    void Start()
    {
        SetupSliders();
        SetupSelectionHandles();
    }

    void SetupSliders()
    {
        if (widthSlider != null)
        {
            widthSlider.minValue = minSize;
            widthSlider.maxValue = maxSize;
            widthSlider.onValueChanged.AddListener(OnWidthChanged);
        }

        if (heightSlider != null)
        {
            heightSlider.minValue = minSize;
            heightSlider.maxValue = maxSize;
            heightSlider.onValueChanged.AddListener(OnHeightChanged);
        }

        if (rotationSlider != null)
        {
            rotationSlider.minValue = 0f;
            rotationSlider.maxValue = 360f;
            rotationSlider.onValueChanged.AddListener(OnRotationChanged);
        }

        if (lockAspectToggle != null)
        {
            lockAspectToggle.onValueChanged.AddListener(OnLockAspectChanged);
        }

        if (applyButton != null)
            applyButton.onClick.AddListener(() => OnApply?.Invoke());

        if (cancelButton != null)
            cancelButton.onClick.AddListener(() => OnCancel?.Invoke());
    }

    void SetupSelectionHandles()
    {
        if (selectionFrame == null) return;

        // Добавляем обработчики для углов
        CreateResizeHandle("TopLeft", new Vector2(-0.5f, 0.5f));
        CreateResizeHandle("TopRight", new Vector2(0.5f, 0.5f));
        CreateResizeHandle("BottomLeft", new Vector2(-0.5f, -0.5f));
        CreateResizeHandle("BottomRight", new Vector2(0.5f, -0.5f));
    }

    void CreateResizeHandle(string name, Vector2 anchoredPosition)
    {
        GameObject handleObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        handleObj.transform.SetParent(selectionFrame, false);

        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.anchorMin = anchoredPosition;
        handleRect.anchorMax = anchoredPosition;
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = Vector2.one * handleSize;

        Image handleImage = handleObj.GetComponent<Image>();
        handleImage.color = Color.white;
        handleImage.raycastTarget = true;

        // Добавляем обработчик перетаскивания
        EventTrigger trigger = handleObj.AddComponent<EventTrigger>();

        EventTrigger.Entry dragEntry = new EventTrigger.Entry();
        dragEntry.eventID = EventTriggerType.Drag;
        dragEntry.callback.AddListener((data) => OnResizeHandleDrag((PointerEventData)data, name));
        trigger.triggers.Add(dragEntry);

        EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
        beginDragEntry.eventID = EventTriggerType.BeginDrag;
        beginDragEntry.callback.AddListener((data) => OnBeginResizeHandle(name));
        trigger.triggers.Add(beginDragEntry);

        EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
        endDragEntry.eventID = EventTriggerType.EndDrag;
        endDragEntry.callback.AddListener((data) => OnEndResizeHandle());
        trigger.triggers.Add(endDragEntry);
    }

    public void SetTexture(Texture2D texture)
    {
        if (previewImage != null)
        {
            previewImage.texture = texture;
            previewImage.gameObject.SetActive(true);

            // Устанавливаем начальные размеры
            float aspect = (float)texture.width / texture.height;
            originalAspect = aspect;

            float startWidth = Mathf.Clamp(200 * aspect, minSize, maxSize);
            float startHeight = Mathf.Clamp(200, minSize, maxSize);

            previewRect.sizeDelta = new Vector2(startWidth, startHeight);

            // Обновляем слайдеры
            if (widthSlider != null)
                widthSlider.value = startWidth;
            if (heightSlider != null)
                heightSlider.value = startHeight;

            // Центрируем превью
            previewRect.anchoredPosition = Vector2.zero;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isResizing) return;

        Vector2 delta = eventData.position - dragStartPoint;
        previewRect.anchoredPosition = originalPosition + delta;

        // Обновляем позицию на объекте
        OnTransformChanged?.Invoke(previewRect.anchoredPosition, previewRect.sizeDelta, previewRect.eulerAngles.z);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isResizing) return;

        isDragging = true;
        dragStartPoint = eventData.position;
        originalPosition = previewRect.anchoredPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
    }

    void OnResizeHandleDrag(PointerEventData eventData, string handleName)
    {
        if (!isResizing) return;

        Vector2 delta = eventData.position - dragStartPoint;

        // Конвертируем дельту в локальное пространство
        RectTransform canvasRect = previewRect.root as RectTransform;
        Vector2 localDelta;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            previewRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localDelta);

        Vector2 newSize = originalSize;

        // Изменяем размер в зависимости от угла
        switch (handleName)
        {
            case "TopRight":
                newSize.x = Mathf.Clamp(originalSize.x + delta.x, minSize, maxSize);
                newSize.y = Mathf.Clamp(originalSize.y - delta.y, minSize, maxSize);
                break;
            case "TopLeft":
                newSize.x = Mathf.Clamp(originalSize.x - delta.x, minSize, maxSize);
                newSize.y = Mathf.Clamp(originalSize.y - delta.y, minSize, maxSize);
                break;
            case "BottomRight":
                newSize.x = Mathf.Clamp(originalSize.x + delta.x, minSize, maxSize);
                newSize.y = Mathf.Clamp(originalSize.y + delta.y, minSize, maxSize);
                break;
            case "BottomLeft":
                newSize.x = Mathf.Clamp(originalSize.x - delta.x, minSize, maxSize);
                newSize.y = Mathf.Clamp(originalSize.y + delta.y, minSize, maxSize);
                break;
        }

        // Сохраняем пропорции если нужно
        if (lockAspect)
        {
            float aspect = originalSize.x / originalSize.y;
            if (Mathf.Abs(newSize.x - originalSize.x) > Mathf.Abs(newSize.y - originalSize.y))
            {
                newSize.y = newSize.x / aspect;
            }
            else
            {
                newSize.x = newSize.y * aspect;
            }
        }

        // Применяем новый размер
        previewRect.sizeDelta = newSize;

        // Обновляем слайдеры
        if (widthSlider != null)
            widthSlider.value = newSize.x;
        if (heightSlider != null)
            heightSlider.value = newSize.y;

        // Обновляем трансформацию
        OnTransformChanged?.Invoke(previewRect.anchoredPosition, newSize, previewRect.eulerAngles.z);
    }

    void OnBeginResizeHandle(string handleName)
    {
        isResizing = true;
        dragStartPoint = Input.mousePosition;
        originalSize = previewRect.sizeDelta;

    }

    void OnEndResizeHandle()
    {
        isResizing = false;
    }

    void OnWidthChanged(float value)
    {
        if (lockAspect)
        {
            float aspect = previewRect.sizeDelta.x / previewRect.sizeDelta.y;
            float newHeight = value / aspect;
            previewRect.sizeDelta = new Vector2(value, newHeight);

            if (heightSlider != null)
                heightSlider.value = newHeight;
        }
        else
        {
            previewRect.sizeDelta = new Vector2(value, previewRect.sizeDelta.y);
        }

        OnTransformChanged?.Invoke(previewRect.anchoredPosition, previewRect.sizeDelta, previewRect.eulerAngles.z);
    }

    void OnHeightChanged(float value)
    {
        if (lockAspect)
        {
            float aspect = previewRect.sizeDelta.x / previewRect.sizeDelta.y;
            float newWidth = value * aspect;
            previewRect.sizeDelta = new Vector2(newWidth, value);

            if (widthSlider != null)
                widthSlider.value = newWidth;
        }
        else
        {
            previewRect.sizeDelta = new Vector2(previewRect.sizeDelta.x, value);
        }

        OnTransformChanged?.Invoke(previewRect.anchoredPosition, previewRect.sizeDelta, previewRect.eulerAngles.z);
    }

    void OnRotationChanged(float value)
    {
        previewRect.eulerAngles = new Vector3(0, 0, value);
        OnTransformChanged?.Invoke(previewRect.anchoredPosition, previewRect.sizeDelta, value);
    }

    void OnLockAspectChanged(bool locked)
    {
        lockAspect = locked;
    }

    public void Reset()
    {
        previewRect.anchoredPosition = Vector2.zero;
        previewRect.sizeDelta = originalSize;
        previewRect.eulerAngles = Vector2.zero;

        if (widthSlider != null)
            widthSlider.value = originalSize.x;
        if (heightSlider != null)
            heightSlider.value = originalSize.y;
        if (rotationSlider != null)
            rotationSlider.value = 0;
    }
}