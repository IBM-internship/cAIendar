namespace AiCalendarAssistant.Config;

public sealed record LlmSettings
{
    // IBM Cloud watsonx settings
    public required string Url        { get; init; }
    public required string ProjectId  { get; init; }
    public required string ModelId    { get; init; }
    public string         Version     { get; init; } = "2023-10-25";
    public required string ApiKey     { get; init; }

    // Local Ollama fallback
    public bool   UseOllama   { get; init; } = true;
    public string OllamaUrl   { get; init; } = "http://host.docker.internal:11434";
    public string OllamaModel { get; init; } = "granite3.3:latest";
	public string OllamaApiKey { get; init; } = "";
}
