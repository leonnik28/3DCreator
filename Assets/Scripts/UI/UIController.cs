using UnityEngine;

/// <summary>
/// Координатор UI. UIDecalsActionPanel содержит Clear All и Snapshot — обрабатывается через DecalEditPanel.
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DecalManager _decalManager;

    private void Start()
    {
        if (_decalManager == null)
            _decalManager = FindObjectOfType<DecalManager>();
    }
}
