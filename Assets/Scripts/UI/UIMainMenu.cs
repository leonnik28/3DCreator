using UnityEngine;
using UnityEngine.UI;
using System;

public class UIMainMenu : UIPanelBase
{
    [Header("Buttons")]
    [SerializeField] private Button _clearAllButton;
    [SerializeField] private Button _snapshotButton;

    public event Action OnClearAllClicked;
    public event Action OnSnapshotClicked;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (_clearAllButton != null)
            _clearAllButton.onClick.AddListener(() => OnClearAllClicked?.Invoke());

        if (_snapshotButton != null)
            _snapshotButton.onClick.AddListener(() => OnSnapshotClicked?.Invoke());
    }
}