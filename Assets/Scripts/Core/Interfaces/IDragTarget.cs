using UnityEngine.EventSystems;

namespace Fotocentr.Core
{
    public interface IDragTarget
    {
        void HandlePointerDrag(PointerEventData eventData);
    }
}
