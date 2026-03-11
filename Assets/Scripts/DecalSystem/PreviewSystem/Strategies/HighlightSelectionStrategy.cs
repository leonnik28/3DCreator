using System.Collections.Generic;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Strategies
{
    /// <summary>
    /// Стратегия выделения слоя с подсветкой
    /// </summary>
    public class HighlightSelectionStrategy : ISelectionStrategy
    {
        private readonly IDecalLayerVisualStrategy _visualStrategy;

        public HighlightSelectionStrategy(IDecalLayerVisualStrategy visualStrategy)
        {
            _visualStrategy = visualStrategy;
        }

        public void ApplySelection(IEnumerable<IDecalLayer> layers, DecalController selected)
        {
            foreach (var layer in layers)
            {
                if (layer == null) continue;

                bool isSelected = layer.SourceDecal == selected;
                _visualStrategy.ApplySelection(layer, isSelected);

                if (isSelected)
                {
                    layer.transform.SetAsLastSibling();
                }
            }
        }
    }
}