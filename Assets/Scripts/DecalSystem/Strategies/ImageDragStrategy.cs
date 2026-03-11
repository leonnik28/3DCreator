using UnityEngine;
using DecalSystem.Interfaces;
using System;

namespace DecalSystem.Strategies
{
    /// <summary>
    /// —тратеги€ перетаскивани€ изображени€
    /// </summary>
    public class ImageDragStrategy : IDragHandlerStrategy
    {
        private readonly RectTransform _imageRect;
        private readonly IStateValidator _validator;
        private readonly Action _onTransformChanged;

        private Vector2 _dragStartImagePosition;
        private bool _isDragging;

        public ImageDragStrategy(
            RectTransform imageRect,
            IStateValidator validator,
            Action onTransformChanged)
        {
            _imageRect = imageRect ?? throw new ArgumentNullException(nameof(imageRect));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _onTransformChanged = onTransformChanged;
        }

        public void OnBeginDrag(Vector2 startPosition)
        {
            if (!_validator.IsValid()) return;

            _isDragging = true;
            _dragStartImagePosition = _imageRect.anchoredPosition;
        }

        public void OnDrag(Vector2 currentPosition, Vector2 startPosition)
        {
            if (!_isDragging || !_validator.IsValid()) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _imageRect.parent as RectTransform,
                currentPosition,
                null,
                out Vector2 localCurrent))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _imageRect.parent as RectTransform,
                    startPosition,
                    null,
                    out Vector2 localStart))
                {
                    Vector2 delta = localCurrent - localStart;
                    _imageRect.anchoredPosition = _dragStartImagePosition + delta;
                    _onTransformChanged?.Invoke();
                }
            }
        }

        public void OnEndDrag()
        {
            _isDragging = false;
        }
    }
}