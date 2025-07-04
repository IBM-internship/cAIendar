// Services/WatsonxSettings.cs
namespace WatsonxDemo.Services;

public sealed record WatsonxSettings
{
    // IBM Cloud
    public required string Url        { get; init; }
    public required string ProjectId  { get; init; }
    public required string ModelId    { get; init; }
    public required string Version    { get; init; }
    public required string ApiKey     { get; init; }

    // Ollama
    public bool   UseOllama   { get; init; } = false;
    public string OllamaUrl   { get; init; } = "http://localhost:11434";
    public string OllamaModel { get; init; } = "granite3.3:latest";
}

