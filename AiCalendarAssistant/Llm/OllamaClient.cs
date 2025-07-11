using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PromptingPipeline.Config;
using PromptingPipeline.Models;

namespace PromptingPipeline.Llm;

public class OllamaClient : ILlmClient
{
    private readonly HttpClient  _http;
    private readonly LlmSettings _cfg;

    private static readonly JsonSerializerOptions Opts = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaClient(HttpClient http, IOptions<LlmSettings> cfg)
    {
        _cfg = cfg.Value;
        _http = http;

        // Add the API key header for each request
        if (!string.IsNullOrWhiteSpace(_cfg.OllamaApiKey))
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _cfg.OllamaApiKey);
        }
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
        return CommonJson.ParseResponse(json);
    }
}

