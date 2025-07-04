using Microsoft.Extensions.DependencyInjection;   // NEW
using Microsoft.Extensions.Hosting;
using WatsonxDemo.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<WatsonxSettings>(
    builder.Configuration.GetSection("Watsonx"));

builder.Services.AddHttpClient<TokenProvider>();
builder.Services.AddHttpClient<WatsonxClient>();

var host   = builder.Build();
var client = host.Services.GetRequiredService<WatsonxClient>();

while (true)
{
    Console.Write("You > ");
    var prompt = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(prompt)) break;

    var answer = await client.ChatAsync(prompt!);
    Console.WriteLine($"Bot > {answer}\n");
}

