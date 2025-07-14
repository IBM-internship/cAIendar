using System.Text.Json;
using System.Net.Http;
using HtmlAgilityPack;

namespace AiCalendarAssistant.Services
{
    public class GoogleSearchSettings
    {
        public string? ApiKey { get; set; }
        public string? Cx { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Cx);
    }
    public class GoogleSearchService
    {
        private readonly GoogleSearchSettings _settings;
        private readonly HttpClient _httpClient;

        public GoogleSearchService(GoogleSearchSettings settings, HttpClient httpClient)
        {
            _settings = settings;
            _httpClient = httpClient;
        }

        public async Task<string> SearchAsync(string query)
        {
            if (!_settings.IsValid)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Missing API configuration. Make sure GOOGLE_API_KEY and GOOGLE_CX are set in the environment."
                });
            }

            var url = $"https://www.googleapis.com/customsearch/v1?key={_settings.ApiKey}&cx={_settings.Cx}&q={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new
                {
                    error = $"Request to Google failed with status code {response.StatusCode}",
                    details = await response.Content.ReadAsStringAsync()
                });
            }

            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> SearchAndScrapeAsync(string query)
        {
            if (!_settings.IsValid)
            {
                return JsonSerializer.Serialize(new
                {
                    error = "Missing API configuration. Make sure GOOGLE_API_KEY and GOOGLE_CX are set in the environment."
                });
            }

            var searchUrl = $"https://www.googleapis.com/customsearch/v1?key={_settings.ApiKey}&cx={_settings.Cx}&q={Uri.EscapeDataString(query)}";
            var searchResponse = await _httpClient.GetAsync(searchUrl);
            if (!searchResponse.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new
                {
                    error = $"Search request failed with status code {searchResponse.StatusCode}",
                    details = await searchResponse.Content.ReadAsStringAsync()
                });
            }

            var searchJson = await searchResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(searchJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
            {
                return JsonSerializer.Serialize(new { error = "No results found." });
            }

            var firstItem = items[0];
            var link = firstItem.GetProperty("link").GetString();

            if (string.IsNullOrWhiteSpace(link))
                return JsonSerializer.Serialize(new { error = "First result had no link." });

            // --- Here is the updated part with User-Agent header ---

            var pageRequest = new HttpRequestMessage(HttpMethod.Get, link);
            pageRequest.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                                                "(KHTML, like Gecko) Chrome/115.0.0.0 Safari/537.36");

            var pageResponse = await _httpClient.SendAsync(pageRequest);

            if (!pageResponse.IsSuccessStatusCode)
            {
                return JsonSerializer.Serialize(new
                {
                    error = $"Failed to load page content from first result: {link}",
                    status = (int)pageResponse.StatusCode
                });
            }

            var html = await pageResponse.Content.ReadAsStringAsync();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var body = htmlDoc.DocumentNode.SelectSingleNode("//body");
            var text = body?.InnerText?.Trim() ?? "";

            return JsonSerializer.Serialize(new
            {
                url = link,
                content = text
            });
        }
    }
}