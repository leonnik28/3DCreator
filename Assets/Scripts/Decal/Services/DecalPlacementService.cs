using UnityEngine;

public class DecalPlacementService
{
    private Camera _mainCamera;
    private LayerMask _modelLayer;
    private bool _isPlacingMode = false;
    private Texture2D _pendingTexture;

    public bool IsPlacingMode => _isPlacingMode;
    public Texture2D PendingTexture => _pendingTexture;

    public DecalPlacementService(Camera camera, LayerMask layerMask)
    {
        _mainCamera = camera;
        _modelLayer = layerMask;
    }

    public void StartPlacing(Texture2D texture)
    {
        _isPlacingMode = true;
        _pendingTexture = texture;
    }

    public void CancelPlacing()
    {
        _isPlacingMode = false;
        _pendingTexture = null;
    }

    public bool TryGetPlacementPoint(Vector2 screenPosition, out RaycastHit hitInfo)
    {
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        return Physics.Raycast(ray, out hitInfo, 100f, _modelLayer);
    }
}