using UnityEngine;

namespace DecalSystem.Interfaces
{
    /// <summary>
    /// Интерфейс для стратегии вычисления размера изображения
    /// </summary>
    public interface IImageSizeStrategy
    {
        Vector2 CalculateSize(IImageDataProvider dataProvider, Rect windowRect);
    }
}