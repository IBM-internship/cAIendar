using System.Text.Json;

namespace PromptingPipeline.Models;

public sealed record Message(string Role, string Content, string? ToolCallId = null);

public sealed record ToolCall(string Id, string Name, JsonElement Arguments);

public sealed record PromptResponse(string? Content, List<ToolCall>? ToolCalls)
{
    public bool HasToolCalls => ToolCalls is { Count: > 0 };
}

public sealed record PromptRequest(
    List<Message> Messages,
    JsonElement? ResponseFormat  = null,
    JsonElement? Tools           = null,
    string?      ToolChoice      = null,
    Dictionary<string,object?>? Extra = null);

