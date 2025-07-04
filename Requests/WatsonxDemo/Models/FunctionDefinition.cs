using System.Text.Json;
using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record FunctionDefinition(
    [property: JsonPropertyName("name")]        string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("parameters")]  JsonElement Parameters);
