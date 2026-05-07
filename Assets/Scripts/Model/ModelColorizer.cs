using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Применяет цвета к частям модели через MaterialPropertyBlock (без создания instance материалов).
/// </summary>
public class ModelColorizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ModelManager _modelManager;

    private ModelPartConfig _currentConfig;
    private readonly Dictionary<string, MaterialPropertyBlock> _propertyBlocks = new Dictionary<string, MaterialPropertyBlock>();
    private readonly Dictionary<int, Color> _partColorCache = new Dictionary<int, Color>();

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

        _propertyBlocks.Clear();
        _partColorCache.Clear();
    }

    private void OnModelChanged(GameObject model, int index)
    {
        RefreshConfig();
    }

    private void RefreshConfig()
    {
        _currentConfig = null;
        _propertyBlocks.Clear();
        _partColorCache.Clear();

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
        // На старте модель может появиться раньше, чем соберётся конфиг частей.
        // Поэтому на всякий случай дёргаем RefreshConfig при отсутствии конфигурации.
        if (_currentConfig == null)
            RefreshConfig();

        var entry = _currentConfig?.GetPart(partIndex);
        if (entry == null || entry.TargetRenderer == null) return;

        var sharedMats = entry.TargetRenderer.sharedMaterials;
        int start = entry.MaterialIndex >= 0 ? entry.MaterialIndex : 0;
        int end = entry.MaterialIndex >= 0 ? entry.MaterialIndex + 1 : (sharedMats != null ? sharedMats.Length : 0);

        for (int i = start; i < end && sharedMats != null && i < sharedMats.Length; i++)
        {
            var key = $"{entry.TargetRenderer.GetInstanceID()}_{i}";
            if (!_propertyBlocks.TryGetValue(key, out var block) || block == null)
            {
                block = new MaterialPropertyBlock();
                _propertyBlocks[key] = block;
            }

            entry.TargetRenderer.GetPropertyBlock(block, i);

            var mat = sharedMats[i];
            if (mat != null)
            {
                bool hasSurfaceColor = mat.HasProperty("_SurfaceColor");

                if (mat.HasProperty("_Color"))
                    block.SetColor("_Color", color);

                if (hasSurfaceColor)
                {
                    block.SetColor("_SurfaceColor", color);
                    if (mat.HasProperty("_BaseColor"))
                        block.SetColor("_BaseColor", Color.white);
                }
                else if (mat.HasProperty("_BaseColor"))
                {
                    block.SetColor("_BaseColor", color);
                }
            }

            // Применяем блок к Renderer целиком.
            // Для большинства случаев "часть" = отдельный Renderer, поэтому этого достаточно
            // и надёжнее, чем SetPropertyBlock(..., materialIndex).
            entry.TargetRenderer.SetPropertyBlock(block, i);
        }

        _partColorCache[partIndex] = color;
    }

    public void SetPartColor(string partId, Color color)
    {
        var entry = _currentConfig?.GetPartById(partId);
        if (entry == null) return;

        int idx = System.Array.IndexOf(_currentConfig.Parts, entry);
        SetPartColor(idx, color);
    }

    public ModelPartConfig GetCurrentConfig() => _currentConfig;

    public bool TryGetPartColor(int partIndex, out Color color)
    {
        if (_partColorCache.TryGetValue(partIndex, out color))
            return true;
        color = default;
        return false;
    }
}
