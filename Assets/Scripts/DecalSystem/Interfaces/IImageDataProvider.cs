using UnityEngine;

namespace DecalSystem.Interfaces
{
    /// <summary>
    /// Интерфейс для провайдера данных изображения
    /// </summary>
    public interface IImageDataProvider
    {
        Texture2D GetTexture();
        float GetAspectRatio();
        Vector3 GetWorldPosition();
        float GetWorldRotation();
    }
}