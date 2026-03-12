using UnityEngine.EventSystems;

namespace Fotocentr.Core
{
    /// <summary>
    /// Объект, который можно перетаскивать указателем. Используется для слабого связывания.
    /// </summary>
    public interface IDragTarget
    {
        void HandlePointerDrag(PointerEventData eventData);
    }
}
