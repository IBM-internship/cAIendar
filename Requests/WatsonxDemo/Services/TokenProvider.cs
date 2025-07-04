// Services/TokenProvider.cs
using System.Text.Json.Serialization;             
using System.Net.Http.Json;
using System.Collections.Generic;                 
using WatsonxDemo.Models;                     

namespace WatsonxDemo.Services;

internal sealed class TokenProvider
{
	private readonly HttpClient _http;
    private readonly WatsonxSettings _cfg;
    private string?  _token;
    private DateTime _expiresUtc;

    public TokenProvider(HttpClient http, IOptions<WatsonxSettings> cfg)
        => (_http, _cfg) = (http, cfg.Value);

	public async ValueTask<string> GetAsync(CancellationToken ct = default)
    {
        if (_token is not null && DateTime.UtcNow < _expiresUtc)
            return _token;

        var form = new Dictionary<string,string>
        {
            ["grant_type"] = "urn:ibm:params:oauth:grant-type:apikey",
            ["apikey"]     = _cfg.ApiKey
        };

        var req = new HttpRequestMessage(HttpMethod.Post,
               "https://iam.cloud.ibm.com/identity/token")        // region-agnostic IAM URL :contentReference[oaicite:1]{index=1}
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Accept.ParseAdd("application/json");          // ✱ tell IAM we expect JSON

        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<IamTokenResponse>(ct)
                   ?? throw new InvalidOperationException("Empty IAM response!");

        _token      = body.AccessToken
                   ?? throw new InvalidOperationException("No access_token in IAM response!");
        _expiresUtc = DateTime.UtcNow.AddSeconds(body.ExpiresIn - 60);
        return _token;
    }
}

// record moved to its own file or keep here – both fine
internal sealed record IamTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")]   int    ExpiresIn);

