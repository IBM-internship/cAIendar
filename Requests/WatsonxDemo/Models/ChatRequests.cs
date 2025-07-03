using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ChatRequest(
    [property: JsonPropertyName("model_id")]  string ModelId,
    [property: JsonPropertyName("project_id")]string ProjectId,
    [property: JsonPropertyName("messages")]  IEnumerable<ChatMessage> Messages);

