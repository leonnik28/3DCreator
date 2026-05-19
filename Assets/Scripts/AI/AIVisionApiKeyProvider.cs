using UnityEngine;

namespace Fotocentr.AI
{
    public class AIVisionApiKeyProvider : MonoBehaviour
    {
        [Header("Secrets")]
        [SerializeField] private string _apiKey;

        [Header("OpenAI-compatible Endpoint")]
        [SerializeField] private string _endpointUrl = "https://api.mistral.ai/v1/chat/completions";

        [SerializeField] private string _model = "pixtral-12b-2409";

        [Header("Retry / Fallback (optional)")]
        [SerializeField] private bool _autoRetryOnLocationError = true;
        [SerializeField] private string _fallbackModel = "pixtral-large-latest";

        [Header("System Prompt")]
        [SerializeField] [TextArea(3, 6)]
        private string _systemPrompt =
            "You are a helpful assistant. Use the provided image as context when available. Respond with clear, copyable text.";

        public string ApiKey => _apiKey;
        public string EndpointUrl => _endpointUrl;
        public string Model => _model;
        public bool AutoRetryOnLocationError => _autoRetryOnLocationError;
        public string FallbackModel => _fallbackModel;
        public string SystemPrompt => _systemPrompt;
    }
}

