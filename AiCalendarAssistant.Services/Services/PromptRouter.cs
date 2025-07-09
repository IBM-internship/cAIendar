using Microsoft.Extensions.Options;
using PromptingPipeline.Config;
using PromptingPipeline.Models;
using AiCalendarAssistant.Interfaces;
// using PromptingPipeline.Interfaces;
using PromptingPipeline.Llm;
namespace PromptingPipeline.Services;

internal sealed class PromptRouter
{
    private readonly ILlmClient  _watsonx;
    private readonly ILlmClient  _ollama;
    private readonly LlmSettings _cfg;

    public PromptRouter(WatsonxClient w, OllamaClient o, IOptions<LlmSettings> cfg)
        => (_watsonx, _ollama, _cfg) = (w, o, cfg.Value);

    public Task<PromptResponse> SendAsync(PromptRequest req, CancellationToken ct = default)
        => (_cfg.UseOllama ? _ollama : _watsonx).SendAsync(req, ct);
}
