using AiCalendarAssistant.Models;

namespace AiCalendarAssistant.Llm;

public interface ILlmClient
{
    Task<PromptResponse> SendAsync(PromptRequest request, CancellationToken ct = default, string? additionalInstructions = null);
}

