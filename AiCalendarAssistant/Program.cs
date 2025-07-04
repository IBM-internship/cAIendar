using AiCalendarAssistant.Data;
using AiCalendarAssistant.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AiCalendarAssistant
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load connection string from file
            string connectionStringFile = "db_connection.txt";

            if (!File.Exists(connectionStringFile))
            {
                throw new FileNotFoundException("Connection string file not found.", connectionStringFile);
            }

            string connectionString = File.ReadAllText(connectionStringFile).Trim();

            // Add services to the container.
            //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? 
            //    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            
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

			builder.Services.AddAuthentication()
	            .AddGoogle(options =>
	            {
		            options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
		            options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

		            // Optional: Configure additional options
		            options.CallbackPath = "/signin-google"; // Default callback path
		            options.SaveTokens = true; // Save access and refresh tokens

		            // Optional: Request additional scopes
		            options.Scope.Add("profile");
		            options.Scope.Add("email");
	            });

			builder.Services.AddControllersWithViews();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }
    }
}
