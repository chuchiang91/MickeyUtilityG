using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Components;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly NavigationManager _navigationManager;

    public ConfigurationService(IConfiguration configuration, NavigationManager navigationManager)
    {
        _configuration = configuration;
        _navigationManager = navigationManager;
    }

    public string GetConfigValue(string key)
    {
        if (_navigationManager.BaseUri.Contains("localhost"))
        {
            // Development environment
            return _configuration[$"Google:{key}"];
        }
        else
        {
            // Production environment
            return _configuration[$"Google:{key}"];
        }
    }
}