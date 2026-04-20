using UnityEngine;
using UnityEngine.UI;

public class TransformControlsController : MonoBehaviour
{
    [SerializeField] private CanvasGroup _handlesCanvasGroup;
    [SerializeField] private Toggle _lockAspectToggle;

    [SerializeField] private DecalCornerHandle _topLeftHandle;
    [SerializeField] private DecalCornerHandle _topRightHandle;
    [SerializeField] private DecalCornerHandle _bottomLeftHandle;
    [SerializeField] private DecalCornerHandle _bottomRightHandle;
    [SerializeField] private DecalRotateHandle _rotateHandle;
    [SerializeField] private DecalCenterDragZone _centerDragZone;

    private IDecalEditor _editor;
    private bool _lockAspect = true;
    private Transform _handlesOriginalParent;
    private const float HandleSize = 24f;

    public bool LockAspect => _lockAspect;

    public void Initialize(IDecalEditor editor, DecalManager decalManager)
    {
        _editor = editor;

        if (_handlesCanvasGroup != null)
            _handlesOriginalParent = _handlesCanvasGroup.transform.parent;

        if (_lockAspectToggle != null)
            _lockAspectToggle.onValueChanged.AddListener((value) => _lockAspect = value);

        SetupHandles();
        ShowControls(false);
    }

    private void SetupHandles()
    {
        if (_topLeftHandle != null) _topLeftHandle.Initialize(_editor);
        if (_topRightHandle != null) _topRightHandle.Initialize(_editor);
        if (_bottomLeftHandle != null) _bottomLeftHandle.Initialize(_editor);
        if (_bottomRightHandle != null) _bottomRightHandle.Initialize(_editor);
        if (_rotateHandle != null) _rotateHandle.Initialize(_editor);
    }

    public void ShowControls(bool show)
    {
        if (_handlesCanvasGroup != null)
        {
            _handlesCanvasGroup.alpha = show ? 1f : 0f;
            _handlesCanvasGroup.blocksRaycasts = show;

            if (show)
            {
                var layerRect = _editor?.GetPreviewRect() as RectTransform;
                if (layerRect != null)
                    PositionHandlesOnLayer(layerRect);
            }
            else if (_handlesOriginalParent != null)
            {
                _handlesCanvasGroup.transform.SetParent(_handlesOriginalParent);
            }
        }

        if (_topLeftHandle != null) _topLeftHandle.gameObject.SetActive(show);
        if (_topRightHandle != null) _topRightHandle.gameObject.SetActive(show);
        if (_bottomLeftHandle != null) _bottomLeftHandle.gameObject.SetActive(show);
        if (_bottomRightHandle != null) _bottomRightHandle.gameObject.SetActive(show);
        if (_rotateHandle != null) _rotateHandle.gameObject.SetActive(show);
        if (_centerDragZone != null) _centerDragZone.gameObject.SetActive(show);

        if (_lockAspectToggle != null)
        {
            _lockAspectToggle.gameObject.SetActive(show);
            _lockAspectToggle.SetIsOnWithoutNotify(_lockAspect);
        }
    }

    private void LateUpdate()
    {
        if (_handlesCanvasGroup != null && _handlesCanvasGroup.alpha > 0 && _editor != null)
        {
            var layerRect = _editor.GetPreviewRect() as RectTransform;
            if (layerRect != null && _handlesCanvasGroup.transform.parent == layerRect)
                PositionHandlesOnLayer(layerRect);
        }
    }

    private void PositionHandlesOnLayer(RectTransform layerRect)
    {
        if (layerRect == null) return;

        var handlesRect = _handlesCanvasGroup.GetComponent<RectTransform>();
        if (handlesRect == null) return;

        _handlesCanvasGroup.transform.SetParent(layerRect, false);
        _handlesCanvasGroup.transform.localRotation = Quaternion.identity;
        _handlesCanvasGroup.transform.localScale = Vector3.one;

        handlesRect.anchorMin = Vector2.zero;
        handlesRect.anchorMax = Vector2.one;
        handlesRect.offsetMin = Vector2.zero;
        handlesRect.offsetMax = Vector2.zero;

        float hw = HandleSize * 0.5f;
        float edgeInset = hw * 0.4f; // было hw, стало в 2 раза ближе к краю

        PositionHandle(_topLeftHandle, 0, 1, edgeInset, -edgeInset);
        PositionHandle(_topRightHandle, 1, 1, -edgeInset, -edgeInset);
        PositionHandle(_bottomLeftHandle, 0, 0, edgeInset, edgeInset);
        PositionHandle(_bottomRightHandle, 1, 0, -edgeInset, edgeInset);

        if (_rotateHandle != null)
        {
            var rt = _rotateHandle.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, HandleSize * 1.2f);
                rt.sizeDelta = new Vector2(HandleSize, HandleSize);
            }
        }

        if (_centerDragZone != null)
        {
            _centerDragZone.transform.SetAsLastSibling();
            _centerDragZone.Initialize(layerRect, layerRect.parent as RectTransform, () => _editor?.OnTransformChanged());
        }
    }

    private void PositionHandle(DecalCornerHandle handle, float anchorX, float anchorY, float offX, float offY)
    {
        if (handle == null) return;
        var rt = handle.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(anchorX, anchorY);
        rt.anchorMax = new Vector2(anchorX, anchorY);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(offX, offY);
        rt.sizeDelta = new Vector2(HandleSize, HandleSize);
    }
}
