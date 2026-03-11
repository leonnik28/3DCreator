using UnityEngine;
using DecalSystem.Interfaces;

namespace DecalSystem.Adapters
{
    /// <summary>
    /// Адаптер для DecalController, реализующий IImageDataProvider
    /// </summary>
    public class DecalImageDataAdapter : IImageDataProvider
    {
        private readonly DecalController _decal;

        public DecalImageDataAdapter(DecalController decal)
        {
            _decal = decal ?? throw new System.ArgumentNullException(nameof(decal));
        }

        public Texture2D GetTexture() => _decal.GetTexture();
        public float GetAspectRatio() => _decal.GetAspectRatio();
        public Vector3 GetWorldPosition() => _decal.transform.position;
        public float GetWorldRotation() => _decal.transform.eulerAngles.z;
    }
}