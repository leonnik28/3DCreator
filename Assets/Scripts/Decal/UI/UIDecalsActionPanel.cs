using UnityEngine;
using UnityEngine.UI;
using System;
using PreviewSystem.Interfaces;

/// <summary>
/// ������ �������� � �������� (�������� ���, ������� ���������, ������)
/// </summary>
public class UIDecalsActionPanel : UIPanelBase
{
    [Header("Buttons")]
    [SerializeField] private Button _clearAllButton;
    [SerializeField] private Button _deleteSelectedButton;
    [SerializeField] private Button _mirrorSelectedButton;
    [SerializeField] private Button _snapshotButton;
    [SerializeField] private Button _generateDescriptionButton;

    [Header("Icons (Optional)")]
    [SerializeField] private GameObject _deleteButtonIcon;
    [SerializeField] private GameObject _deleteButtonDisabledIcon;

    public event Action OnClearAllClicked;
    public event Action OnDeleteSelectedClicked;
    public event Action OnMirrorSelectedClicked;
    public event Action OnSnapshotClicked;
    public event Action OnGenerateDescriptionClicked;

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

        if (_mirrorSelectedButton == null)
            Debug.LogWarning("Mirror Selected button is not assigned!");

        if (_snapshotButton == null)
            Debug.LogWarning("Snapshot button is not assigned!");

        if (_generateDescriptionButton == null)
            Debug.LogWarning("Generate Description button is not assigned!");
    }

    private void SetupButtons()
    {
        if (_clearAllButton != null)
            _clearAllButton.onClick.AddListener(() => OnClearAllClicked?.Invoke());

        if (_deleteSelectedButton != null)
            _deleteSelectedButton.onClick.AddListener(() => OnDeleteSelectedClicked?.Invoke());

        if (_mirrorSelectedButton != null)
            _mirrorSelectedButton.onClick.AddListener(() => OnMirrorSelectedClicked?.Invoke());

        if (_snapshotButton != null)
            _snapshotButton.onClick.AddListener(() => OnSnapshotClicked?.Invoke());

        if (_generateDescriptionButton != null)
            _generateDescriptionButton.onClick.AddListener(() => OnGenerateDescriptionClicked?.Invoke());
    }

    /// <summary>
    /// ������������� � �������� ��������
    /// </summary>
    public void Initialize(IDecalRemovalService removalService)
    {
        _removalService = removalService ?? throw new ArgumentNullException(nameof(removalService));
        UpdateDeleteButtonState();
    }

    /// <summary>
    /// ���������� ��������� ������ ��������
    /// </summary>
    public void UpdateDeleteButtonState()
    {
        if (_deleteSelectedButton == null) return;

        bool canDelete = _removalService?.CanDeleteSelected() ?? false;

        _deleteSelectedButton.interactable = canDelete;

        // �����������: ������ ������ � ����������� �� ���������
        if (_deleteButtonIcon != null)
            _deleteButtonIcon.SetActive(canDelete);

        if (_deleteButtonDisabledIcon != null)
            _deleteButtonDisabledIcon.SetActive(!canDelete);
    }

    /// <summary>
    /// ����� ��������� ������
    /// </summary>
    public void ResetButtons()
    {
        if (_clearAllButton != null)
            _clearAllButton.interactable = true;

        if (_snapshotButton != null)
            _snapshotButton.interactable = true;

        if (_generateDescriptionButton != null)
            _generateDescriptionButton.interactable = true;

        UpdateDeleteButtonState();
    }

    /// <summary>
    /// ���������� ���� ������ (��������, �� ����� ��������)
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

        if (_generateDescriptionButton != null)
            _generateDescriptionButton.interactable = interactable;
    }

    /// <summary>
    /// ����������/�������� ������
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
