using System.Text.Json;
using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ToolDefinition(
    [property: JsonPropertyName("type")]     string Type,
    [property: JsonPropertyName("function")] FunctionDefinition Function);
