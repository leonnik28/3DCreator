using System.Collections.Generic;

namespace PreviewSystem.Interfaces
{
    public interface ILayerOrderStrategy
    {
        void ApplyOrder(IEnumerable<IDecalLayer> layers);
    }

    public interface ISelectionStrategy
    {
        void ApplySelection(IEnumerable<IDecalLayer> layers, DecalController selected);
    }
}