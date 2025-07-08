# Watsonx Demo

This repository contains a .NET 9 console application that demonstrates how to call the IBM watsonx generative AI API. It can use either the official watsonx endpoint or an Ollama server.

## Prerequisites

- .NET 9 SDK (Preview) – required to build and run the application.
- A watsonx API key and project details or access to an Ollama server.

## Configuration

The application expects a configuration section named `Watsonx`. You can provide it using `appsettings.json`, user secrets or environment variables. Below is an example of an `appsettings.json` file:

```json
{
  "Watsonx": {
    "Url": "https://us-south.ml.cloud.ibm.com",
    "ProjectId": "your-project-id",
    "ModelId": "ibm/granite-13b-chat-v2",
    "Version": "2024-05-15",
    "ApiKey": "YOUR_API_KEY",

    "UseOllama": false,
    "OllamaUrl": "http://localhost:11434",
    "OllamaModel": "granite3.3:latest"
  }
}
```

When `UseOllama` is `false` the program calls the watsonx API using IAM authentication. If set to `true` it will query an Ollama instance instead.

Add this file locally (it is ignored by Git) or configure the same settings via environment variables such as `Watsonx__ApiKey`.

## Building and Running

```bash
# restore dependencies and build
 dotnet build

# run the program
 dotnet run
```

The program will execute a couple of hard coded prompts and print their responses. You can uncomment the loop in `Program.cs` to interact with the bot repeatedly.

## Project Structure

```
.
├── Models/       # Request/response types for chat APIs
├── Services/     # HTTP clients and helpers
├── Program.cs    # Application entry point
└── WatsonxDemo.csproj
```

### Models

The `Models` folder defines records that mirror the JSON payloads used when calling the chat endpoints:

- `ChatRequest` – request body for watsonx chat completion.
- `ChatMessage` and `ChatContent` – pieces of a conversation.
- `ChatCompletionResponse`/`ChatChoice` – minimal deserialization types for the response.
- `ChatResponse` – unused but provided for completeness.

### Services

- `ILlmClient` – abstraction implemented by both HTTP clients.
- `WatsonxClient` – talks to the IBM watsonx service and handles token acquisition using `TokenProvider`.
- `OllamaClient` – sends chat requests to an Ollama server.
- `TokenProvider` – obtains an API token from IBM Cloud and creates IAM headers for watsonx requests.
- `ChatRouter` – converts a simple schema (either `prompt`+`system` or an array of `messages`) into a `ChatRequest` and delegates to the selected client.
- `WatsonxSettings` – strongly typed configuration options.

### Program.cs

Sets up dependency injection, binds configuration to `WatsonxSettings`, registers HTTP clients and decides at runtime whether to use `WatsonxClient` or `OllamaClient`. A couple of example prompts are executed after startup.

## Development Notes

- Standard .NET tooling is used: `dotnet build`, `dotnet run`, etc.
- Temporary files (`bin/`, `obj/`) and `appsettings.json` are excluded via `.gitignore`.
- The project targets `net9.0` and enables nullable reference types and implicit usings.

Feel free to extend the project by adding more commands, improving error handling or parsing richer request schemas.

