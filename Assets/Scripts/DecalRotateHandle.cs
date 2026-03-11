using UnityEngine;
using UnityEngine.EventSystems;

public class DecalRotateHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [SerializeField] private float _sensitivity = 0.5f;

    private IDecalEditor _editor;
    private RectTransform _targetRect;
    private float _dragStartRotation;

    public void Initialize(IDecalEditor editor)
    {
        _editor = editor;
        _targetRect = editor.GetPreviewRect();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragStartRotation = _targetRect.eulerAngles.z;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float rotationDelta = eventData.delta.x * _sensitivity;
        float newRotation = _dragStartRotation + rotationDelta;
        newRotation = (newRotation % 360 + 360) % 360;

        _targetRect.eulerAngles = new Vector3(0, 0, newRotation);
        _editor.OnTransformChanged();
    }
}