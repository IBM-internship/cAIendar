// Services/ILlmClient.cs
using WatsonxDemo.Models;

namespace WatsonxDemo.Services;

internal interface ILlmClient
{
    Task<string?> ChatAsync(ChatRequest request, CancellationToken ct = default);
}

