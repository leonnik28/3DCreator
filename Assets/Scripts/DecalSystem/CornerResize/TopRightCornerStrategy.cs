using UnityEngine;

namespace DecalSystem.CornerResize
{
    /// <summary>
    /// Логика для правого верхнего угла.
    /// Тянем влево/вниз — уменьшаем; вправо/вверх — увеличиваем.
    /// </summary>
    public sealed class TopRightCornerStrategy : ICornerResizeStrategy
    {
        public Vector2 GetSizeDeltaFromScreenDelta(Vector2 screenDelta)
        {
            return new Vector2(screenDelta.x, screenDelta.y);
        }

        public Vector2 GetPositionDeltaFromSizeDelta(Vector2 sizeDelta)
        {
            return new Vector2(sizeDelta.x * 0.5f, sizeDelta.y * 0.5f);
        }
    }
}
