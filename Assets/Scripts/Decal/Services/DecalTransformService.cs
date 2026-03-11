using UnityEngine;

/// <summary>
/// Сервис, который переводит 2D?координаты на канвасе в позицию/размер/поворот 3D?декали.
/// </summary>
public class DecalTransformService
{
    private readonly Camera _camera;
    private readonly LayerMask _modelLayer;

    public DecalTransformService(Camera camera, LayerMask layerMask)
    {
        _camera = camera;
        _modelLayer = layerMask;
    }

    public void UpdateTransform(
        DecalController decal,
        Vector2 uiPosition,
        Vector2 uiSize,
        float rotation,
        RectTransform canvasRect,
        float uiToWorldScale = 100f)
    {
        if (decal == null || canvasRect == null || _camera == null)
            return;

        // 1. Позиция: UI ? Viewport ? Raycast ? поверхность модели
        var viewportPoint = new Vector2(
            uiPosition.x / canvasRect.rect.width + 0.5f,
            uiPosition.y / canvasRect.rect.height + 0.5f
        );

        var ray = _camera.ViewportPointToRay(viewportPoint);
        if (Physics.Raycast(ray, out var hit, 100f, _modelLayer))
        {
            decal.PlaceOnSurface(hit.point, hit.normal);
        }

        // 2. Размер: ширина UI делится на коэффициент, получаем мировую величину
        var worldSize = uiSize.x / uiToWorldScale;
        decal.SetSize(worldSize);

        // 3. Поворот по оси Z
        decal.transform.eulerAngles = new Vector3(0f, 0f, rotation);
    }
}