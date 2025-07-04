using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WatsonxDemo.Services;

var builder = Host.CreateApplicationBuilder(args);

// 1) bind config
builder.Services.Configure<WatsonxSettings>(
    builder.Configuration.GetSection("Watsonx"));

// 2) register both typed HttpClients
builder.Services.AddHttpClient<TokenProvider>();
builder.Services.AddHttpClient<WatsonxClient>();
builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddSingleton<ChatRouter>();

// 3) choose implementation at runtime
builder.Services.AddSingleton<ILlmClient>(sp =>
{
    var cfg = sp.GetRequiredService<IOptions<WatsonxSettings>>().Value;
    return cfg.UseOllama
        ? sp.GetRequiredService<OllamaClient>()
        : sp.GetRequiredService<WatsonxClient>();
});

var host   = builder.Build();
var router = host.Services.GetRequiredService<ChatRouter>();

// while (true)
// {
//     Console.Write("You > ");
//     var prompt = Console.ReadLine();
//     if (string.IsNullOrWhiteSpace(prompt)) break;
//
//     using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { prompt }));
//     var answer = await router.PromptAsync(doc);
//     Console.WriteLine($"Bot > {answer}\n");
// }

using (var simple = JsonDocument.Parse(
    """{ "prompt": "What is the capital of France?" }"""))
{
	var resp = await router.PromptAsync(simple);
    Console.WriteLine($"Capital? {resp}\n");
}


// --- structured JSON schema example ---
var schemaJson =
    """
    {
      "prompt": "List the capitals of France and Germany.",
      "system": "Return the answer as JSON that matches the schema.",
      "schema": {
        "title": "CountryCapitals",
        "type": "object",
        "properties": {
          "france":  { "type": "string", "description": "Capital of France" },
          "germany": { "type": "string", "description": "Capital of Germany" }
        },
        "required": ["france", "germany"]
      }
    }
    """;

using (var withSchema = JsonDocument.Parse(schemaJson))
{
    var resp2 = await router.PromptAsync(withSchema);
    Console.WriteLine($"Capitals JSON: {resp2}\n");
}
