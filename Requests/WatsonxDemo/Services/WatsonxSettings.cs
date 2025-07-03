// Services/WatsonxSettings.cs
namespace WatsonxDemo.Services;

public sealed record WatsonxSettings
{
    public required string Url       { get; init; }
    public required string ProjectId { get; init; }
    public required string ModelId   { get; init; }
    public required string Version   { get; init; }
    public required string ApiKey    { get; init; }
}

