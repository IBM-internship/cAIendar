using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using WatsonxDemo.Models;

namespace WatsonxDemo.Services;

internal sealed class ChatRouter
{
    private readonly ILlmClient _client;
    private readonly WatsonxSettings _cfg;

    public ChatRouter(ILlmClient client, IOptions<WatsonxSettings> cfg)
        => (_client, _cfg) = (client, cfg.Value);

    public async Task<string?> PromptAsync(JsonDocument schema, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>();

        if (schema.RootElement.TryGetProperty("messages", out var arr))
        {
            foreach (var m in arr.EnumerateArray())
            {
                if (m.TryGetProperty("role", out var roleEl) &&
                    m.TryGetProperty("text", out var textEl))
                {
                    var role = roleEl.GetString();
                    var text = textEl.GetString();
                    if (!string.IsNullOrWhiteSpace(role) && !string.IsNullOrWhiteSpace(text))
                    {
                        messages.Add(new ChatMessage(role!, new[] { new ChatContent("text", text!) }));
                    }
                }
            }
        }
        else if (schema.RootElement.TryGetProperty("prompt", out var promptEl))
        {
            var system = schema.RootElement.TryGetProperty("system", out var sysEl)
                ? sysEl.GetString() ?? "You are a helpful assistant."
                : "You are a helpful assistant.";

            messages.Add(new ChatMessage("system", new[] { new ChatContent("text", system) }));
            messages.Add(new ChatMessage("user", new[] { new ChatContent("text", promptEl.GetString() ?? string.Empty) }));
        }
        else
        {
            throw new InvalidOperationException("No prompt or messages in schema");
        }

        IEnumerable<FunctionDefinition>? functions = null;
        if (schema.RootElement.TryGetProperty("functions", out var fnArr)||
			schema.RootElement.TryGetProperty("tools",     out fnArr))
        {
            var list = new List<FunctionDefinition>();
            foreach (var f in fnArr.EnumerateArray())
            {
                if (f.TryGetProperty("name", out var nEl) &&
                    f.TryGetProperty("parameters", out var pEl))
                {
                    var name = nEl.GetString() ?? string.Empty;
                    var desc = f.TryGetProperty("description", out var dEl)
                        ? dEl.GetString() ?? string.Empty
                        : string.Empty;
                    list.Add(new FunctionDefinition(name, desc, pEl.Clone()));
                }
            }
            functions = list;
        }

        string? functionCall = null;
        if (schema.RootElement.TryGetProperty("function_call", out var fcEl) ||
			schema.RootElement.TryGetProperty("tool_choice",   out fcEl)))
        {
            functionCall = fcEl.GetString();
        }

        var request = new ChatRequest(_cfg.ModelId, _cfg.ProjectId, messages, functions, functionCall);
        return await _client.ChatAsync(request, ct);
    }
}
