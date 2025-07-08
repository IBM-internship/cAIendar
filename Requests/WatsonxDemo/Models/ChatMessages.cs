using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ChatMessage(
    [property: JsonPropertyName("role")]   string Role,
    [property: JsonPropertyName("content")]IEnumerable<ChatContent> Content);

