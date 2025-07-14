using AiCalendarAssistant.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AiCalendarAssistant.Controllers.ApiControllers
{
    public class SearchRequest
    {
        public string? Query { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly GoogleSearchService _searchService;

        public SearchController(GoogleSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] SearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Query))
                return BadRequest(new { error = "Missing 'query' in request body." });

            var jsonResult = await _searchService.SearchAsync(request.Query);
            Console.WriteLine(jsonResult);
            try
            {
                using var doc = JsonDocument.Parse(jsonResult);
                var cloned = JsonSerializer.Deserialize<object>(doc.RootElement.GetRawText());
                return Ok(cloned);
            }
            catch
            {
                return StatusCode(500, new { error = "Failed to parse search result." });
            }
        }
    }
}