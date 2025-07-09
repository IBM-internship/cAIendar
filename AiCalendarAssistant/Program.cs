using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using System.Collections;
using AiCalendarAssistant.Services.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Load .env from current directory (project root)
DotNetEnv.Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));


// Read connection string from environment variables (set in .env)
const string connectionStringFile = "db_connection.txt";

if (!File.Exists(connectionStringFile))
{
	throw new FileNotFoundException("Connection string file not found.", connectionStringFile);
}

var connectionString = File.ReadAllText(connectionStringFile).Trim();

// Add services to the container
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
		options.Scope.Add("https://www.googleapis.com/auth/gmail.readonly");
	});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<GmailEmailService>();

var app = builder.Build();

// Configure the HTTP request pipeline
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
