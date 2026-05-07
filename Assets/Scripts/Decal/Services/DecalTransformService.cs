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
                GetProjectionSurfaceBasis(out _, out _, out _, out Vector3 surfaceNormal);
                var ray = new Ray(zoneWorldPoint + surfaceNormal * 0.5f, -surfaceNormal);
                if (Physics.Raycast(ray, out var hit, 5f, _modelLayer))
                    decal.PlaceOnSurface(hit.point, hit.normal);
                else
                    decal.PlaceOnSurface(zoneWorldPoint, surfaceNormal);

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

        GetProjectionSurfaceBasis(out Vector3 surfaceCenter, out Vector3 surfaceRight, out Vector3 surfaceUp, out _);
        return surfaceCenter + surfaceRight * x + surfaceUp * y;
    }

    private void GetProjectionSurfaceBasis(out Vector3 center, out Vector3 right, out Vector3 up, out Vector3 normal)
    {
        center = GetProjectionSurfaceCenterWorld();
        right = Vector3.right;
        up = Vector3.up;
        normal = Vector3.forward;

        if (_projectionZone == null)
            return;

        if (_projectionZone.UseZoneTransformAxes)
        {
            Transform zoneTransform = _projectionZone.transform;
            right = zoneTransform.right;
            up = zoneTransform.up;
            normal = zoneTransform.forward;
            return;
        }

        var renderer = GetProjectionSurfaceRenderer();
        if (renderer == null)
        {
            Transform zoneTransform = _projectionZone.transform;
            right = zoneTransform.right;
            up = zoneTransform.up;
            normal = zoneTransform.forward;
            return;
        }

        Transform rendererTransform = renderer.transform;
        if (!TryGetFlatSurfaceAxes(renderer, out right, out up, out normal))
        {
            right = rendererTransform.right;
            up = rendererTransform.up;
            normal = rendererTransform.forward;
        }
    }

    private Vector3 GetProjectionSurfaceCenterWorld()
    {
        if (_projectionZone == null)
            return Vector3.zero;

        var renderer = GetProjectionSurfaceRenderer();
        if (renderer == null)
            return _projectionZone.transform.TransformPoint(_projectionZone.Offset);

        return renderer.bounds.center + _projectionZone.transform.TransformVector(_projectionZone.Offset);
    }

    private Renderer GetProjectionSurfaceRenderer()
    {
        if (_projectionZone == null)
            return null;

        return _projectionZone.SurfaceRenderer
            ?? _projectionZone.GetComponent<Renderer>()
            ?? _projectionZone.GetComponentInChildren<Renderer>(true);
    }

    private bool TryGetFlatSurfaceAxes(Renderer renderer, out Vector3 right, out Vector3 up, out Vector3 normal)
    {
        right = Vector3.right;
        up = Vector3.up;
        normal = Vector3.forward;

        if (renderer == null)
            return false;

        var meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return false;

        Transform t = renderer.transform;
        Vector3 scaledSize = Vector3.Scale(meshFilter.sharedMesh.bounds.size, AbsVector(t.lossyScale));

        int normalAxis = GetSmallestAxis(scaledSize);
        int firstAxis = (normalAxis + 1) % 3;
        int secondAxis = (normalAxis + 2) % 3;

        Vector3 firstDir = GetTransformAxis(t, firstAxis);
        Vector3 secondDir = GetTransformAxis(t, secondAxis);
        Vector3 referenceUp = _projectionZone != null ? _projectionZone.transform.up : Vector3.up;

        int upAxis = Mathf.Abs(Vector3.Dot(firstDir.normalized, referenceUp.normalized)) >= Mathf.Abs(Vector3.Dot(secondDir.normalized, referenceUp.normalized))
            ? firstAxis
            : secondAxis;
        int rightAxis = upAxis == firstAxis ? secondAxis : firstAxis;

        right = GetTransformAxis(t, rightAxis).normalized;
        up = GetTransformAxis(t, upAxis).normalized;
        if (Vector3.Dot(up, referenceUp) < 0f)
            up = -up;

        Vector3 axisNormal = GetTransformAxis(t, normalAxis).normalized;
        normal = Vector3.Cross(right, up).normalized;
        if (Vector3.Dot(normal, axisNormal) < 0f)
        {
            normal = -normal;
        }

        if (TryGetAverageMeshNormalWorld(renderer, out Vector3 averageNormalWorld) &&
            Vector3.Dot(normal, averageNormalWorld) < 0f)
        {
            normal = -normal;
        }

        return true;
    }

    private static Vector3 GetTransformAxis(Transform transform, int axis)
    {
        if (axis == 0) return transform.right;
        if (axis == 1) return transform.up;
        return transform.forward;
    }

    private static int GetSmallestAxis(Vector3 value)
    {
        if (value.x <= value.y && value.x <= value.z) return 0;
        if (value.y <= value.x && value.y <= value.z) return 1;
        return 2;
    }

    private static Vector3 AbsVector(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    private static bool TryGetAverageMeshNormalWorld(Renderer renderer, out Vector3 averageNormalWorld)
    {
        averageNormalWorld = Vector3.zero;

        var meshFilter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
        var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
        if (mesh == null || mesh.normals == null || mesh.normals.Length == 0)
            return false;

        Vector3 averageNormalOS = Vector3.zero;
        var normals = mesh.normals;
        for (int i = 0; i < normals.Length; i++)
            averageNormalOS += normals[i];

        if (averageNormalOS.sqrMagnitude < 0.000001f)
            return false;

        averageNormalWorld = renderer.transform.TransformDirection(averageNormalOS.normalized).normalized;
        return averageNormalWorld.sqrMagnitude > 0.000001f;
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
