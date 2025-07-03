using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WatsonxDemo.Models;                          // NEW
using System.Text.Json;                           // NEW

namespace WatsonxDemo.Services;

internal sealed class WatsonxClient
{
    private readonly HttpClient     _http;
    private readonly TokenProvider  _tokens;
    private readonly WatsonxSettings _cfg;

    public WatsonxClient(HttpClient http, TokenProvider tokens,
                         IOptions<WatsonxSettings> cfg)
        => (_http, _tokens, _cfg) = (http, tokens, cfg.Value);

    public async Task<string?> ChatAsync(string question,
                                         CancellationToken ct = default)
    {
        var url = $"{_cfg.Url.TrimEnd('/')}/ml/text/chat" +
                  $"?version={_cfg.Version}";

        var request = new ChatRequest(
            _cfg.ModelId,
            _cfg.ProjectId,
            new[]
            {
                new ChatMessage("system",
                    new[] { new ChatContent("text", "You are a helpful assistant.") }),
                new ChatMessage("user",
                    new[] { new ChatContent("text", question) })
            });

        var httpMsg = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request)
        };
        httpMsg.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer",
                                          await _tokens.GetAsync(ct));

        var resp = await _http.SendAsync(httpMsg, ct);
		Console.WriteLine($"[WatsonxClient] Request sent to: {url}");
		Console.WriteLine($"[WatsonxClient] Request body: {JsonSerializer.Serialize(request)}");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<ChatResponse>(ct);
		Console.WriteLine($"[WatsonxClient] Response received: {JsonSerializer.Serialize(body)}");
		Console.WriteLine("Raw response body: " + await resp.Content.ReadAsStringAsync(ct));
        return body?.Messages?.FirstOrDefault(m => m.Role == "assistant")
                            ?.Content.FirstOrDefault()?.Text;
    }
}

