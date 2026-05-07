using BriefingTool.Config;
using BriefingTool.Services.Interfaces;
using System.Text.Json.Serialization;

namespace BriefingTool.Services;

public class TokenService(AzureAdConfig azureAdConfig, HttpClient httpClient) : ITokenService
{ 
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public async Task<string> GetAccessTokenAsync()
    { 
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry.AddMinutes(-1))
            return _cachedToken;

        var tokenEndpoint = $"https://login.microsoftonline.com/{azureAdConfig.TenantId}/oauth2/v2.0/token";

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = azureAdConfig.ClientId,
            ["client_secret"] = azureAdConfig.ClientSecret,
            ["scope"] = azureAdConfig.Scope
        });

        var response = await httpClient.PostAsync(tokenEndpoint, body);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("Failed to deserialize token response");

        _cachedToken = result.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);

        return _cachedToken;
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn
    );
}
