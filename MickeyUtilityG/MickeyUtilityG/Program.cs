using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using MickeyUtilityG.Services.Shared;
using MickeyUtilityG;
using MickeyUtilityG.Services.PageServices;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        builder.Services.AddScoped<ConfigurationService>();
        builder.Services.AddScoped<TodoListService>();
        builder.Services.AddScoped<GoogleSheetsApiService>();

        // Load configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        if (builder.HostEnvironment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
        }

        await builder.Build().RunAsync();
    }
}