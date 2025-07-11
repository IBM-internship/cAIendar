using System.Text.Json;
using AiCalendarAssistant.Config;
using AiCalendarAssistant.Interfaces;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Llm;

public class OllamaClient(HttpClient http, LlmSettings cfg) : ILlmClient
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<PromptResponse> SendAsync(PromptRequest p, CancellationToken ct = default)
    {
        var url = $"{cfg.OllamaUrl.TrimEnd('/')}/v1/chat/completions";

        var payload = new Dictionary<string, object?>
        {
            ["model"]    = cfg.OllamaModel,
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

        var resp = await http.PostAsJsonAsync(url, payload, Opts, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        Console.WriteLine($"Ollama response: {json}");
        return CommonJson.ParseResponse(json);
    }
}
