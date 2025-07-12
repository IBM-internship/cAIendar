using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using AiCalendarAssistant.Services;
using Microsoft.EntityFrameworkCore;
using AiCalendarAssistant.Services.Contracts;
using Microsoft.AspNetCore.Authentication;
using AiCalendarAssistant.Config;
using AiCalendarAssistant.Infrastructure;
using AiCalendarAssistant.Llm;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
const string connectionStringFile = "db_connection.txt";

if (!File.Exists(connectionStringFile))
{
    throw new FileNotFoundException("Connection string file not found.", connectionStringFile);
}

var connectionString = File.ReadAllText(connectionStringFile).Trim();

// Add services to the containers
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

var googleClientId = Environment.GetEnvironmentVariable("Authentication__Google__ClientId")
                     ?? throw new InvalidOperationException("Google ClientId not found in environment variables.");
var googleClientSecret = Environment.GetEnvironmentVariable("Authentication__Google__ClientSecret")
                         ?? throw new InvalidOperationException("Google ClientSecret not found in environment variables.");

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
        options.AccessType = "offline";

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("https://www.googleapis.com/auth/gmail.modify");

        options.Events.OnTicketReceived = async context =>
        {
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var signInManager =
                context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return;
            }

            var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (user != null && context.Properties!.GetTokens() is { } tokens)
            {
                var authenticationTokens = tokens.ToList();
                var accessToken = authenticationTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
                var refreshToken = authenticationTokens.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
                var expiresAt = authenticationTokens.FirstOrDefault(t => t.Name == "expires_at")?.Value;

                if (accessToken != null)
                    await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "access_token",
                        accessToken);
                if (refreshToken != null)
                    await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "refresh_token",
                        refreshToken);
                if (expiresAt != null)
                    await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "expires_at", expiresAt);
            }
        };

        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            context.Response.Redirect(context.RedirectUri + "&prompt=consent");
            return Task.CompletedTask;
        };
    });

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GmailEmailService>();
builder.Services.AddScoped<TokenRefreshService>(provider =>
    new TokenRefreshService(
        provider.GetRequiredService<IHttpContextAccessor>(),
        provider.GetRequiredService<IConfiguration>(),
        provider.GetRequiredService<ILogger<TokenRefreshService>>(),
        provider.GetRequiredService<UserManager<ApplicationUser>>(),
        googleClientId,
        googleClientSecret
    ));
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<INoteService, NoteService>();

var watsonxUrl        = Environment.GetEnvironmentVariable("Llm__Url");
var watsonxProjectId  = Environment.GetEnvironmentVariable("Llm__ProjectId");
var watsonxModelId    = Environment.GetEnvironmentVariable("Llm__ModelId");
var watsonxApiKey     = Environment.GetEnvironmentVariable("Llm__ApiKey");
var watsonxVersion    = Environment.GetEnvironmentVariable("Llm__Version");
var ollamaUse         = Environment.GetEnvironmentVariable("Llm__UseOllama");
var ollamaUrl         = Environment.GetEnvironmentVariable("Llm__OllamaUrl");
var ollamaModel       = Environment.GetEnvironmentVariable("Llm__OllamaModel");
Console.WriteLine($"Ollama model: {ollamaModel}");
// Create LlmSettings from environment variables
var llmSettings = new LlmSettings
{
    Url         = watsonxUrl        ?? throw new InvalidOperationException("Missing Llm__Url"),
    ProjectId   = watsonxProjectId  ?? throw new InvalidOperationException("Missing Llm__ProjectId"),
    ModelId     = watsonxModelId    ?? throw new InvalidOperationException("Missing Llm__ModelId"),
    ApiKey      = watsonxApiKey     ?? throw new InvalidOperationException("Missing Llm__ApiKey"),
    Version     = watsonxVersion    ?? "2023-10-25",
    UseOllama   = bool.TryParse(ollamaUse, out var useOllama) && useOllama,
    OllamaUrl   = ollamaUrl         ?? "http://host.docker.internal:11434",
    OllamaModel = ollamaModel       ?? "granite3.3:latest"
};

builder.Services.AddSingleton(llmSettings);
builder.Services.AddHttpClient<TokenProvider>();
builder.Services.AddHttpClient<WatsonxClient>();
builder.Services.AddHttpClient<OllamaClient>();
builder.Services.AddSingleton<PromptRouter>();
builder.Services.AddSingleton<EmailProcessor>();
builder.Services.AddScoped<ChatMessenger>();
builder.Services.AddSingleton<EventProcessor>();
builder.Services.AddSingleton<EmailComposer>();


var app = builder.Build();
// var router = app.Services.GetRequiredService<PromptRouter>();

// var chat = new PromptRequest(new()
// {
//     new("system", "You are a helpful assistant."),
//     new("user",   "What is the capital of France?")
// });
//
// var chatResp = await router.SendAsync(chat);
// Console.WriteLine($"Capital → {chatResp.Content}");

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
