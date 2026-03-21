using UnityEngine;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Visual
{
    /// <summary>
    /// ��������� ����������� ���������� �� ������ �������
    /// </summary>
    public class ShaderBasedVisualStrategy : IDecalLayerVisualStrategy
    {
        private static readonly int SelectedProperty = Shader.PropertyToID("_Selected");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");
        private static readonly int DimColorProperty = Shader.PropertyToID("_DimColor");

        private readonly ILayerVisualParameters _defaultParameters;

        public ShaderBasedVisualStrategy(ILayerVisualParameters defaultParameters)
        {
            _defaultParameters = defaultParameters;
        }

        public void Initialize(IDecalLayer layer)
        {
            layer.SetVisualParameters(_defaultParameters);
        }

        public void ApplySelection(IDecalLayer layer, bool selected)
        {
            layer.SetSelected(selected);
        }

        public void UpdateVisual(IDecalLayer layer, ILayerVisualParameters parameters)
        {
            layer.SetVisualParameters(parameters);
        }

        public void Cleanup(IDecalLayer layer)
        {
            // ������� ��� �������������
        }
    }

}