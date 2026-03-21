using UnityEngine;
using PreviewSystem.Interfaces;

namespace PreviewSystem.Services
{
    /// <summary>
    /// Реализация сервиса удаления декалей
    /// </summary>
    public class DecalRemovalService : IDecalRemovalService
    {
        private readonly DecalManager _decalManager;
        private readonly IDecalEditor _editor;

        public DecalRemovalService(DecalManager decalManager, IDecalEditor editor)
        {
            _decalManager = decalManager ?? throw new System.ArgumentNullException(nameof(decalManager));
            _editor = editor ?? throw new System.ArgumentNullException(nameof(editor));
        }

        public void DeleteSelected()
        {
            var activeDecal = _editor.GetActiveDecal();
            if (activeDecal != null)
            {
                _decalManager.DeleteDecal(activeDecal);
            }
        }

        public bool CanDeleteSelected()
        {
            return _editor.GetActiveDecal() != null;
        }
    }
}