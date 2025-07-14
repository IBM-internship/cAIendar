using System.Net.Http.Headers;
using System.Text.Json;
using AiCalendarAssistant.Config;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Llm;

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
            // throw new InvalidOperationException("Ollama API key is not configured.");
        }else{
			_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.OllamaApiKey);
		}
    }

    public async Task<PromptResponse> SendAsync(PromptRequest p, CancellationToken ct = default, string? additionalInstructions = null)
    {
        var url = $"{_cfg.OllamaUrl.TrimEnd('/')}/v1/chat/completions";

        var payload = new Dictionary<string, object?>
        {
            ["model"]    = _cfg.OllamaModel,
			["messages"] = p.Messages.Select(m =>
{
    // base fields
    var obj = new Dictionary<string, object?>
    {
        ["role"]    = m.Role,
        ["content"] = (m.Role == "system" && !string.IsNullOrWhiteSpace(additionalInstructions))
                      ? m.Content + additionalInstructions
                      : m.Content
    };

    // assistant message containing tool_calls
    if (m.ToolCalls is { Count: > 0 })
        obj["tool_calls"] = m.ToolCalls.Select(tc => new
        {
            id       = tc.Id,
            type     = "function",
            function = new
            {
                name      = tc.Name,
                arguments = tc.Arguments
            }
        });

    // tool reply
    if (!string.IsNullOrWhiteSpace(m.ToolCallId))
        obj["tool_call_id"] = m.ToolCallId;

    return obj;
}).ToArray(),

            // ["messages"] = p.Messages.Select(m =>
            // {
            //     // If this is the system message and additionalInstructions is provided, append it
            //     if (m.Role == "system" && !string.IsNullOrWhiteSpace(additionalInstructions))
            //     {
            //         return new { role = m.Role, content = m.Content + additionalInstructions };
            //     }
            //     return new { role = m.Role, content = m.Content };
            // }).ToArray()
        };
        Console.WriteLine($"Payload messages: {JsonSerializer.Serialize(payload["messages"], Opts)}");
        // here can we add the additionalInstructions string to the system message. Just append it
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
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        Console.WriteLine($"Ollama response: {json}");
        resp.EnsureSuccessStatusCode();

        return CommonJson.ParseResponse(json);
    }
}
