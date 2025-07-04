using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ChatRequest(
    [property: JsonPropertyName("model_id")]     string ModelId,
    [property: JsonPropertyName("project_id")]   string ProjectId,
    [property: JsonPropertyName("messages")]     IEnumerable<ChatMessage> Messages,
    [property: JsonPropertyName("functions")]    IEnumerable<FunctionDefinition>? Functions = null,
    [property: JsonPropertyName("function_call")] string? FunctionCall = null);
	[property: JsonPropertyName("tools")]       IEnumerable<ToolDefinition>? Tools = null,
    [property: JsonPropertyName("tool_choice")] object? ToolChoice = null);
