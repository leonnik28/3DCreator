using UnityEngine;

namespace DecalSystem.Interfaces
{
    /// <summary>
    /// Интерфейс для стратегии позиционирования изображения
    /// </summary>
    public interface IImagePositioningStrategy
    {
        Vector2 CalculatePosition(IImageDataProvider dataProvider);
    }
}