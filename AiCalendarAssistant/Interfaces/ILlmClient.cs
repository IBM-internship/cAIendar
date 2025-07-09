namespace PromptingPipeline.Llm;

using PromptingPipeline.Models;

internal interface ILlmClient
{
    Task<PromptResponse> SendAsync(PromptRequest request, CancellationToken ct = default);
}

