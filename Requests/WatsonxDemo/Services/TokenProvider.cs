// using System.Net.Http.Json;
// using System.Collections.Generic;                 // NEW
// using Microsoft.Extensions.Options;
//
// namespace WatsonxDemo.Services;
//
// internal sealed class TokenProvider
// {
//     private readonly HttpClient _http;
//     private readonly WatsonxSettings _cfg;
//     private string?  _token;
//     private DateTime _expiresUtc;
//
//     public TokenProvider(HttpClient http, IOptions<WatsonxSettings> cfg)
//         => (_http, _cfg) = (http, cfg.Value);
//
//     public async ValueTask<string> GetAsync(CancellationToken ct = default)
//     {
//         if (_token is not null && DateTime.UtcNow < _expiresUtc)
//             return _token;
//
//         var form = new Dictionary<string,string>          // CONCRETE collection
//         {
//             ["grant_type"] = "urn:ibm:params:oauth:grant-type:apikey",
//             ["apikey"]     = _cfg.ApiKey
//         };
//         var resp = await _http.PostAsync(
//             "https://iam.cloud.ibm.com/identity/token",
//             new FormUrlEncodedContent(form), ct);
//
//         resp.EnsureSuccessStatusCode();
//         var body = await resp.Content.ReadFromJsonAsync<IamTokenResponse>(ct);
//
// 		// test print:
// 		Console.WriteLine($"[TokenProvider] Using API key: {_cfg.ApiKey}");
//         _token      = body!.AccessToken;
//         Console.WriteLine($"[TokenProvider] Generated token: {_token}...");
//         _expiresUtc = DateTime.UtcNow.AddSeconds(body.ExpiresIn - 60);
//         return _token;
//     }
// }
//
// internal sealed record IamTokenResponse(string AccessToken, int ExpiresIn);

//// Services/TokenProvider.cs
using System.Text.Json.Serialization;             // ✱ NEW
using System.Net.Http.Json;
using System.Collections.Generic;                 // NEW
using Microsoft.Extensions.Options;

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

		Console.WriteLine($"[TokenProvider] Using API key: {_cfg.ApiKey}");

        var body = await resp.Content.ReadFromJsonAsync<IamTokenResponse>(ct)
                   ?? throw new InvalidOperationException("Empty IAM response!");

        _token      = body.AccessToken
                   ?? throw new InvalidOperationException("No access_token in IAM response!");

		Console.WriteLine($"[TokenProvider] Generated token: {_token}...");


        _expiresUtc = DateTime.UtcNow.AddSeconds(body.ExpiresIn - 60);
        return _token;
    }
}

// record moved to its own file or keep here – both fine
internal sealed record IamTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")]   int    ExpiresIn);

