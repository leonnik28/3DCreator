using UnityEngine;

namespace DecalSystem.CornerResize
{
    /// <summary>
    /// Логика для правого нижнего угла.
    /// Тянем влево — уменьшаем ширину; вправо — увеличиваем. Вниз — увеличиваем высоту; вверх — уменьшаем.
    /// </summary>
    public sealed class BottomRightCornerStrategy : ICornerResizeStrategy
    {
        public Vector2 GetSizeDeltaFromScreenDelta(Vector2 screenDelta)
        {
            // Правый нижний: влево — уменьшаем ширину, вниз — увеличиваем высоту
            return new Vector2(screenDelta.x, -screenDelta.y);
        }

        public Vector2 GetPositionDeltaFromSizeDelta(Vector2 sizeDelta)
        {
            return new Vector2(sizeDelta.x * 0.5f, -sizeDelta.y * 0.5f);
        }
    }
}
