using System.Text.Json;
using System.Net.Http;

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
    }
}