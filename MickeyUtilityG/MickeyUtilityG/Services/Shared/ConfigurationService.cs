using Microsoft.AspNetCore.Components;

public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(IConfiguration configuration, NavigationManager navigationManager, ILogger<ConfigurationService> logger)
    {
        _configuration = configuration;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    public string GetConfigValue(string key)
    {
        var value = _configuration[$"Google:{key}"];
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning($"Configuration value for 'Google:{key}' is null or empty");
        }
        else
        {
            _logger.LogInformation($"Retrieved configuration value for 'Google:{key}'");
        }
        return value;
    }
}