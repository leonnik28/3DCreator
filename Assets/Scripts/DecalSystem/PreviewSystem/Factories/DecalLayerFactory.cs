using UnityEngine;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Factories
{
    /// <summary>
    /// Фабрика для создания слоёв декалей
    /// </summary>
    public class DecalLayerFactory : IDecalLayerFactory
    {
        private readonly GameObject _layerPrefab;
        private readonly Transform _container;
        private readonly IDecalLayerVisualStrategy _visualStrategy;
        private readonly ILayerVisualParameters _defaultParameters;

        public DecalLayerFactory(
            GameObject layerPrefab,
            Transform container,
            IDecalLayerVisualStrategy visualStrategy,
            ILayerVisualParameters defaultParameters)
        {
            _layerPrefab = layerPrefab;
            _container = container;
            _visualStrategy = visualStrategy;
            _defaultParameters = defaultParameters;
        }

        public IDecalLayer Create(DecalController decal, RectTransform parentRect)
        {
            if (decal == null) return null;

            GameObject layerObj = Object.Instantiate(_layerPrefab, _container);
            var layer = layerObj.GetComponent<UIDecalLayer>();

            if (layer == null)
            {
                Debug.LogError("Layer prefab must have UIDecalLayer component!");
                Object.Destroy(layerObj);
                return null;
            }

            layer.Initialize(decal, parentRect);
            layer.SetVisualParameters(_defaultParameters);
            _visualStrategy.Initialize(layer);

            return layer;
        }

        public void Destroy(IDecalLayer layer)
        {
            if (layer?.gameObject != null)
            {
                _visualStrategy.Cleanup(layer);
                Object.Destroy(layer.gameObject);
            }
        }
    }
}