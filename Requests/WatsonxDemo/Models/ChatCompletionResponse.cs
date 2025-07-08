// Models/ChatCompletionResponse.cs
using System.Text.Json.Serialization;

namespace WatsonxDemo.Models;

public sealed record ChatCompletionResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice> Choices);

public sealed record ChatChoice(
    [property: JsonPropertyName("index")]          int Index,
    [property: JsonPropertyName("message")]        ChatChoiceMessage Message,
    [property: JsonPropertyName("finish_reason")]  string FinishReason);

public sealed record ChatChoiceMessage(
    [property: JsonPropertyName("role")]    string Role,
    [property: JsonPropertyName("content")] string Content);

