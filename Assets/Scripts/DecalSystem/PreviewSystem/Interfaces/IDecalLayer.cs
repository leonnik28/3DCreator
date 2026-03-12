using UnityEngine;

namespace PreviewSystem.Interfaces
{
    /// <summary>
    /// Интерфейс слоя декали в UI
    /// </summary>
    public interface IDecalLayer
    {
        DecalController SourceDecal { get; }
        event System.Action<DecalController> OnLayerClicked;

        void Initialize(DecalController decal, RectTransform parentRect);
        void UpdateTransform(Vector2 position, float rotation);
        void UpdateWindowSize();
        void SetSelected(bool selected);
        void SetVisualParameters(ILayerVisualParameters parameters);
        void SetOnMoved(System.Action callback);

        RectTransform RectTransform { get; }
        GameObject gameObject { get; }
        Transform transform { get; }
    }

    /// <summary>
    /// Параметры визуального оформления слоя
    /// </summary>
    public interface ILayerVisualParameters
    {
        Color OutlineColor { get; set; }
        Color DimColor { get; set; }
        float OutlineWidth { get; set; }
    }
}