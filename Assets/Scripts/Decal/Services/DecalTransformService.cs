using UnityEngine;

public class DecalTransformService
{
    private readonly Camera _camera;
    private readonly LayerMask _modelLayer;
    private readonly float _uiToWorldScale;
    private readonly ModelProjectionZone _projectionZone;

    public DecalTransformService(Camera camera, LayerMask layerMask, float uiToWorldScale = 100f)
    {
        _camera = camera;
        _modelLayer = layerMask;
        _uiToWorldScale = uiToWorldScale;
    }

    public DecalTransformService(Camera camera, LayerMask layerMask, ModelProjectionZone projectionZone, float uiToWorldScale = 100f)
    {
        _camera = camera;
        _modelLayer = layerMask;
        _uiToWorldScale = uiToWorldScale;
        _projectionZone = projectionZone;
    }

    public void UpdateTransform(
        DecalController decal,
        RectTransform layerRect,
        RectTransform previewRect,
        Canvas canvas,
        float rotation)
    {
        if (decal == null || layerRect == null || _camera == null)
            return;

        if (_projectionZone != null && previewRect != null)
        {
            if (TryGetClippedRectInPreview(layerRect, previewRect, canvas, out var clippedCenter, out var clippedSize))
            {
                var zoneWorldPoint = GetZoneWorldPoint(clippedCenter);
                var ray = new Ray(zoneWorldPoint + _projectionZone.transform.forward * 0.5f, -_projectionZone.transform.forward);
                if (Physics.Raycast(ray, out var hit, 5f, _modelLayer))
                    decal.PlaceOnSurface(hit.point, hit.normal);
                else
                    decal.PlaceOnSurface(zoneWorldPoint, _projectionZone.transform.forward);

                float zoneH = Mathf.Max(_projectionZone.ZoneHeight, 0.001f);
                float zoneW = zoneH * Mathf.Max(_projectionZone.CanvasAspect, 0.001f);
                float worldH = Mathf.Max(zoneH * clippedSize.y, 0.001f);
                float worldW = Mathf.Max(zoneW * clippedSize.x, 0.001f);

                decal.SetSize(worldH * 0.5f);
                decal.SetAspectRatio(worldW / worldH);
            }
            else
            {
                decal.SetSize(0.0005f);
                decal.SetAspectRatio(1f);
            }
        }
        else
        {
            Vector2 viewportPoint = GetLayerCenterViewportPoint(layerRect, canvas);
            var ray = _camera.ViewportPointToRay(viewportPoint);
            if (Physics.Raycast(ray, out var hit, 100f, _modelLayer))
                decal.PlaceOnSurface(hit.point, hit.normal);

            Vector2 uiSize = layerRect.sizeDelta;
            float prevH = (previewRect != null && previewRect.rect.height > 1f) ? previewRect.rect.height : 200f;
            float layerRatio = Mathf.Clamp01(Mathf.Abs(uiSize.y) / prevH);
            float worldHalfHeight = (layerRatio * prevH * 0.5f) / _uiToWorldScale;

            decal.SetSize(worldHalfHeight);
            decal.SetAspectRatio(uiSize.x / Mathf.Max(uiSize.y, 0.001f));

            var euler = decal.transform.eulerAngles;
            decal.transform.eulerAngles = new Vector3(euler.x, euler.y, rotation);
        }
    }

    /// <summary>
    /// Возвращает центр и размер пересечения слоя с окном превью
    /// в нормализованных координатах (0..1), где (0,0) — левый нижний угол.
    /// </summary>
    private bool TryGetClippedRectInPreview(
        RectTransform layerRect,
        RectTransform previewRect,
        Canvas canvas,
        out Vector2 center,
        out Vector2 size)
    {
        center = new Vector2(0.5f, 0.5f);
        size = Vector2.zero;

        if (layerRect == null || previewRect == null)
            return false;

        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera ?? _camera;

        var layerBounds = GetBoundsInPreviewSpace(layerRect, previewRect);
        var previewBounds = previewRect.rect;

        if (previewBounds.width <= 0.0001f || previewBounds.height <= 0.0001f)
            return false;

        float xMin = Mathf.Max(layerBounds.xMin, previewBounds.xMin);
        float xMax = Mathf.Min(layerBounds.xMax, previewBounds.xMax);
        float yMin = Mathf.Max(layerBounds.yMin, previewBounds.yMin);
        float yMax = Mathf.Min(layerBounds.yMax, previewBounds.yMax);

        if (xMax <= xMin || yMax <= yMin)
            return false;

        float cx = (xMin + xMax) * 0.5f;
        float cy = (yMin + yMax) * 0.5f;
        float w = xMax - xMin;
        float h = yMax - yMin;

        center = new Vector2(
            Mathf.InverseLerp(previewBounds.xMin, previewBounds.xMax, cx),
            Mathf.InverseLerp(previewBounds.yMin, previewBounds.yMax, cy)
        );
        size = new Vector2(
            Mathf.Clamp01(w / previewBounds.width),
            Mathf.Clamp01(h / previewBounds.height)
        );
        return true;
    }

    private Rect GetBoundsInPreviewSpace(RectTransform layerRect, RectTransform previewRect)
    {
        var layerLocalRect = layerRect.rect;
        Vector3[] localCorners = new Vector3[4]
        {
            new Vector3(layerLocalRect.xMin, layerLocalRect.yMin, 0f),
            new Vector3(layerLocalRect.xMax, layerLocalRect.yMin, 0f),
            new Vector3(layerLocalRect.xMax, layerLocalRect.yMax, 0f),
            new Vector3(layerLocalRect.xMin, layerLocalRect.yMax, 0f)
        };

        float xMin = float.MaxValue;
        float xMax = float.MinValue;
        float yMin = float.MaxValue;
        float yMax = float.MinValue;

        for (int i = 0; i < localCorners.Length; i++)
        {
            Vector3 world = layerRect.TransformPoint(localCorners[i]);
            Vector3 localInPreview = previewRect.InverseTransformPoint(world);
            xMin = Mathf.Min(xMin, localInPreview.x);
            xMax = Mathf.Max(xMax, localInPreview.x);
            yMin = Mathf.Min(yMin, localInPreview.y);
            yMax = Mathf.Max(yMax, localInPreview.y);
        }

        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private Vector3 GetZoneWorldPoint(Vector2 uv)
    {
        float zoneHeight = Mathf.Max(_projectionZone.ZoneHeight, 0.001f);
        float zoneWidth = zoneHeight * Mathf.Max(_projectionZone.CanvasAspect, 0.001f);

        float x = (uv.x - 0.5f) * zoneWidth;
        float y = (uv.y - 0.5f) * zoneHeight;

        var local = new Vector3(x, y, 0f) + _projectionZone.Offset;
        return _projectionZone.transform.TransformPoint(local);
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
