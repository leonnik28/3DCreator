using UnityEngine;

namespace PreviewSystem.Interfaces
{
    /// <summary>
    /// Стратегия визуального оформления слоя
    /// </summary>
    public interface IDecalLayerVisualStrategy
    {
        void ApplySelection(IDecalLayer layer, bool selected);
        void UpdateVisual(IDecalLayer layer, ILayerVisualParameters parameters);
        void Initialize(IDecalLayer layer);
        void Cleanup(IDecalLayer layer);
    }

    /// <summary>
    /// Провайдер визуальных параметров
    /// </summary>
    public interface IVisualParametersProvider
    {
        ILayerVisualParameters GetParameters();
    }
}