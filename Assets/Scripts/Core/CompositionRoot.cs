using UnityEngine;
using Fotocentr.Core;

namespace Fotocentr.Core
{
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
