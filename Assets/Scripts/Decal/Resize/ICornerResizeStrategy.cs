using UnityEngine;

namespace DecalSystem.CornerResize
{
    public interface ICornerResizeStrategy
    {
        Vector2 GetSizeDeltaFromScreenDelta(Vector2 screenDelta);

        Vector2 GetPositionDeltaFromSizeDelta(Vector2 sizeDelta);
    }
}
