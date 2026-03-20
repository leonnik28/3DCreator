using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Fotocentr.AI
{
    /// <summary>
    /// OpenAI-compatible Vision chat client.
    /// Подходит не только для OpenAI: многие провайдеры держат OpenAI-compatible формат /v1/chat/completions.
    /// </summary>
    public class OpenAIVisionClient
    {
        private readonly MonoBehaviour _runner;

        public OpenAIVisionClient(MonoBehaviour runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public void SendImageAndPrompt(
            byte[] imagePngBytes,
            string prompt,
            string apiKey,
            string endpointUrl,
            string model,
            string systemPrompt,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (imagePngBytes == null || imagePngBytes.Length == 0)
            {
                onError?.Invoke("Image bytes are empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                onError?.Invoke("Prompt is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke("API key is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                onError?.Invoke("Endpoint URL is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                onError?.Invoke("Model is empty.");
                return;
            }

            _runner.StartCoroutine(
                SendCoroutine(
                    imagePngBytes,
                    prompt,
                    apiKey,
                    endpointUrl,
                    model,
                    systemPrompt,
                    onSuccess,
                    onError
                )
            );
        }

        public void SendPrompt(
            string prompt,
            string apiKey,
            string endpointUrl,
            string model,
            string systemPrompt,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                onError?.Invoke("Prompt is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke("API key is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                onError?.Invoke("Endpoint URL is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                onError?.Invoke("Model is empty.");
                return;
            }

            _runner.StartCoroutine(
                SendTextCoroutine(
                    prompt,
                    apiKey,
                    endpointUrl,
                    model,
                    systemPrompt,
                    onSuccess,
                    onError
                )
            );
        }

        private IEnumerator SendCoroutine(
            byte[] imagePngBytes,
            string prompt,
            string apiKey,
            string endpointUrl,
            string model,
            string systemPrompt,
            Action<string> onSuccess,
            Action<string> onError)
        {
            string base64 = Convert.ToBase64String(imagePngBytes);
            string dataUrl = "data:image/png;base64," + base64;

            // OpenAI / OpenAI-compatible формат:
            // messages: [{ role: "system", content: "..." }, { role:"user", content:[ {type:"text"...}, {type:"image_url"...} ] }]
            string json = BuildPayloadJson(prompt, systemPrompt, model, dataUrl);

            using var req = new UnityWebRequest(endpointUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");

            // OpenAI compatible authorization header.
            req.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string body = req.downloadHandler != null ? req.downloadHandler.text : null;
                string snippet = TrimForLog(body, 2000);
                string hint = "";
                bool isLocationBlocked = false;
                if (!string.IsNullOrWhiteSpace(body) && body.IndexOf("User location is not supported", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isLocationBlocked = true;
                    hint =
                        "\nHint: provider blocked your region. " +
                        "In OpenRouter, avoid the random router `openrouter/free` and set a specific free multimodal model instead.";
                }
                Debug.LogError("[AI] HTTP error: " + req.responseCode + ". " + req.error + hint + (string.IsNullOrWhiteSpace(snippet) ? "" : "\nBody snippet:\n" + snippet));
                onError?.Invoke(
                    isLocationBlocked
                        ? $"HTTP error: {req.responseCode}. {req.error}. User location is not supported"
                        : $"HTTP error: {req.responseCode}. {req.error}"
                );
                yield break;
            }

            string responseText = req.downloadHandler.text;
            try
            {
                string content = ExtractContent(responseText);
                if (string.IsNullOrWhiteSpace(content))
                {
                    onError?.Invoke("AI response is empty.");
                    yield break;
                }

                onSuccess?.Invoke(content);
            }
            catch (Exception e)
            {
                onError?.Invoke("Failed to parse AI response: " + e.Message);
            }
        }

        private IEnumerator SendTextCoroutine(
            string prompt,
            string apiKey,
            string endpointUrl,
            string model,
            string systemPrompt,
            Action<string> onSuccess,
            Action<string> onError)
        {
            // OpenAI / OpenAI-compatible формат:
            // messages: [{ role: "system", content: "..." }, { role:"user", content: "..." }]
            string json = BuildTextPayloadJson(prompt, systemPrompt, model);

            using var req = new UnityWebRequest(endpointUrl, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", "Bearer " + apiKey.Trim());

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                string body = req.downloadHandler != null ? req.downloadHandler.text : null;
                string snippet = TrimForLog(body, 2000);
                string hint = "";
                bool isLocationBlocked = false;
                if (!string.IsNullOrWhiteSpace(body) && body.IndexOf("User location is not supported", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    isLocationBlocked = true;
                    hint =
                        "\nHint: provider blocked your region. " +
                        "In OpenRouter, avoid the random router `openrouter/free` and set a specific free multimodal model instead.";
                }
                Debug.LogError("[AI] HTTP error: " + req.responseCode + ". " + req.error + hint + (string.IsNullOrWhiteSpace(snippet) ? "" : "\nBody snippet:\n" + snippet));
                onError?.Invoke(
                    isLocationBlocked
                        ? $"HTTP error: {req.responseCode}. {req.error}. User location is not supported"
                        : $"HTTP error: {req.responseCode}. {req.error}"
                );
                yield break;
            }

            string responseText = req.downloadHandler.text;
            try
            {
                string content = ExtractContent(responseText);
                if (string.IsNullOrWhiteSpace(content))
                {
                    onError?.Invoke("AI response is empty.");
                    yield break;
                }

                onSuccess?.Invoke(content);
            }
            catch (Exception e)
            {
                onError?.Invoke("Failed to parse AI response: " + e.Message);
            }
        }

        private static string BuildPayloadJson(string prompt, string systemPrompt, string model, string dataUrl)
        {
            string escPrompt = EscapeJsonString(prompt);
            string escModel = EscapeJsonString(model);
            string escDataUrl = EscapeJsonString(dataUrl);

            string userMessage =
                "{"
                + "\"role\":\"user\","
                + "\"content\":["
                + "{\"type\":\"text\",\"text\":\"" + escPrompt + "\"},"
                + "{\"type\":\"image_url\",\"image_url\":{\"url\":\"" + escDataUrl + "\"}}"
                + "]"
                + "}";

            string messages;
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages = "[" + userMessage + "]";
            }
            else
            {
                string escSystem = EscapeJsonString(systemPrompt);
                string systemMessage =
                    "{"
                    + "\"role\":\"system\","
                    + "\"content\":\"" + escSystem + "\""
                    + "}";
                messages = "[" + systemMessage + "," + userMessage + "]";
            }

            // temperature: число, не строка.
            return "{"
                   + "\"model\":\"" + escModel + "\","
                   + "\"temperature\":0.2,"
                   + "\"messages\":" + messages
                   + "}";
        }

        private static string BuildTextPayloadJson(string prompt, string systemPrompt, string model)
        {
            string escPrompt = EscapeJsonString(prompt);
            string escModel = EscapeJsonString(model);

            string userMessage = "{"
                                  + "\"role\":\"user\","
                                  + "\"content\":\"" + escPrompt + "\""
                                  + "}";

            string messages;
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                messages = "[" + userMessage + "]";
            }
            else
            {
                string escSystem = EscapeJsonString(systemPrompt);
                string systemMessage = "{"
                                        + "\"role\":\"system\","
                                        + "\"content\":\"" + escSystem + "\""
                                        + "}";
                messages = "[" + systemMessage + "," + userMessage + "]";
            }

            return "{"
                   + "\"model\":\"" + escModel + "\","
                   + "\"temperature\":0.2,"
                   + "\"messages\":" + messages
                   + "}";
        }

        private static string EscapeJsonString(string s)
        {
            if (s == null) return string.Empty;

            // Базовое экранирование для корректного JSON.
            var sb = new StringBuilder(s.Length + 16);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            sb.Append("\\u" + ((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static string ExtractContent(string responseText)
        {
            // Unity 2021.x иногда урезает System.Text.Json (например, JsonDocument может быть недоступен),
            // поэтому парсим через DataContractJsonSerializer.
            try
            {
                var bytes = Encoding.UTF8.GetBytes(responseText);
                using var ms = new MemoryStream(bytes);
                var serializer = new DataContractJsonSerializer(typeof(OpenAIChatCompletionResponse));
                var obj = serializer.ReadObject(ms) as OpenAIChatCompletionResponse;

                if (obj?.choices != null && obj.choices.Length > 0)
                {
                    var first = obj.choices[0];
                    if (first?.message != null && !string.IsNullOrWhiteSpace(first.message.content))
                        return first.message.content;

                    if (!string.IsNullOrWhiteSpace(first?.text))
                        return first.text;
                }
            }
            catch
            {
                // fall through to a very small fallback below.
            }

            // Fallback: более надёжное извлечение JSON string значения (учитывает экранирование).
            // Ограничиваем поиск областью choices, чтобы не цеплять другие поля "content".
            try
            {
                int choicesIdx = responseText.IndexOf("\"choices\"", StringComparison.OrdinalIgnoreCase);
                string searchArea = choicesIdx >= 0 && choicesIdx + 1 < responseText.Length
                    ? responseText.Substring(choicesIdx)
                    : responseText;

                string content = TryExtractJsonStringValue(searchArea, "\"content\"");
                if (!string.IsNullOrWhiteSpace(content))
                    return content;

                string text = TryExtractJsonStringValue(searchArea, "\"text\"");
                if (!string.IsNullOrWhiteSpace(text))
                    return text;

                // OpenRouter/провайдер может вернуть {"error": {"message": "..."}}
                string errorMessage = TryExtractErrorMessage(responseText);
                if (!string.IsNullOrWhiteSpace(errorMessage))
                    return errorMessage;
            }
            catch
            {
                // ignore
            }

            string snippet = TrimForLog(responseText, 4000);
            Debug.LogError("[AI] Can't find content in response JSON. Snippet:\n" + snippet);
            throw new InvalidOperationException("Can't find content in response JSON.");
        }

        private static string TryExtractErrorMessage(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText))
                return null;

            int errorIdx = responseText.IndexOf("\"error\"", StringComparison.OrdinalIgnoreCase);
            if (errorIdx < 0 || errorIdx + 1 >= responseText.Length)
                return null;

            string area = responseText.Substring(errorIdx);
            return TryExtractJsonStringValue(area, "\"message\"");
        }

        private static string TryExtractJsonStringValue(string json, string quotedKey)
        {
            if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(quotedKey))
                return null;

            int keyIdx = json.IndexOf(quotedKey, StringComparison.OrdinalIgnoreCase);
            if (keyIdx < 0) return null;

            int colon = json.IndexOf(':', keyIdx + quotedKey.Length);
            if (colon < 0) return null;

            int i = colon + 1;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;

            if (i >= json.Length || json[i] != '"')
                return null;

            // Parse JSON string starting after the opening quote.
            i++; // skip opening quote
            var sb = new StringBuilder(128);

            while (i < json.Length)
            {
                char c = json[i];
                if (c == '"')
                {
                    // closing quote (not escaped)
                    return sb.ToString();
                }

                if (c == '\\')
                {
                    if (i + 1 >= json.Length) break;
                    char esc = json[i + 1];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); i += 2; break;
                        case '\\': sb.Append('\\'); i += 2; break;
                        case '/': sb.Append('/'); i += 2; break;
                        case 'b': sb.Append('\b'); i += 2; break;
                        case 'f': sb.Append('\f'); i += 2; break;
                        case 'n': sb.Append('\n'); i += 2; break;
                        case 'r': sb.Append('\r'); i += 2; break;
                        case 't': sb.Append('\t'); i += 2; break;
                        case 'u':
                        {
                            if (i + 5 >= json.Length) { i = json.Length; break; }
                            string hex = json.Substring(i + 2, 4);
                            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int code))
                                sb.Append((char)code);
                            i += 6;
                            break;
                        }
                        default:
                            // Unknown escape sequence, best-effort: append char as-is.
                            sb.Append(esc);
                            i += 2;
                            break;
                    }
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return null;
        }

        private static string TrimForLog(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length <= maxLen) return s;
            return s.Substring(0, maxLen) + "...(truncated)";
        }

        [DataContract]
        private class OpenAIChatCompletionResponse
        {
            [DataMember(Name = "choices")]
            public OpenAIChoice[] choices;
        }

        [DataContract]
        private class OpenAIChoice
        {
            [DataMember(Name = "message")]
            public OpenAIMessage message;

            [DataMember(Name = "text")]
            public string text;
        }

        [DataContract]
        private class OpenAIMessage
        {
            [DataMember(Name = "content")]
            public string content;
        }
    }
}

