using UnityEngine;
using System;
using DecalSystem.Interfaces;
using DecalSystem.Strategies;

namespace DecalSystem.Factories
{
    /// <summary>
    /// Фабрика для создания стратегий
    /// </summary>
    public static class PreviewWindowStrategyFactory
    {
        public static IImagePositioningStrategy CreatePositioningStrategy(
            Camera camera,
            RectTransform canvasRect)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));
            if (canvasRect == null)
                throw new ArgumentNullException(nameof(canvasRect));

            return new WorldToUIPositioningStrategy(camera, canvasRect);
        }

        public static IImageSizeStrategy CreateSizeStrategy()
        {
            return new AspectRatioSizeStrategy();
        }

        public static IDragHandlerStrategy CreateDragStrategy(
            RectTransform imageRect,
            IStateValidator validator,
            Action onTransformChanged)
        {
            if (imageRect == null)
                throw new ArgumentNullException(nameof(imageRect));
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            return new ImageDragStrategy(imageRect, validator, onTransformChanged);
        }
    }
}