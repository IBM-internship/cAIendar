using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ChatContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text);

