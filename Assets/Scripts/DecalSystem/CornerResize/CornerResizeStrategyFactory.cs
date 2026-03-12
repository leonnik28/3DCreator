using UnityEngine;

namespace DecalSystem.CornerResize
{
    /// <summary>
    /// Создаёт стратегию ресайза по типу угла.
    /// </summary>
    public static class CornerResizeStrategyFactory
    {
        private static readonly ICornerResizeStrategy TopLeft = new TopLeftCornerStrategy();
        private static readonly ICornerResizeStrategy TopRight = new TopRightCornerStrategy();
        private static readonly ICornerResizeStrategy BottomLeft = new BottomLeftCornerStrategy();
        private static readonly ICornerResizeStrategy BottomRight = new BottomRightCornerStrategy();

        public static ICornerResizeStrategy GetStrategy(CornerType cornerType)
        {
            return cornerType switch
            {
                CornerType.TopLeft => TopLeft,
                CornerType.TopRight => TopRight,
                CornerType.BottomLeft => BottomLeft,
                CornerType.BottomRight => BottomRight,
                _ => TopLeft
            };
        }
    }
}
