using UnityEngine;

/// <summary>
/// ������ ������������� 2D-������ � 3D-�������. ������ ���� ���� ��� ��������.
/// </summary>
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

    /// <summary>
    /// ��������� 3D-������ �� ������ 2D-����.
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

        if (_projectionZone != null && previewRect != null)
        {
            if (TryGetLayerNormalizedPositionInPreview(layerRect, previewRect, canvas, out var uv))
            {
                var zoneWorldPoint = GetZoneWorldPoint(uv);
                var ray = new Ray(zoneWorldPoint + _projectionZone.transform.forward * 0.5f, -_projectionZone.transform.forward);
                if (Physics.Raycast(ray, out var hit, 5f, _modelLayer))
                    decal.PlaceOnSurface(hit.point, hit.normal);
                else
                    decal.PlaceOnSurface(zoneWorldPoint, _projectionZone.transform.forward);
            }

            // Маска полотна: даже если часть слоя вышла за пределы окна превью,
            // на 3D‑объекте должна отображаться только видимая часть.
            if (TryGetClippedRectInPreview(layerRect, previewRect, canvas, out var maskCenter, out var maskSize))
            {
                decal.SetCanvasMask(maskCenter, maskSize);
            }
        }
        else
        {
            Vector2 viewportPoint = GetLayerCenterViewportPoint(layerRect, canvas);
            var ray = _camera.ViewportPointToRay(viewportPoint);
            if (Physics.Raycast(ray, out var hit, 100f, _modelLayer))
                decal.PlaceOnSurface(hit.point, hit.normal);
        }

        if (_projectionZone != null && previewRect != null)
        {
            // 1:1 маппинг размеров слоя из 2D окна в размеры зоны на модели
            float previewW = Mathf.Max(previewRect.rect.width, 0.001f);
            float previewH = Mathf.Max(previewRect.rect.height, 0.001f);

            float layerW = Mathf.Abs(layerRect.rect.width);
            float layerH = Mathf.Abs(layerRect.rect.height);

            float normW = Mathf.Clamp01(layerW / previewW);
            float normH = Mathf.Clamp01(layerH / previewH);

            float zoneH = Mathf.Max(_projectionZone.ZoneHeight, 0.001f);
            float zoneW = zoneH * Mathf.Max(_projectionZone.CanvasAspect, 0.001f);

            float worldH = Mathf.Max(zoneH * normH, 0.001f);
            float worldW = Mathf.Max(zoneW * normW, 0.001f);

            decal.SetSize(worldH * 0.5f);
            decal.SetAspectRatio(worldW / worldH);

            decal.SetUpHint(_projectionZone.transform.up);
            decal.SetRollDegrees(rotation);
        }
        else
        {
            // Fallback: старая схема через масштаб UI->world
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

    private bool TryGetLayerNormalizedPositionInPreview(RectTransform layerRect, RectTransform previewRect, Canvas canvas, out Vector2 uv)
    {
        uv = new Vector2(0.5f, 0.5f);
        if (layerRect == null || previewRect == null) return false;

        Vector3[] corners = new Vector3[4];
        layerRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;

        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera ?? _camera;

        var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, center);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(previewRect, screenPoint, cam, out var local))
            return false;

        var rect = previewRect.rect;
        if (rect.width <= 0.0001f || rect.height <= 0.0001f)
            return false;

        float u = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
        float v = Mathf.InverseLerp(rect.yMin, rect.yMax, local.y);
        uv = new Vector2(u, v);
        return true;
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

        // Получаем мировые углы слоя и переводим в локальные координаты превью.
        Vector3[] corners = new Vector3[4];
        layerRect.GetWorldCorners(corners);

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                previewRect,
                RectTransformUtility.WorldToScreenPoint(cam, corners[0]),
                cam,
                out var bl)) // bottom-left
            return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                previewRect,
                RectTransformUtility.WorldToScreenPoint(cam, corners[2]),
                cam,
                out var tr)) // top-right
            return false;

        var rect = previewRect.rect;
        if (rect.width <= 0.0001f || rect.height <= 0.0001f)
            return false;

        float xMin = Mathf.Max(bl.x, rect.xMin);
        float xMax = Mathf.Min(tr.x, rect.xMax);
        float yMin = Mathf.Max(bl.y, rect.yMin);
        float yMax = Mathf.Min(tr.y, rect.yMax);

        if (xMax <= xMin || yMax <= yMin)
            return false; // слой полностью вне окна

        float cx = (xMin + xMax) * 0.5f;
        float cy = (yMin + yMax) * 0.5f;

        float w = xMax - xMin;
        float h = yMax - yMin;

        // Нормализация в 0..1 относительно окна превью.
        float uCenter = Mathf.InverseLerp(rect.xMin, rect.xMax, cx);
        float vCenter = Mathf.InverseLerp(rect.yMin, rect.yMax, cy);

        center = new Vector2(uCenter, vCenter);
        size = new Vector2(w / rect.width, h / rect.height);
        return true;
    }

    private Vector3 GetZoneWorldPoint(Vector2 uv)
    {
        float zoneHeight = Mathf.Max(_projectionZone.ZoneHeight, 0.001f);
        float zoneWidth = zoneHeight * Mathf.Max(_projectionZone.CanvasAspect, 0.001f);

        float x = (uv.x - 0.5f) * zoneWidth;
        float y = (uv.y - 0.5f) * zoneHeight;

        // В локальных координатах зоны: X - ширина, Y - высота, Z = 0 плоскость зоны
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
