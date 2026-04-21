using UnityEngine;

/// <summary>
/// Проецирует выделенный декаль с 2D-полотна на кружку пропорционально
/// его положению и размеру на полотне. Использует шейдер MugDecalProjection.
/// </summary>
public class OverallDecalProjector : MonoBehaviour
{
    private enum ProjectionKind
    {
        MugCylinder = 0,
        PosterRect = 1,
        TShirtRect = 2,
        PillowRect = 3,
        ShopperRect = 4
    }

    [Header("Target")]
    [SerializeField] private Renderer _objectRenderer;
    [SerializeField] private int _materialIndex;
    [SerializeField] private ProjectionKind _projectionKind = ProjectionKind.MugCylinder;

    [Header("References")]
    [SerializeField] private PreviewWindowController _previewController;
    [SerializeField] private DecalManager _decalManager;

    [Header("Base")]
    [SerializeField] private Color _baseColor = Color.white;

    [Header("UV Tweaks")]
    [SerializeField, Min(0.01f)] private float _vScale = 1f;

    [Header("Rect Projection Tweaks")]
    [SerializeField, Range(-1f, 1f)] private float _planeFrontSign = 1f;
    [SerializeField, Range(0f, 1f)] private float _rectCurvature = 0f;

    [Header("No-Print Zone (Handle)")]
    [SerializeField, Range(0f, 1f)] private float _noPrintCenterU = 0f;
    [SerializeField, Range(0f, 0.5f)] private float _noPrintHalfU = 0f;
    [SerializeField] private bool _noPrintAtCanvasEdges = true;

    private static readonly int DecalTexId      = Shader.PropertyToID("_DecalTex");
    private static readonly int DecalRectId     = Shader.PropertyToID("_DecalRect");
    private static readonly int DecalRotationId = Shader.PropertyToID("_DecalRotation");
    private static readonly int CylRadiusId     = Shader.PropertyToID("_CylRadius");
    private static readonly int CylHalfHId      = Shader.PropertyToID("_CylHalfH");
    private static readonly int HeightAxisId    = Shader.PropertyToID("_HeightAxis");
    private static readonly int VScaleId        = Shader.PropertyToID("_VScale");
    private static readonly int NoPrintCenterUId = Shader.PropertyToID("_NoPrintCenterU");
    private static readonly int NoPrintHalfUId   = Shader.PropertyToID("_NoPrintHalfU");
    private static readonly int NoPrintAtEdgesId = Shader.PropertyToID("_NoPrintAtEdges");
    private static readonly int PlaneAxisUId     = Shader.PropertyToID("_PlaneAxisU");
    private static readonly int PlaneAxisVId     = Shader.PropertyToID("_PlaneAxisV");
    private static readonly int PlaneAxisNId     = Shader.PropertyToID("_PlaneAxisN");
    private static readonly int PlaneHalfUId     = Shader.PropertyToID("_PlaneHalfU");
    private static readonly int PlaneHalfVId     = Shader.PropertyToID("_PlaneHalfV");
    private static readonly int PlaneCenterId    = Shader.PropertyToID("_PlaneCenterOS");
    private static readonly int PlaneOffsetId    = Shader.PropertyToID("_PlaneOffset");
    private static readonly int FrontOnlyId      = Shader.PropertyToID("_FrontOnly");
    private static readonly int CurvatureId      = Shader.PropertyToID("_Curvature");

    private MaterialPropertyBlock _propBlock;
    private bool _initialized;

    private void Awake()
    {
        if (_decalManager == null)
            _decalManager = FindObjectOfType<DecalManager>();
        if (_previewController == null)
            _previewController = FindObjectOfType<PreviewWindowController>();

        EnsureMaterial();
    }

    private void OnEnable()
    {
        if (_decalManager != null)
        {
            _decalManager.OnDecalSelected += OnDecalSelected;
            _decalManager.OnDecalTransformChanged += OnDecalTransformChanged;
        }
    }

    private void OnDisable()
    {
        if (_decalManager != null)
        {
            _decalManager.OnDecalSelected -= OnDecalSelected;
            _decalManager.OnDecalTransformChanged -= OnDecalTransformChanged;
        }
    }

    private void Start()
    {
        UpdateFromSelectedDecal();
    }

    private void EnsureMaterial()
    {
        if (_objectRenderer == null) return;
        if (_propBlock == null)
            _propBlock = new MaterialPropertyBlock();
        _initialized = true;
    }

    private void ApplyToRenderer()
    {
        if (_objectRenderer == null || _propBlock == null) return;
        _objectRenderer.SetPropertyBlock(_propBlock, _materialIndex);
    }

    private void OnDecalSelected(DecalController decal)
    {
        UpdateFromSelectedDecal();
    }

    private void OnDecalTransformChanged(DecalController decal)
    {
        if (decal == _decalManager?.SelectedDecal)
            UpdateFromSelectedDecal();
    }

    /// <summary>
    /// Обновляет параметры материала по выделенному декалю.
    /// </summary>
    public void UpdateFromSelectedDecal()
    {
        EnsureMaterial();
        if (_propBlock == null) return;

        // Чистим блок, чтобы изменения ModelColorizer по _BaseColor не перетирались
        // "хвостами" предыдущих значений.
        _propBlock.Clear();

        ApplyGeometryForCurrentMode();

        var decal = _decalManager?.SelectedDecal;
        if (decal == null || _previewController == null)
        {
            ClearDecal();
            ApplyToRenderer();
            return;
        }

        var layerRect = _previewController.GetLayerRect(decal);
        var previewRect = _previewController.GetRectTransform();
        var canvas = _previewController.GetCanvas();

        if (layerRect == null || previewRect == null || canvas == null)
        {
            ClearDecal();
            ApplyToRenderer();
            return;
        }

        if (!TryGetDecalRectInCanvasSpace(layerRect, previewRect, canvas, out var center, out var halfSize, out var rotationDeg))
        {
            ClearDecal();
            ApplyToRenderer();
            return;
        }

        var tex = decal.GetTexture();
        if (tex == null)
        {
            ClearDecal();
            ApplyToRenderer();
            return;
        }

        _propBlock.SetTexture(DecalTexId, tex);
        _propBlock.SetVector(DecalRectId, new Vector4(center.x, center.y, halfSize.x, halfSize.y));
        _propBlock.SetFloat(DecalRotationId, rotationDeg);
        // BaseColor ( _BaseColor ) управляется ModelColorizer по выбранной части.
        // Здесь не трогаем, иначе Apply цвета может перетираться.
        ApplyToRenderer();
    }

    private void ApplyGeometryForCurrentMode()
    {
        if (_projectionKind == ProjectionKind.MugCylinder)
        {
            ApplyCylinderGeometry();
            _propBlock.SetFloat(VScaleId, _vScale);
            _propBlock.SetFloat(NoPrintCenterUId, _noPrintCenterU);
            _propBlock.SetFloat(NoPrintHalfUId, _noPrintHalfU);
            _propBlock.SetFloat(NoPrintAtEdgesId, _noPrintAtCanvasEdges ? 1f : 0f);
            return;
        }

        ApplyRectGeometry();

        // Отключаем "ручку" и цилиндрические ограничения в прямоугольных шейдерах.
        _propBlock.SetFloat(NoPrintCenterUId, 0f);
        _propBlock.SetFloat(NoPrintHalfUId, 0f);
        _propBlock.SetFloat(NoPrintAtEdgesId, 0f);
        _propBlock.SetFloat(VScaleId, 1f);
    }

    private void ClearDecal()
    {
        if (_propBlock == null) return;

        _propBlock.SetVector(DecalRectId, new Vector4(0.5f, 0.5f, 0f, 0f));
        // BaseColor управляется ModelColorizer по выбранной части.
    }

    private void ApplyCylinderGeometry()
    {
        if (_propBlock == null || _objectRenderer == null) return;

        var mf = _objectRenderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds;
            // Определяем ось высоты как самую большую по extents.
            // 0=X, 1=Y, 2=Z
            int axis = 1;
            float ex = Mathf.Abs(b.extents.x);
            float ey = Mathf.Abs(b.extents.y);
            float ez = Mathf.Abs(b.extents.z);
            if (ex >= ey && ex >= ez) axis = 0;
            else if (ez >= ex && ez >= ey) axis = 2;

            float halfH;
            float r;
            if (axis == 0)
            {
                halfH = ex;
                r = Mathf.Max(ey, ez);
            }
            else if (axis == 1)
            {
                halfH = ey;
                r = Mathf.Max(ex, ez);
            }
            else
            {
                halfH = ez;
                r = Mathf.Max(ex, ey);
            }

            _propBlock.SetFloat(CylRadiusId, Mathf.Max(r, 0.01f));
            _propBlock.SetFloat(CylHalfHId, Mathf.Max(halfH, 0.01f));
            _propBlock.SetFloat(HeightAxisId, axis);
        }
        else
        {
            _propBlock.SetFloat(CylRadiusId, 0.5f);
            _propBlock.SetFloat(CylHalfHId, 1f);
            _propBlock.SetFloat(HeightAxisId, 1f);
        }
    }

    private void ApplyRectGeometry()
    {
        if (_propBlock == null || _objectRenderer == null) return;

        var mf = _objectRenderer.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var b = mf.sharedMesh.bounds;
            float ex = Mathf.Abs(b.extents.x);
            float ey = Mathf.Abs(b.extents.y);
            float ez = Mathf.Abs(b.extents.z);

            int axisN = 0;
            float minExtent = ex;
            if (ey < minExtent) { minExtent = ey; axisN = 1; }
            if (ez < minExtent) { axisN = 2; }

            int axisU;
            int axisV;
            PickRectAxes(axisN, ex, ey, ez, out axisU, out axisV);

            float halfU = GetAxisExtent(axisU, ex, ey, ez);
            float halfV = GetAxisExtent(axisV, ex, ey, ez);
            float halfN = Mathf.Max(GetAxisExtent(axisN, ex, ey, ez), 0.001f);

            float centerN = GetAxisValue(axisN, b.center);
            float planeOffset = centerN + Mathf.Sign(_planeFrontSign) * halfN;

            _propBlock.SetFloat(PlaneAxisUId, axisU);
            _propBlock.SetFloat(PlaneAxisVId, axisV);
            _propBlock.SetFloat(PlaneAxisNId, axisN);
            _propBlock.SetFloat(PlaneHalfUId, Mathf.Max(halfU, 0.01f));
            _propBlock.SetFloat(PlaneHalfVId, Mathf.Max(halfV, 0.01f));
            _propBlock.SetVector(PlaneCenterId, b.center);
            _propBlock.SetFloat(PlaneOffsetId, planeOffset);
            _propBlock.SetFloat(FrontOnlyId, 1f);

            float presetCurvature = GetPresetCurvature();
            _propBlock.SetFloat(CurvatureId, Mathf.Max(_rectCurvature, presetCurvature));
        }
        else
        {
            _propBlock.SetFloat(PlaneAxisUId, 0f);
            _propBlock.SetFloat(PlaneAxisVId, 1f);
            _propBlock.SetFloat(PlaneAxisNId, 2f);
            _propBlock.SetFloat(PlaneHalfUId, 0.5f);
            _propBlock.SetFloat(PlaneHalfVId, 0.5f);
            _propBlock.SetVector(PlaneCenterId, Vector4.zero);
            _propBlock.SetFloat(PlaneOffsetId, Mathf.Sign(_planeFrontSign) * 0.01f);
            _propBlock.SetFloat(FrontOnlyId, 1f);
            _propBlock.SetFloat(CurvatureId, GetPresetCurvature());
        }
    }

    private float GetPresetCurvature()
    {
        return _projectionKind == ProjectionKind.PillowRect ? 0.25f : 0f;
    }

    private static void PickRectAxes(int axisN, float ex, float ey, float ez, out int axisU, out int axisV)
    {
        float[] extents = { ex, ey, ez };
        int first = -1;
        int second = -1;
        for (int i = 0; i < 3; i++)
        {
            if (i == axisN) continue;
            if (first < 0) first = i;
            else second = i;
        }

        if (extents[first] >= extents[second])
        {
            axisU = first;
            axisV = second;
        }
        else
        {
            axisU = second;
            axisV = first;
        }
    }

    private static float GetAxisExtent(int axis, float ex, float ey, float ez)
    {
        if (axis == 0) return ex;
        if (axis == 1) return ey;
        return ez;
    }

    private static float GetAxisValue(int axis, Vector3 value)
    {
        if (axis == 0) return value.x;
        if (axis == 1) return value.y;
        return value.z;
    }

    /// <summary>
    /// Вычисляет нормализованный rect декаля (0..1) в пространстве полотна превью.
    /// center, halfSize — в 0..1, rotationDeg — угол в градусах.
    /// </summary>
    private bool TryGetDecalRectInCanvasSpace(
        RectTransform layerRect,
        RectTransform previewRect,
        Canvas canvas,
        out Vector2 center,
        out Vector2 halfSize,
        out float rotationDeg)
    {
        center = new Vector2(0.5f, 0.5f);
        halfSize = Vector2.zero;
        rotationDeg = 0f;

        if (layerRect == null || previewRect == null) return false;

        var rect = previewRect.rect;
        if (rect.width <= 0.0001f || rect.height <= 0.0001f) return false;

        // Углы слоя в локальном пространстве слоя (pivot обычно в центре)
        var r = layerRect.rect;
        Vector3[] localCorners = new Vector3[4]
        {
            new Vector3(r.xMin, r.yMin, 0f),
            new Vector3(r.xMax, r.yMin, 0f),
            new Vector3(r.xMax, r.yMax, 0f),
            new Vector3(r.xMin, r.yMax, 0f)
        };

        float xMin = float.MaxValue, xMax = float.MinValue;
        float yMin = float.MaxValue, yMax = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector3 world = layerRect.TransformPoint(localCorners[i]);
            Vector3 localInPreview = previewRect.InverseTransformPoint(world);
            xMin = Mathf.Min(xMin, localInPreview.x);
            xMax = Mathf.Max(xMax, localInPreview.x);
            yMin = Mathf.Min(yMin, localInPreview.y);
            yMax = Mathf.Max(yMax, localInPreview.y);
        }

        float cx = (xMin + xMax) * 0.5f;
        float cy = (yMin + yMax) * 0.5f;
        float w = xMax - xMin;
        float h = yMax - yMin;

        center = new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, cx),
            Mathf.InverseLerp(rect.yMin, rect.yMax, cy)
        );
        halfSize = new Vector2(
            Mathf.Clamp01((w / rect.width) * 0.5f),
            Mathf.Clamp01((h / rect.height) * 0.5f)
        );
        rotationDeg = layerRect.eulerAngles.z;

        return halfSize.x > 0.0001f && halfSize.y > 0.0001f;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_propBlock != null && _initialized && _objectRenderer != null)
        {
            EnsureMaterial();
            ApplyGeometryForCurrentMode();
            ApplyToRenderer();
        }
    }
#endif
}
