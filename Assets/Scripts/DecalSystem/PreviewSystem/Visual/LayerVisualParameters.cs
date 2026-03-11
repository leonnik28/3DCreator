using UnityEngine;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Visual
{
    /// <summary>
    /// Реализация параметров визуального оформления
    /// </summary>
    [System.Serializable]
    public class LayerVisualParameters : ILayerVisualParameters
    {
        [SerializeField] private Color _outlineColor = new Color(1f, 0.8f, 0f, 1f);
        [SerializeField] private Color _dimColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private float _outlineWidth = 0.02f;

        public Color OutlineColor
        {
            get => _outlineColor;
            set => _outlineColor = value;
        }

        public Color DimColor
        {
            get => _dimColor;
            set => _dimColor = value;
        }

        public float OutlineWidth
        {
            get => _outlineWidth;
            set => _outlineWidth = Mathf.Clamp01(value);
        }
    }
}