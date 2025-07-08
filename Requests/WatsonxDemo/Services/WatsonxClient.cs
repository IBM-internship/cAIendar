using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using WatsonxDemo.Models;                          // NEW
using System.Text.Json;                            // NEW

namespace WatsonxDemo.Services;

internal sealed class WatsonxClient: ILlmClient
{
    private readonly HttpClient     _http;
    private readonly TokenProvider  _tokens;
    private readonly WatsonxSettings _cfg;

    public WatsonxClient(HttpClient http, TokenProvider tokens,
                         IOptions<WatsonxSettings> cfg)
        => (_http, _tokens, _cfg) = (http, tokens, cfg.Value);

    public async Task<string?> ChatAsync(ChatRequest request,
                                         CancellationToken ct = default)
    {
        var url = $"{_cfg.Url.TrimEnd('/')}/ml/v1/text/chat" +
                  $"?version={_cfg.Version}";

    var opts = new JsonSerializerOptions
    {
        DefaultIgnoreCondition   = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true          // robustness
    };

    var msg = new HttpRequestMessage(HttpMethod.Post, url)
    {
        Content = JsonContent.Create(request, options: opts)
    };
    msg.Headers.Authorization =
        new AuthenticationHeaderValue("Bearer", await _tokens.GetAsync(ct));

    var resp = await _http.SendAsync(msg, ct);
    resp.EnsureSuccessStatusCode();

    // ★ deserialize to the new type
    var body = await resp.Content.ReadFromJsonAsync<ChatCompletionResponse>(opts, ct);

    // pick first choice
    return body?.Choices?.FirstOrDefault()?.Message.Content;
}

}

