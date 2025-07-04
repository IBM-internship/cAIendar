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

while (true)
{
    Console.Write("You > ");
    var prompt = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(prompt)) break;

    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { prompt }));
    var answer = await router.PromptAsync(doc);
    Console.WriteLine($"Bot > {answer}\n");
}

