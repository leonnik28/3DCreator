using UnityEngine;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Visual
{
    /// <summary>
    /// Стратегия визуального оформления на основе шейдера
    /// </summary>
    public class ShaderBasedVisualStrategy : IDecalLayerVisualStrategy
    {
        private static readonly int SelectedProperty = Shader.PropertyToID("_Selected");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");
        private static readonly int DimColorProperty = Shader.PropertyToID("_DimColor");

        private readonly ILayerVisualParameters _defaultParameters;
        private readonly MaterialFactory _materialFactory;

        public ShaderBasedVisualStrategy(ILayerVisualParameters defaultParameters)
        {
            _defaultParameters = defaultParameters;
            _materialFactory = new MaterialFactory();
        }

        public void Initialize(IDecalLayer layer)
        {
            if (layer is UIDecalLayer uiLayer)
            {
                uiLayer.SetVisualParameters(_defaultParameters);
            }
        }

        public void ApplySelection(IDecalLayer layer, bool selected)
        {
            if (layer is UIDecalLayer uiLayer)
            {
                uiLayer.SetSelected(selected);
            }
        }

        public void UpdateVisual(IDecalLayer layer, ILayerVisualParameters parameters)
        {
            if (layer is UIDecalLayer uiLayer)
            {
                uiLayer.SetVisualParameters(parameters);
            }
        }

        public void Cleanup(IDecalLayer layer)
        {
            // Очистка при необходимости
        }
    }

    /// <summary>
    /// Фабрика материалов
    /// </summary>
    public class MaterialFactory
    {
        private const string ShaderName = "UI/UIDecalLayer";

        public Material CreateMaterial()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Shader {ShaderName} not found! Using default.");
                shader = Shader.Find("UI/Default");
            }

            Material material = new Material(shader);
            material.name = $"UIDecalLayer_Material_{System.Guid.NewGuid()}";
            return material;
        }
    }
}