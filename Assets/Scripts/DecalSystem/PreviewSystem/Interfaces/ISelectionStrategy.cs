using System.Collections.Generic;

namespace PreviewSystem.Interfaces
{
    /// <summary>
    /// Стратегия управления порядком слоёв
    /// </summary>
    public interface ILayerOrderStrategy
    {
        void ApplyOrder(IEnumerable<IDecalLayer> layers);
    }

    /// <summary>
    /// Стратегия выделения слоя
    /// </summary>
    public interface ISelectionStrategy
    {
        void ApplySelection(IEnumerable<IDecalLayer> layers, DecalController selected);
    }
}