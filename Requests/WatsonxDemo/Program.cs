using Microsoft.Extensions.DependencyInjection;   // NEW
using Microsoft.Extensions.Hosting;
using WatsonxDemo.Services;

var builder = Host.CreateApplicationBuilder(args);

// builder.Services.Configure<WatsonxSettings>(
//     builder.Configuration.GetSection("Watsonx"));

builder.Services.AddHttpClient<TokenProvider>();
builder.Services.AddHttpClient<WatsonxClient>();

var host   = builder.Build();
var client = host.Services.GetRequiredService<WatsonxClient>();

while (true)
{
    Console.Write("You > ");
    var prompt = Console.ReadLine();
	Console.WriteLine();
    if (string.IsNullOrWhiteSpace(prompt)) break;
	Console.WriteLine("Thinking...\n");

    var answer = await client.ChatAsync(prompt!);
	Console.WriteLine("Done!\n");
    Console.WriteLine($"Bot > {answer}\n");
}

