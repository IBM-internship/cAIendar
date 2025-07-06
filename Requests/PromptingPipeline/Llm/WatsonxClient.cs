using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PromptingPipeline.Config;
using PromptingPipeline.Infrastructure;
using PromptingPipeline.Models;

namespace PromptingPipeline.Llm;

internal sealed class WatsonxClient : ILlmClient
{
    private readonly HttpClient    _http;
    private readonly TokenProvider _tokens;
    private readonly LlmSettings   _cfg;

    private static readonly JsonSerializerOptions Opts = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public WatsonxClient(HttpClient http, TokenProvider tokens, IOptions<LlmSettings> cfg)
        => (_http, _tokens, _cfg) = (http, tokens, cfg.Value);

    public async Task<PromptResponse> SendAsync(PromptRequest p, CancellationToken ct = default)
    {
        var url = $"{_cfg.Url.TrimEnd('/')}/ml/v1/text/chat?version={_cfg.Version}";

        var payload = new Dictionary<string, object?>
        {
            ["model_id"]   = _cfg.ModelId,
            ["project_id"] = _cfg.ProjectId,
            ["messages"]   = p.Messages.Select(m => new { role = m.Role, content = m.Content, tool_call_id = m.ToolCallId }).ToArray()
        };
        if (p.ResponseFormat.HasValue) payload["response_format"] = p.ResponseFormat.Value;
        if (p.Tools.HasValue) { payload["tools"] = p.Tools.Value; payload["tool_choice"] = p.ToolChoice ?? "auto"; }
        if (p.Extra is not null) foreach (var kv in p.Extra) payload[kv.Key] = kv.Value;

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: Opts)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetAsync(ct));

        var resp = await _http.SendAsync(req, ct);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
		Console.WriteLine($"Watsonx response: {json}");
        resp.EnsureSuccessStatusCode();

        return CommonJson.ParseResponse(json);
    }
}
