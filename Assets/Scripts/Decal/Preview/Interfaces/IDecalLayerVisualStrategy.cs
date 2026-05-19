using UnityEngine;

namespace PreviewSystem.Interfaces
{
    public interface IDecalLayerVisualStrategy
    {
        void ApplySelection(IDecalLayer layer, bool selected);
        void UpdateVisual(IDecalLayer layer, ILayerVisualParameters parameters);
        void Initialize(IDecalLayer layer);
        void Cleanup(IDecalLayer layer);
    }

    public interface IVisualParametersProvider
    {
        ILayerVisualParameters GetParameters();
    }
}