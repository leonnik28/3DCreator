using System;
using UnityEngine;

namespace PreviewSystem.Interfaces
{
    /// <summary>
    /// Интерфейс окна предпросмотра
    /// </summary>
    public interface IPreviewWindow
    {
        event Action<DecalController> OnDecalLayerClicked;

        void AddLayer(DecalController decal);
        void RemoveLayer(DecalController decal);
        void UpdateLayerPosition(DecalController decal);
        void UpdateAllLayers();
        void HighlightSelected(DecalController selected);
        void Clear();

        RectTransform GetRectTransform();
        RectTransform GetCanvasRect();
    }
}