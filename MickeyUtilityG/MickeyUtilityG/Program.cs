using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
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
        builder.Services.AddLogging();

        // Load configuration
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        // Register services
        builder.Services.AddScoped<GoogleSheetsApiService>();
        builder.Services.AddScoped<TodoListService>();

        await builder.Build().RunAsync();
    }
}