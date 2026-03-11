using UnityEngine;
using DecalSystem.Interfaces;

namespace DecalSystem.Strategies
{
    /// <summary>
    /// Стратегия вычисления размера изображения с сохранением пропорций
    /// </summary>
    public class AspectRatioSizeStrategy : IImageSizeStrategy
    {
        public Vector2 CalculateSize(IImageDataProvider dataProvider, Rect windowRect)
        {
            if (dataProvider == null)
                throw new System.ArgumentNullException(nameof(dataProvider));

            float aspectRatio = dataProvider.GetAspectRatio();
            float windowAspect = windowRect.width / windowRect.height;

            if (aspectRatio > windowAspect)
            {
                // Подгоняем по ширине
                return new Vector2(
                    windowRect.width,
                    windowRect.width / aspectRatio
                );
            }
            else
            {
                // Подгоняем по высоте
                return new Vector2(
                    windowRect.height * aspectRatio,
                    windowRect.height
                );
            }
        }
    }
}