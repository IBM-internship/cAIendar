using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using AiCalendarAssistant.Config;

namespace AiCalendarAssistant.Infrastructure;

public class TokenProvider(HttpClient http, IOptions<LlmSettings> cfg)
{
    private readonly LlmSettings _cfg = cfg.Value;
    private string?  _token;
    private DateTime _expiresUtc;

    public async ValueTask<string> GetAsync(CancellationToken ct = default)
    {
        if (_token is not null && DateTime.UtcNow < _expiresUtc)
            return _token;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ibm:params:oauth:grant-type:apikey",
            ["apikey"]     = _cfg.ApiKey
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://iam.cloud.ibm.com/identity/token");
        req.Content = new FormUrlEncodedContent(form);
        req.Headers.Accept.ParseAdd("application/json");

        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<IamTokenResponse>(ct)
                   ?? throw new InvalidOperationException("Empty IAM response!");

        _token      = body.AccessToken ?? throw new InvalidOperationException("Missing token!");
        _expiresUtc = DateTime.UtcNow.AddSeconds(body.ExpiresIn - 60);
        return _token;
    }

    private sealed record IamTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")]   int    ExpiresIn);
}
