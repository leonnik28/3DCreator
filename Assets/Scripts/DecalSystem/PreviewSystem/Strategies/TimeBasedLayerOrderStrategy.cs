using System.Collections.Generic;
using System.Linq;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Strategies
{
    /// <summary>
    /// Стратегия сортировки слоёв по времени создания
    /// </summary>
    public class TimeBasedLayerOrderStrategy : ILayerOrderStrategy
    {
        public void ApplyOrder(IEnumerable<IDecalLayer> layers)
        {
            var sortedLayers = layers
                .Where(l => l?.SourceDecal != null)
                .OrderBy(l => l.SourceDecal.CreationTime)
                .ToList();

            for (int i = 0; i < sortedLayers.Count; i++)
            {
                sortedLayers[i].transform.SetSiblingIndex(i);
            }
        }
    }
}