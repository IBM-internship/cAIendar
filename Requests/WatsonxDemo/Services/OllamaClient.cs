// Services/OllamaClient.cs
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Linq;
using WatsonxDemo.Models;

namespace WatsonxDemo.Services;

internal sealed class OllamaClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly WatsonxSettings _cfg;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public OllamaClient(HttpClient http, IOptions<WatsonxSettings> cfg)
        => (_http, _cfg) = (http, cfg.Value);

    public async Task<string?> ChatAsync(ChatRequest request,
                                         CancellationToken ct = default)
    {
        var baseUrl = _cfg.OllamaUrl.TrimEnd('/');
        var url     = $"{baseUrl}/v1/chat/completions";          // OpenAI-style :contentReference[oaicite:1]{index=1}

	var tools = request.Tools ?? request.Functions?.Select(fn => new {
					  type = "function",
					  function = new { fn.Name, fn.Description, parameters = fn.Parameters }
				   });

		var reqBody = new
		{
			model = _cfg.OllamaModel,
            messages = request.Messages.Select(m => new
            {
                role    = m.Role,
                content = m.Content.First().Text
            }).ToArray()
			tools = tools?.Any() == true ? tools : null,
			tool_choice = request.ToolChoice ?? request.FunctionCall ?? "auto"
		};

        var resp = await _http.PostAsJsonAsync(url, reqBody, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var data = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOpts, ct);
        return data?.Choices?.FirstOrDefault()?.Message.Content;
    }
}

