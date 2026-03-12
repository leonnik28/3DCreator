using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Применяет цвета к частям модели. Создаёт instance materials.
/// </summary>
public class ModelColorizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ModelManager _modelManager;

    private ModelPartConfig _currentConfig;
    private readonly Dictionary<string, Material> _instanceMaterials = new Dictionary<string, Material>();

    private void Start()
    {
        if (_modelManager == null)
            _modelManager = FindObjectOfType<ModelManager>();

        if (_modelManager != null)
            _modelManager.OnModelChanged += OnModelChanged;

        RefreshConfig();
    }

    private void OnDestroy()
    {
        if (_modelManager != null)
            _modelManager.OnModelChanged -= OnModelChanged;

        foreach (var mat in _instanceMaterials.Values)
            if (mat != null) Destroy(mat);
        _instanceMaterials.Clear();
    }

    private void OnModelChanged(GameObject model, int index)
    {
        RefreshConfig();
    }

    private void RefreshConfig()
    {
        _currentConfig = null;
        foreach (var mat in _instanceMaterials.Values)
            if (mat != null) Destroy(mat);
        _instanceMaterials.Clear();

        if (_modelManager?.CurrentModel == null) return;

        _currentConfig = _modelManager.CurrentModel.GetComponent<ModelPartConfig>();
        if (_currentConfig == null)
        {
            var renderer = _modelManager.CurrentModel.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                _currentConfig = _modelManager.CurrentModel.gameObject.AddComponent<ModelPartConfig>();
                _currentConfig.SetupDefaultPart(renderer);
            }
        }
    }

    public void SetPartColor(int partIndex, Color color)
    {
        var entry = _currentConfig?.GetPart(partIndex);
        if (entry == null || entry.TargetRenderer == null) return;

        var sharedMats = entry.TargetRenderer.sharedMaterials;
        var mats = entry.TargetRenderer.materials;
        int start = entry.MaterialIndex >= 0 ? entry.MaterialIndex : 0;
        int end = entry.MaterialIndex >= 0 ? entry.MaterialIndex + 1 : mats.Length;

        for (int i = start; i < end && i < mats.Length; i++)
        {
            var key = $"{entry.TargetRenderer.GetInstanceID()}_{i}";
            if (!_instanceMaterials.TryGetValue(key, out var mat))
            {
                if (sharedMats != null && i < sharedMats.Length && sharedMats[i] != null)
                {
                    mat = new Material(sharedMats[i]);
                    _instanceMaterials[key] = mat;
                    mats[i] = mat;
                }
            }
            if (mat != null)
            {
                if (mat.HasProperty("_Color")) mat.color = color;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            }
        }
        entry.TargetRenderer.materials = mats;
    }

    public void SetPartColor(string partId, Color color)
    {
        var entry = _currentConfig?.GetPartById(partId);
        if (entry == null) return;

        int idx = System.Array.IndexOf(_currentConfig.Parts, entry);
        SetPartColor(idx, color);
    }

    public ModelPartConfig GetCurrentConfig() => _currentConfig;
}
