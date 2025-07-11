using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AiCalendarAssistant.Config;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Llm
{
    public class OllamaClient : ILlmClient
    {
        private readonly HttpClient _http;
        private readonly LlmSettings _cfg;
        private static readonly JsonSerializerOptions Opts = new()
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        public OllamaClient(HttpClient http, LlmSettings cfg)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));

            // Add API Key header
            if (string.IsNullOrWhiteSpace(_cfg.OllamaApiKey))
            {
                throw new InvalidOperationException("Ollama API key is not configured.");
            }
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.OllamaApiKey);
        }

        public async Task<PromptResponse> SendAsync(PromptRequest p, CancellationToken ct = default)
        {
            var url = $"{_cfg.OllamaUrl.TrimEnd('/')}/v1/chat/completions";

            var payload = new Dictionary<string, object?>
            {
                ["model"]    = _cfg.OllamaModel,
                ["messages"] = p.Messages.Select(m => new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId }).ToArray()
            };
            if (p.ResponseFormat.HasValue) payload["response_format"] = p.ResponseFormat.Value;
            if (p.Tools.HasValue)
            {
                payload["tools"] = p.Tools.Value;
                payload["tool_choice"] = p.ToolChoice ?? "auto";
            }

            if (p.Extra is not null)
            {
                foreach (var kv in p.Extra)
                {
                    payload[kv.Key] = kv.Value;
                }
            }

            var resp = await _http.PostAsJsonAsync(url, payload, Opts, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
            Console.WriteLine($"Ollama response: {json}");
            return CommonJson.ParseResponse(json);
        }
    }
}

