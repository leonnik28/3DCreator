using UnityEngine;

/// <summary>
/// ˜˜˜˜˜˜ ˜˜˜˜˜˜˜˜˜˜˜˜˜ 2D-˜˜˜˜˜˜ ˜ 3D-˜˜˜˜˜˜˜. ˜˜˜˜˜˜ ˜˜˜˜ ˜˜˜˜ ˜˜˜ ˜˜˜˜˜˜˜˜.
/// </summary>
public class DecalTransformService
{
    private readonly Camera _camera;
    private readonly LayerMask _modelLayer;
    private readonly float _uiToWorldScale;

    public DecalTransformService(Camera camera, LayerMask layerMask, float uiToWorldScale = 100f)
    {
        _camera = camera;
        _modelLayer = layerMask;
        _uiToWorldScale = uiToWorldScale;
    }

    /// <summary>
    /// ˜˜˜˜˜˜˜˜˜ 3D-˜˜˜˜˜˜ ˜˜ ˜˜˜˜˜˜ 2D-˜˜˜˜.
    /// </summary>
    public void UpdateTransform(
        DecalController decal,
        RectTransform layerRect,
        RectTransform previewRect,
        Canvas canvas,
        float rotation)
    {
        if (decal == null || layerRect == null || _camera == null)
            return;

        Vector2 viewportPoint = GetLayerCenterViewportPoint(layerRect, canvas);
        var ray = _camera.ViewportPointToRay(viewportPoint);
        if (Physics.Raycast(ray, out var hit, 100f, _modelLayer))
        {
            decal.PlaceOnSurface(hit.point, hit.normal);
        }

        Vector2 uiSize = layerRect.sizeDelta;
        float previewH = (previewRect != null && previewRect.rect.height > 1f) ? previewRect.rect.height : 200f;
        float layerRatio = Mathf.Clamp01(Mathf.Abs(uiSize.y) / previewH);
        float worldHalfHeight = (layerRatio * previewH * 0.5f) / _uiToWorldScale;

        decal.SetSize(worldHalfHeight);
        decal.SetAspectRatio(uiSize.x / Mathf.Max(uiSize.y, 0.001f));

        var euler = decal.transform.eulerAngles;
        decal.transform.eulerAngles = new Vector3(euler.x, euler.y, rotation);
    }

    private Vector2 GetLayerCenterViewportPoint(RectTransform layerRect, Canvas canvas)
    {
        if (layerRect == null) return new Vector2(0.5f, 0.5f);

        Vector3[] corners = new Vector3[4];
        layerRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;

        Vector2 screenPoint;
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            screenPoint = new Vector2(center.x, center.y);
        else
        {
            Camera cam = canvas?.worldCamera ?? _camera ?? Camera.main;
            screenPoint = RectTransformUtility.WorldToScreenPoint(cam, center);
        }
        return new Vector2(screenPoint.x / Screen.width, screenPoint.y / Screen.height);
    }
}
