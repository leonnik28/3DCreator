using UnityEngine;
using DecalSystem.Interfaces;

namespace DecalSystem.Validators
{
    /// <summary>
    /// Валидатор состояния окна предпросмотра
    /// </summary>
    public class PreviewWindowValidator : IStateValidator
    {
        private readonly RectTransform _window;
        private readonly RectTransform _imageRect;
        private readonly IDecalEditor _editor;

        public PreviewWindowValidator(
            RectTransform window,
            RectTransform imageRect,
            IDecalEditor editor)
        {
            _window = window;
            _imageRect = imageRect;
            _editor = editor;
        }

        public bool IsValid()
        {
            return _window != null &&
                   _imageRect != null &&
                   _editor?.GetActiveDecal() != null;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid())
                throw new System.InvalidOperationException("Preview window is in invalid state");
        }
    }
}