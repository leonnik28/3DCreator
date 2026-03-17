using UnityEngine;

/// <summary>
/// Привязывает RenderTexture с 2D‑полотном к материалу кружки
/// с шейдером "Universal Render Pipeline/MugCylindricalCanvas"
/// и настраивает параметры области проекции.
/// </summary>
public class MugCanvasBinder : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer _mugRenderer;
    [SerializeField] private int _materialIndex = 0;

    [Header("Canvas")]
    [SerializeField] private RenderTexture _canvasRT;
    [SerializeField] private Vector2 _canvasTiling = Vector2.one;
    [SerializeField] private Vector2 _canvasOffset = Vector2.zero;

    [Header("Mug Geometry (world)")]
    [SerializeField] private Transform _mugCenter;
    [SerializeField] private float _mugRadius = 0.5f;
    [SerializeField] private float _mugHeight = 1.0f;

    private static readonly int CanvasTexId    = Shader.PropertyToID("_CanvasTex");
    private static readonly int CanvasTilingId = Shader.PropertyToID("_CanvasTiling");
    private static readonly int CanvasOffsetId = Shader.PropertyToID("_CanvasOffset");
    private static readonly int MugCenterWSId  = Shader.PropertyToID("_MugCenterWS");
    private static readonly int MugRadiusId    = Shader.PropertyToID("_MugRadius");
    private static readonly int MugHeightId    = Shader.PropertyToID("_MugHeight");

    private void Awake()
    {
        ApplySettings();
    }

    /// <summary>
    /// Вызывайте после изменения RT или геометрии.
    /// </summary>
    public void ApplySettings()
    {
        if (_mugRenderer == null || _canvasRT == null)
            return;

        var mats = _mugRenderer.materials;
        if (_materialIndex < 0 || _materialIndex >= mats.Length)
            return;

        var mat = mats[_materialIndex];

        mat.SetTexture(CanvasTexId, _canvasRT);
        mat.SetVector(CanvasTilingId, new Vector4(_canvasTiling.x, _canvasTiling.y, 0f, 0f));
        mat.SetVector(CanvasOffsetId, new Vector4(_canvasOffset.x, _canvasOffset.y, 0f, 0f));

        Vector3 center = _mugCenter != null ? _mugCenter.position : _mugRenderer.bounds.center;
        mat.SetVector(MugCenterWSId, new Vector4(center.x, center.y, center.z, 0f));
        mat.SetFloat(MugRadiusId, _mugRadius);
        mat.SetFloat(MugHeightId, _mugHeight);
    }

    public void SetCanvas(RenderTexture rt)
    {
        _canvasRT = rt;
        ApplySettings();
    }
}

