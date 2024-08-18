using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace MickeyUtilityG.Services.Shared
{
    public class GoogleSheetsApiService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;
        private readonly ILogger<GoogleSheetsApiService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string _accessToken;
        private readonly ConfigurationService _configService;

        public GoogleSheetsApiService(ConfigurationService configService, IJSRuntime jsRuntime, NavigationManager navigationManager, ILogger<GoogleSheetsApiService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _configService = configService;
        }

        private string GetClientId() => _configService.GetConfigValue("googleClientId");
        private string GetClientSecret() => _configService.GetConfigValue("googleClientSecret");
        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                var redirectUri = _navigationManager.BaseUri.TrimEnd('/') + "/authentication/google-callback";
                var clientId =  GetClientId();

                var authUrl = "https://accounts.google.com/o/oauth2/v2/auth" +
                    $"?client_id={Uri.EscapeDataString(clientId)}" +
                    $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                    "&response_type=code" +
                    "&scope=" + Uri.EscapeDataString("https://www.googleapis.com/auth/spreadsheets") +
                    "&access_type=offline" +
                    "&include_granted_scopes=true" +
                    "&prompt=consent";

                _logger.LogInformation($"Opening auth URL: {authUrl}");
                await _jsRuntime.InvokeVoidAsync("open", authUrl, "_blank");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during authentication process");
                return false;
            }
        }

        public async Task HandleAuthorizationCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation($"Handling authorization code: {code.Substring(0, 5)}...");

                var redirectUri = _navigationManager.BaseUri.TrimEnd('/') + "/authentication/google-callback";
                var clientId =  GetClientId();
                var clientSecret =  GetClientSecret();

                var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", clientId),
                    new KeyValuePair<string, string>("client_secret", clientSecret),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("grant_type", "authorization_code")
                });
                tokenRequest.Content = content;

                var response = await _httpClient.SendAsync(tokenRequest);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                _accessToken = tokenResponse.GetProperty("access_token").GetString();

                _logger.LogInformation("Successfully obtained access token");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling authorization code");
                throw;
            }
        }

        public async Task<List<List<object>>> GetValuesAsync(string spreadsheetId, string range)
        {
            _logger.LogInformation($"Getting values for spreadsheet {spreadsheetId}, range {range}");

            if (string.IsNullOrEmpty(_accessToken))
            {
                _logger.LogWarning("Access token is not available. Please authenticate first.");
                throw new InvalidOperationException("Access token is not available. Please authenticate first.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{range}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var valueRange = JsonSerializer.Deserialize<JsonElement>(content);

                var values = new List<List<object>>();
                if (valueRange.TryGetProperty("values", out var valuesElement))
                {
                    foreach (var row in valuesElement.EnumerateArray())
                    {
                        var rowValues = new List<object>();
                        foreach (var cell in row.EnumerateArray())
                        {
                            rowValues.Add(cell.GetString());
                        }
                        values.Add(rowValues);
                    }
                }

                _logger.LogInformation($"Successfully retrieved {values.Count} rows from Google Sheets");
                return values;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting values from Google Sheets");
                throw;
            }
        }

        public async Task UpdateValuesAsync(string spreadsheetId, string range, List<List<object>> values)
        {
            _logger.LogInformation($"Updating values for spreadsheet {spreadsheetId}, range {range}");

            if (string.IsNullOrEmpty(_accessToken))
            {
                _logger.LogWarning("Access token is not available. Please authenticate first.");
                throw new InvalidOperationException("Access token is not available. Please authenticate first.");
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put,
                    $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values/{range}?valueInputOption=USER_ENTERED");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

                var content = new
                {
                    values = values
                };
                request.Content = new StringContent(JsonSerializer.Serialize(content));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                var updateResult = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var updatedCells = updateResult.GetProperty("updatedCells").GetInt32();
                _logger.LogInformation($"Successfully updated {updatedCells} cells");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating values in Google Sheets");
                throw;
            }
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(_accessToken);
        }
    }
}