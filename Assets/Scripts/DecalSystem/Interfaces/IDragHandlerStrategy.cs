using UnityEngine;

namespace DecalSystem.Interfaces
{
    /// <summary>
    /// Интерфейс для обработчика перетаскивания
    /// </summary>
    public interface IDragHandlerStrategy
    {
        void OnBeginDrag(Vector2 startPosition);
        void OnDrag(Vector2 currentPosition, Vector2 startPosition);
        void OnEndDrag();
    }
}