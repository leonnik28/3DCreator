using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ˜˜˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜˜˜, ˜˜˜˜˜, ˜˜˜˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜ ˜˜˜˜˜˜˜ ˜ ˜˜˜˜˜.
/// </summary>
public class DecalManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask _modelLayer = 1 << 0;
    [SerializeField] private float _defaultDecalSize = 0.5f;
    [SerializeField] private float _uiToWorldScale = 80f;

    [Header("Components")]
    [SerializeField] private DecalFactory _decalFactory;
    [SerializeField] private GameObject _targetModel;

    public event Action<DecalController> OnDecalCreated;
    public event Action<DecalController> OnDecalDeleted;
    public event Action<DecalController> OnDecalSelected;
    public event Action<DecalController> OnDecalTransformChanged;

    private readonly List<DecalController> _activeDecals = new List<DecalController>();
    private DecalController _selectedDecal;
    private Camera _mainCamera;
    private DecalTransformService _transformService;

    public IReadOnlyList<DecalController> ActiveDecals => _activeDecals;
    public DecalController SelectedDecal => _selectedDecal;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _transformService = new DecalTransformService(_mainCamera, _modelLayer, _uiToWorldScale);
        ValidateTargetModel();
    }

    private void ValidateTargetModel()
    {
        if (_targetModel == null)
            return;

        if (_targetModel.GetComponent<Collider>() == null)
            _targetModel.AddComponent<BoxCollider>();
    }

    public void SetTargetModel(GameObject model)
    {
        _targetModel = model;
        ValidateTargetModel();
    }

    public void CreateNewDecal(Texture2D texture)
    {
        if (texture == null || _targetModel == null || _decalFactory == null)
            return;

        var surfacePoint = GetModelCenter();
        var surfaceNormal = GetSurfaceNormalAtPoint(surfacePoint);

        var decal = _decalFactory.CreateDecal(
            texture,
            surfacePoint,
            surfaceNormal,
            _defaultDecalSize,
            _modelLayer
        );

        if (decal == null)
            return;

        decal.CreationTime = Time.time;
        decal.OnDeleted += HandleDecalDeleted;
        _activeDecals.Add(decal);

        OnDecalCreated?.Invoke(decal);
        SelectDecal(decal);
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜ ˜˜˜˜˜˜˜ CreationTime.
    /// </summary>
    public void SelectNextDecal()
    {
        if (_activeDecals.Count == 0)
        {
            SelectDecal(null);
            return;
        }

        var sorted = _activeDecals.OrderBy(d => d.CreationTime).ToList();
        var currentIndex = _selectedDecal != null ? sorted.IndexOf(_selectedDecal) : -1;
        var nextIndex = (currentIndex + 1) % sorted.Count;

        SelectDecal(sorted[nextIndex]);
    }

    public void SelectDecal(DecalController decal)
    {
        if (_selectedDecal == decal)
            return;

        if (_selectedDecal != null)
            _selectedDecal.SetSelected(false);

        _selectedDecal = decal;

        if (_selectedDecal != null)
            _selectedDecal.SetSelected(true);

        OnDecalSelected?.Invoke(_selectedDecal);
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜ ˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜ 2D-˜˜˜˜ (˜˜˜˜˜˜˜˜).
    /// </summary>
    public void UpdateEditingDecal(RectTransform layerRect, RectTransform previewRect, Canvas canvas, float rotation)
    {
        if (_selectedDecal == null)
            return;

        _transformService.UpdateTransform(_selectedDecal, layerRect, previewRect, canvas, rotation);
        OnDecalTransformChanged?.Invoke(_selectedDecal);
    }

    public void DeleteDecal(DecalController decal)
    {
        if (decal == null || !_activeDecals.Contains(decal))
            return;

        _decalFactory.DestroyDecal(decal);
        // ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜ HandleDecalDeleted
    }

    public void ClearAllDecals()
    {
        var copy = _activeDecals.ToList();
        foreach (var decal in copy)
        {
            _decalFactory.DestroyDecal(decal);
        }
    }

    private void HandleDecalDeleted(DecalController decal)
    {
        decal.OnDeleted -= HandleDecalDeleted;
        _activeDecals.Remove(decal);
        OnDecalDeleted?.Invoke(decal);

        if (_selectedDecal == decal)
        {
            SelectNextDecal();
        }
    }

    private Vector3 GetModelCenter()
    {
        if (_targetModel == null)
            return Vector3.zero;

        var renderer = _targetModel.GetComponent<Renderer>();
        if (renderer != null)
            return renderer.bounds.center;

        var collider = _targetModel.GetComponent<Collider>();
        if (collider != null)
            return collider.bounds.center;

        return _targetModel.transform.position;
    }

    private Vector3 GetSurfaceNormalAtPoint(Vector3 point)
    {
        if (_targetModel == null)
            return Vector3.up;

        var directions = new[]
        {
            Vector3.down, Vector3.up,
            Vector3.forward, Vector3.back,
            Vector3.left, Vector3.right
        };

        foreach (var dir in directions)
        {
            var ray = new Ray(point + dir * 2f, -dir);
            if (Physics.Raycast(ray, out var hit, 4f, _modelLayer) &&
                hit.collider.gameObject == _targetModel)
            {
                return hit.normal;
            }
        }

        return Vector3.up;
    }

    private void OnDestroy()
    {
        foreach (var decal in _activeDecals)
        {
            if (decal != null)
                decal.OnDeleted -= HandleDecalDeleted;
        }
    }
}