using UnityEngine;
using UnityEngine.UI;
using System;
using PreviewSystem.Interfaces;

/// <summary>
/// Панель действий с декалями (очистить все, удалить выбранное, снимок)
/// </summary>
public class UIDecalsActionPanel : UIPanelBase
{
    [Header("Buttons")]
    [SerializeField] private Button _clearAllButton;
    [SerializeField] private Button _deleteSelectedButton;
    [SerializeField] private Button _snapshotButton;

    [Header("Icons (Optional)")]
    [SerializeField] private GameObject _deleteButtonIcon;
    [SerializeField] private GameObject _deleteButtonDisabledIcon;

    public event Action OnClearAllClicked;
    public event Action OnDeleteSelectedClicked;
    public event Action OnSnapshotClicked;

    private IDecalRemovalService _removalService;

    private void Awake()
    {
        ValidateButtons();
    }

    private void Start()
    {
        SetupButtons();
    }

    private void ValidateButtons()
    {
        if (_clearAllButton == null)
            Debug.LogWarning("Clear All button is not assigned!");

        if (_deleteSelectedButton == null)
            Debug.LogWarning("Delete Selected button is not assigned!");

        if (_snapshotButton == null)
            Debug.LogWarning("Snapshot button is not assigned!");
    }

    private void SetupButtons()
    {
        if (_clearAllButton != null)
            _clearAllButton.onClick.AddListener(() => OnClearAllClicked?.Invoke());

        if (_deleteSelectedButton != null)
            _deleteSelectedButton.onClick.AddListener(() => OnDeleteSelectedClicked?.Invoke());

        if (_snapshotButton != null)
            _snapshotButton.onClick.AddListener(() => OnSnapshotClicked?.Invoke());
    }

    /// <summary>
    /// Инициализация с сервисом удаления
    /// </summary>
    public void Initialize(IDecalRemovalService removalService)
    {
        _removalService = removalService ?? throw new ArgumentNullException(nameof(removalService));
        UpdateDeleteButtonState();
    }

    /// <summary>
    /// Обновление состояния кнопки удаления
    /// </summary>
    public void UpdateDeleteButtonState()
    {
        if (_deleteSelectedButton == null) return;

        bool canDelete = _removalService?.CanDeleteSelected() ?? false;

        _deleteSelectedButton.interactable = canDelete;

        // Опционально: меняем иконку в зависимости от состояния
        if (_deleteButtonIcon != null)
            _deleteButtonIcon.SetActive(canDelete);

        if (_deleteButtonDisabledIcon != null)
            _deleteButtonDisabledIcon.SetActive(!canDelete);
    }

    /// <summary>
    /// Сброс состояния кнопок
    /// </summary>
    public void ResetButtons()
    {
        if (_clearAllButton != null)
            _clearAllButton.interactable = true;

        if (_snapshotButton != null)
            _snapshotButton.interactable = true;

        UpdateDeleteButtonState();
    }

    /// <summary>
    /// Блокировка всех кнопок (например, во время загрузки)
    /// </summary>
    public void SetButtonsInteractable(bool interactable)
    {
        if (_clearAllButton != null)
            _clearAllButton.interactable = interactable;

        if (_deleteSelectedButton != null)
        {
            _deleteSelectedButton.interactable = interactable &&
                (_removalService?.CanDeleteSelected() ?? false);
        }

        if (_snapshotButton != null)
            _snapshotButton.interactable = interactable;
    }

    /// <summary>
    /// Показывает/скрывает панель
    /// </summary>
    public override void Show()
    {
        base.Show();
        UpdateDeleteButtonState();
    }

    public override void Hide()
    {
        base.Hide();
    }
}