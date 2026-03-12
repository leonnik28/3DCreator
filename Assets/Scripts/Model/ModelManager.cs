using UnityEngine;
using System;

/// <summary>
/// Управляет текущей 3D-моделью и её сменой.
/// </summary>
public class ModelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ModelDatabase _database;
    [SerializeField] private Transform _modelContainer;
    [SerializeField] private DecalManager _decalManager;
    [SerializeField] private DecalFactory _decalFactory;
    [SerializeField] private ModelRotator _modelRotator;
    [SerializeField] private OrbitCameraController _orbitCamera;

    public event Action<GameObject, int> OnModelChanged;

    private GameObject _currentModelInstance;
    private int _currentIndex = -1;

    public GameObject CurrentModel => _currentModelInstance;
    public int CurrentIndex => _currentIndex;
    public ModelDatabase Database => _database;

    private void Awake()
    {
        if (_modelContainer == null)
            _modelContainer = transform;

        if (_database != null && _database.Count > 0 && _currentIndex < 0)
            SetModel(0);
    }

    public void SetModel(int index)
    {
        if (_database == null || index < 0 || index >= _database.Count)
            return;

        var entry = _database[index];
        if (entry.Prefab == null)
        {
            Debug.LogWarning($"Model at index {index} has no prefab assigned.");
            return;
        }

        if (_currentModelInstance != null)
        {
            _decalManager?.ClearAllDecals();
            Destroy(_currentModelInstance);
            _currentModelInstance = null;
        }

        _currentModelInstance = Instantiate(entry.Prefab, _modelContainer);
        _currentModelInstance.name = entry.Prefab.name;

        EnsureCollider(_currentModelInstance);
        _currentIndex = index;

        if (_decalManager != null)
            _decalManager.SetTargetModel(_currentModelInstance);

        if (_modelRotator != null)
            _modelRotator.SetTarget(_currentModelInstance.transform);
        if (_orbitCamera != null)
            _orbitCamera.SetTarget(_currentModelInstance.transform);

        OnModelChanged?.Invoke(_currentModelInstance, _currentIndex);
    }

    public void NextModel()
    {
        if (_database == null || _database.Count == 0) return;
        SetModel((_currentIndex + 1) % _database.Count);
    }

    public void PreviousModel()
    {
        if (_database == null || _database.Count == 0) return;
        SetModel((_currentIndex - 1 + _database.Count) % _database.Count);
    }

    private void EnsureCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() == null)
            go.AddComponent<MeshCollider>();
    }
}
