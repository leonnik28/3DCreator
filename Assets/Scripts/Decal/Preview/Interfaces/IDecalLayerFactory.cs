using UnityEngine;

namespace PreviewSystem.Interfaces
{
    public interface IDecalLayerFactory
    {
        IDecalLayer Create(DecalController decal, RectTransform parentRect);
        void Destroy(IDecalLayer layer);
    }
}