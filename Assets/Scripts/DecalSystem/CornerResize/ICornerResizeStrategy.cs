using UnityEngine;

namespace DecalSystem.CornerResize
{
    /// <summary>
    /// Стратегия расчёта размера и позиции при перетаскивании угловой ручки.
    /// Каждый угол имеет свою логику: направление движения мыши по-разному влияет на width/height.
    /// </summary>
    public interface ICornerResizeStrategy
    {
        /// <summary>
        /// Преобразует дельту движения мыши (в экранных координатах) в изменение размера RectTransform.
        /// </summary>
        Vector2 GetSizeDeltaFromScreenDelta(Vector2 screenDelta);

        /// <summary>
        /// Вычисляет смещение позиции центра, чтобы противоположный угол оставался на месте.
        /// </summary>
        Vector2 GetPositionDeltaFromSizeDelta(Vector2 sizeDelta);
    }
}
