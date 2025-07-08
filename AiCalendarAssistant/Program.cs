// Program.cs
//
// Demo console that exercises every prompt flavour and shows a full
// tool-calling round-trip:
//
//   1) plain chat
//   2) JSON-schema extraction
//   3) tool selection  ⟶ mock function ⟶ assistant follow-up
//
// Requires the canvas files (“PromptingPipeline – v2”) to be present in
// the project and an appsettings.json with the “Llm” section.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PromptingPipeline.Config;
using PromptingPipeline.Infrastructure;
using PromptingPipeline.Interfaces;
using PromptingPipeline.Llm;
using PromptingPipeline.Models;
using PromptingPipeline.Services;
using System.Text.Json;
using System;

Console.WriteLine(DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd"));
Console.WriteLine(TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).ToString("HH:mm"));
Console.WriteLine(DateTime.UtcNow.AddHours(3).DayOfWeek.ToString(),"\n\n");
// Console.WriteLine(DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd"));
// Console.WriteLine(TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(3)).ToString("HH:mm"));
// Console.WriteLine(DateTime.UtcNow.AddHours(3).DayOfWeek.ToString(),"\n\n");


//
// ─── BOOTSTRAP DI/HTTP/CONFIG ──────────────────────────────────────────────
//
var builder = Host.CreateApplicationBuilder(args);
// builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection("Llm"));


builder.Configuration.AddEnvironmentVariables();

// var llmSettings = new LlmSettings
// {
//     Url          = Environment.GetEnvironmentVariable("Url")          ?? throw new InvalidOperationException("Missing Url"),
//     ProjectId    = Environment.GetEnvironmentVariable("ProjectId")    ?? throw new InvalidOperationException("Missing ProjectId"),
//     ModelId      = Environment.GetEnvironmentVariable("ModelId")      ?? throw new InvalidOperationException("Missing ModelId"),
//     ApiKey       = Environment.GetEnvironmentVariable("ApiKey")       ?? throw new InvalidOperationException("Missing ApiKey"),
//     UseOllama   = bool.Parse(Environment.GetEnvironmentVariable("UseOllama") ?? "false"),
//     OllamaModel  = Environment.GetEnvironmentVariable("OllamaModel")  ?? throw new InvalidOperationException("Missing OllamaModel"),
//     OllamaUrl    = Environment.GetEnvironmentVariable("OllamaUrl")    ?? throw new InvalidOperationException("Missing OllamaUrl"),
// };
//
// builder.Services.AddSingleton(llmSettings);



builder.Services.AddHttpClient<TokenProvider>();
builder.Services.AddHttpClient<WatsonxClient>();
builder.Services.AddHttpClient<OllamaClient>();

builder.Services.AddSingleton<PromptRouter>();
builder.Services.AddSingleton<IEmailReader, EmailReader>();
builder.Services.AddSingleton<EmailProcessor>();

var host   = builder.Build();
var router = host.Services.GetRequiredService<PromptRouter>();
if (false){
//
// ─── 1) SIMPLE CHAT ────────────────────────────────────────────────────────
//
var chat = new PromptRequest(new()
{
    new("system", "You are a helpful assistant."),
    new("user",   "What is the capital of France?")
});

var chatResp = await router.SendAsync(chat);
Console.WriteLine($"Capital → {chatResp.Content}");
Console.WriteLine();

//
// ─── 2) JSON-SCHEMA EXTRACTION ─────────────────────────────────────────────
//
using var schemaDoc = JsonDocument.Parse("""
{
  "type": "json_schema",
  "json_schema": {
    "name": "book_info",
    "strict": true,
    "schema": {
      "type": "object",
      "properties": {
        "book_title":       { "type": "string" },
        "author":           { "type": "string" },
        "publication_year": { "type": "integer" }
      },
      "required": ["book_title","author","publication_year"],
      "additionalProperties": false
    }
  }
}
""");

var json = new PromptRequest(new()
{
    new("system", "You are a helpful assistant."),
    new("user",
        "Extract the book title, author, and publication year from this text:\n\n" +
        "The Hobbit by J.R.R. Tolkien was first published in 1937.")
},
ResponseFormat: schemaDoc.RootElement);

var jsonResp = await router.SendAsync(json);
Console.WriteLine($"Book JSON → {jsonResp.Content}");
Console.WriteLine();

//
// ─── 3) TOOL-CALL ROUND-TRIP ───────────────────────────────────────────────
//
using var toolsDoc = JsonDocument.Parse("""
[
  {
    "type": "function",
    "function": {
      "name": "get_current_weather",
      "description": "Fetches the current weather for a given location",
      "parameters": {
        "type": "object",
        "properties": {
          "location": { "type": "string", "description": "City and country" },
          "unit":     { "type": "string", "enum": ["celsius","fahrenheit"] }
        },
        "required": ["location"]
      }
    }
  }
]
""");

// first pass – assistant decides whether to call a tool
var history = new List<Message>
{
    new("system", "You are a helpful assistant that can call external tools using the appropriate json for tool_calls. If the user asks for information that requires external data or function calling, use the tools provided. By using a tool_call you will get the information returned by the tool and then you will be able to get back to the user with the final answer."),
    new("user",   "What is the current weather in Paris?")
};

var toolPrompt = new PromptRequest(
    history,
    Tools: toolsDoc.RootElement,
    ToolChoice: "auto",
    Extra: new() { ["max_tokens"] = 10000 });

var first = await router.SendAsync(toolPrompt);

// If the assistant called a tool, execute it (mock) and send results back
if (first.HasToolCalls)
{
    foreach (var call in first.ToolCalls!)
    {
        Console.WriteLine($"Assistant requested tool → {call.Name}: {call.Arguments}");

        // ─── mock tool implementation ──────────────────────────────────
        var resultJson = call.Name switch
        {
            "get_current_weather" => /* pretend we queried an API */ """
                { "temperature": -20, "unit": "celsius", "condition": "raining with meatballs" }
            """,
            _ => "{}"
        };

        // append tool result for second round
        history.Add(new("tool", resultJson, call.Id));
    }

    // second pass – assistant gets the tool output and produces the final answer
    var followUp = new PromptRequest(history);
    var final = await router.SendAsync(followUp);

    Console.WriteLine();
    Console.WriteLine($"Assistant reply → {final.Content}");
}
else
{
    Console.WriteLine("Assistant did not request a tool – nothing to do.");
	Console.WriteLine("Raw response:");
	Console.WriteLine(JsonSerializer.Serialize(first, new JsonSerializerOptions { WriteIndented = true }));
}

}
//
// ─── 4) EMAIL PROCESSING ───────────────────────────────────────────────────
//
var emailProcessor = host.Services.GetRequiredService<EmailProcessor>();
await emailProcessor.ProcessEmailAsync();

// -- ─── 5) USER NOTE PROCESSING ──────────────────────────────────────────────
var userNoteProcessor = new UserNoteProcessor(router, new UserNoteReader());
await userNoteProcessor.ProcessUserNoteAsync();
