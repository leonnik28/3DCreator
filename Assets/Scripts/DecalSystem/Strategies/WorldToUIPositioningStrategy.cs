using UnityEngine;
using DecalSystem.Interfaces;

namespace DecalSystem.Strategies
{
    /// <summary>
    /// Стратегия позиционирования изображения на основе мировых координат
    /// </summary>
    public class WorldToUIPositioningStrategy : IImagePositioningStrategy
    {
        private readonly Camera _camera;
        private readonly RectTransform _canvasRect;

        public WorldToUIPositioningStrategy(Camera camera, RectTransform canvasRect)
        {
            _camera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            _canvasRect = canvasRect ?? throw new System.ArgumentNullException(nameof(canvasRect));
        }

        public Vector2 CalculatePosition(IImageDataProvider dataProvider)
        {
            if (dataProvider == null)
                throw new System.ArgumentNullException(nameof(dataProvider));

            Vector3 worldPos = dataProvider.GetWorldPosition();
            Vector2 viewportPoint = _camera.WorldToViewportPoint(worldPos);

            return new Vector2(
                (viewportPoint.x - 0.5f) * _canvasRect.rect.width,
                (viewportPoint.y - 0.5f) * _canvasRect.rect.height
            );
        }
    }
}