using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ChatResponse(
    [property: JsonPropertyName("messages")] IEnumerable<ChatMessage>? Messages);

