using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

public class ConfigurationService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IConfiguration _configuration;
    private readonly NavigationManager _navigationManager;

    public ConfigurationService(IJSRuntime jsRuntime, IConfiguration configuration, NavigationManager navigationManager)
    {
        _jsRuntime = jsRuntime;
        _configuration = configuration;
        _navigationManager = navigationManager;
    }

    public async Task<string> GetConfigValue(string key)
    {
        if (_navigationManager.BaseUri.Contains("localhost"))
        {
            // Development environment
            return _configuration[$"Google:{key}"];
        }
        else
        {
            // Production environment
            return await _jsRuntime.InvokeAsync<string>("eval", $"window.config.{key}");
        }
    }
}