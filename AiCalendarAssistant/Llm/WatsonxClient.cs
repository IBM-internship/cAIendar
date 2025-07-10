using System.Net.Http.Headers;
using System.Text.Json;
using AiCalendarAssistant.Config;
using AiCalendarAssistant.Infrastructure;
using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Llm;

public class WatsonxClient(HttpClient http, TokenProvider tokens, LlmSettings cfg) : ILlmClient
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<PromptResponse> SendAsync(PromptRequest p, CancellationToken ct = default)
    {
        var url = $"{cfg.Url.TrimEnd('/')}/ml/v1/text/chat?version={cfg.Version}";

        var payload = new Dictionary<string, object?>
        {
            ["model_id"]   = cfg.ModelId,
            ["project_id"] = cfg.ProjectId,
            ["messages"]   = p.Messages.Select(m => new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId }).ToArray()
        };
        if (p.ResponseFormat.HasValue) payload["response_format"] = p.ResponseFormat.Value;
        if (p.Tools.HasValue) { payload["tools"] = p.Tools.Value; payload["tool_choice"] = p.ToolChoice ?? "auto"; }
        if (p.Extra is not null) foreach (var kv in p.Extra) payload[kv.Key] = kv.Value;

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = JsonContent.Create(payload, options: Opts);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetAsync(ct));

        var resp = await http.SendAsync(req, ct);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
		// Console.WriteLine($"Watsonx response: {json}");
        resp.EnsureSuccessStatusCode();

        return CommonJson.ParseResponse(json);
    }
}
