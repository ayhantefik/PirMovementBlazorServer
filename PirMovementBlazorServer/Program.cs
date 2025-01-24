using PirMovementBlazorServer.Connection;
using PirMovementBlazorServer.Infrastructure;
using PirMovementBlazorServer.Services;

namespace PirMovementBlazorServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var websiteConfig = new WebsiteConfig(builder.Configuration);

            // Add services to the container.
            builder.Services.AddHttpClient("PirApi", client =>
            {
                client.BaseAddress = new Uri($"{websiteConfig.Url}");
            });

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri($"{websiteConfig.Url}")
            });
            builder.Services.AddSingleton<DbConnectionFactory>();
            builder.Services.AddSingleton<MovementListService>();
            builder.Services.AddHttpClient();

            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddHostedService<MqttService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });

            app.Urls.Add($"{websiteConfig.Url}");

            app.Run();
        }
    }
}
