using AiCalendarAssistant.Config;
using AiCalendarAssistant.Llm;
using AiCalendarAssistant.Models;
using Microsoft.Extensions.Options;
using System.Text;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services;

public sealed class PromptRouter(
    WatsonxClient watsonx,
    OllamaClient ollama,
    IOptions<LlmSettings> cfgOptions)
{
    private readonly ILlmClient _watsonx = watsonx ?? throw new ArgumentNullException(nameof(watsonx));
    private readonly ILlmClient _ollama = ollama ?? throw new ArgumentNullException(nameof(ollama));
    private readonly LlmSettings _cfg = cfgOptions?.Value ?? throw new ArgumentNullException(nameof(cfgOptions));

    public async Task<PromptResponse> SendAsync(PromptRequest req, ApplicationUser? user = null, CancellationToken ct = default)
    {
        // Build “extra info” block, always including today’s date...
        var sb = new StringBuilder()
            .AppendLine("Extra info:")
            .AppendLine($"- Today is {DateTime.Now:yyyy-MM-dd} ({DateTime.Now.DayOfWeek})");

        // ...and only include the user description if one was provided
        if (!string.IsNullOrWhiteSpace(user?.UserDiscription))
        {
            sb.AppendLine($"- User description: {user.UserDiscription}");
        }

        var additionalInstructions = sb.ToString();
        Console.WriteLine(additionalInstructions);

        // Try up to 3 times
        for (var i = 0; i < 3; i++)
        {
            try
            {
                var client = _cfg.UseOllama ? _ollama : _watsonx;
                return await client.SendAsync(req, ct, additionalInstructions);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Attempt {i + 1} failed: {e.Message}");
            }
        }

        throw new InvalidOperationException("Failed to send prompt after multiple attempts.");
    }
}