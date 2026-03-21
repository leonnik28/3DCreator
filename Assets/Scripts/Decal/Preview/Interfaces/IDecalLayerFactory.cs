using UnityEngine;

namespace PreviewSystem.Interfaces
{
    /// <summary>
    /// Фабрика для создания слоёв декалей
    /// </summary>
    public interface IDecalLayerFactory
    {
        IDecalLayer Create(DecalController decal, RectTransform parentRect);
        void Destroy(IDecalLayer layer);
    }
}