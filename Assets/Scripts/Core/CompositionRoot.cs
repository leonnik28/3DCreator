using UnityEngine;
using Fotocentr.Core;

namespace Fotocentr.Core
{
    /// <summary>
    /// Точка входа зависимостей. Собирает сервисы и предоставляет их потребителям.
    /// Заменяет FindObjectOfType на явное связывание (Dependency Injection).
    /// Добавьте на сцену и назначьте DecalEditPanel в Consumers.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class CompositionRoot : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private DecalManager _decalManager;
        [SerializeField] private SceneCaptureService _sceneCapture;

        [Header("Consumers (optional - auto-inject)")]
        [SerializeField] private DecalEditPanel _decalEditPanel;

        private void Awake()
        {
            ResolveIfNull();

            if (_decalEditPanel != null)
                _decalEditPanel.Inject(_decalManager, _sceneCapture);
        }

        private void ResolveIfNull()
        {
            if (_decalManager == null)
                _decalManager = FindObjectOfType<DecalManager>();

            if (_sceneCapture == null)
                _sceneCapture = FindObjectOfType<SceneCaptureService>();
        }
    }
}
