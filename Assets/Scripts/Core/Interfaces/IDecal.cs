using UnityEngine;
using System;

namespace Fotocentr.Core
{
    /// <summary>
    /// Абстракция 3D-декали на модели. Позволяет работать с декалью через интерфейс.
    /// </summary>
    public interface IDecal
    {
        Texture2D GetTexture();
        float GetAspectRatio();
        float GetSize();
        float CreationTime { get; set; }

        void PlaceOnSurface(Vector3 hitPoint, Vector3 hitNormal);
        void SetSize(float worldHalfHeight);
        void SetAspectRatio(float aspect);
        void SetSelected(bool selected);

        event Action<IDecal> OnDeleted;
    }
}
