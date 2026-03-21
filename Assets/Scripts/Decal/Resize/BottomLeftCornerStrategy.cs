using UnityEngine;

namespace DecalSystem.CornerResize
{
    /// <summary>
    /// Логика для левого нижнего угла.
    /// Тянем вправо — уменьшаем ширину; влево — увеличиваем. Вниз — увеличиваем высоту; вверх — уменьшаем.
    /// </summary>
    public sealed class BottomLeftCornerStrategy : ICornerResizeStrategy
    {
        public Vector2 GetSizeDeltaFromScreenDelta(Vector2 screenDelta)
        {
            // Левый нижний: вправо — уменьшаем ширину, вниз — увеличиваем высоту
            return new Vector2(-screenDelta.x, -screenDelta.y);
        }

        public Vector2 GetPositionDeltaFromSizeDelta(Vector2 sizeDelta)
        {
            return new Vector2(-sizeDelta.x * 0.5f, -sizeDelta.y * 0.5f);
        }
    }
}
