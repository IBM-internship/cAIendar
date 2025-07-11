using Microsoft.AspNetCore.Identity;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using AiCalendarAssistant.Data.Models;

namespace AiCalendarAssistant.Services;

public class TokenRefreshService(
    IHttpContextAccessor httpContextAccessor,
    ILogger<TokenRefreshService> logger,
    UserManager<ApplicationUser> userManager,
    string googleClientId,
    string googleClientSecret)
{
    public async Task<string?> GetValidAccessTokenAsync()
    {
        var context = httpContextAccessor.HttpContext!;
        var user = await userManager.GetUserAsync(context.User);
        
        if (user == null)
        {
            logger.LogWarning("User not found");
            return null;
        }

        var accessToken = await userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
        var refreshToken = await userManager.GetAuthenticationTokenAsync(user, "Google", "refresh_token");
        var expiresAtString = await userManager.GetAuthenticationTokenAsync(user, "Google", "expires_at");

        logger.LogInformation("Access token exists: {B}", !string.IsNullOrEmpty(accessToken));
        logger.LogInformation("Refresh token exists: {B}", !string.IsNullOrEmpty(refreshToken));
        logger.LogInformation("Expires at: {ExpiresAtString}", expiresAtString);

        if (string.IsNullOrEmpty(accessToken))
        {
            logger.LogWarning("No access token found - user needs to re-authenticate with Google for Gmail access");
            return null;
        }

        if (string.IsNullOrEmpty(expiresAtString) ||
            !DateTimeOffset.TryParse(expiresAtString, out var expiresAt) ||
            expiresAt > DateTimeOffset.UtcNow.AddMinutes(5)) return accessToken;
        
        logger.LogInformation("Access token is expired or expiring soon, attempting refresh");

        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogWarning("No refresh token available");
            return null;
        }

        var newToken = await RefreshAccessTokenAsync(refreshToken, user);
        return string.IsNullOrEmpty(newToken) ? accessToken : newToken;

    }

    private async Task<string?> RefreshAccessTokenAsync(string refreshToken, ApplicationUser user)
    {
        try
        {
            if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
            {
                logger.LogError("Google OAuth credentials not configured");
                return null;
            }

            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = googleClientId,
                    ClientSecret = googleClientSecret
                },
                Scopes =
                [
                    "https://www.googleapis.com/auth/gmail.modify",
                    "email",
                    "profile"
                ]
            });

            var tokenResponse = await flow.RefreshTokenAsync("user", refreshToken, CancellationToken.None);

            if (tokenResponse?.AccessToken != null)
            {
                await UpdateStoredTokensAsync(tokenResponse, user);
                logger.LogInformation("Successfully refreshed access token");
                return tokenResponse.AccessToken;
            }

            logger.LogWarning("Token refresh returned null access token");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing access token");
            return null;
        }
    }

    private async Task UpdateStoredTokensAsync(TokenResponse tokenResponse, ApplicationUser user)
    {
        try
        {
            await userManager.SetAuthenticationTokenAsync(user, "Google", "access_token", tokenResponse.AccessToken);
            
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
            {
                await userManager.SetAuthenticationTokenAsync(user, "Google", "refresh_token", tokenResponse.RefreshToken);
            }

            if (tokenResponse.ExpiresInSeconds.HasValue)
            {
                var expiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds.Value);
                await userManager.SetAuthenticationTokenAsync(user, "Google", "expires_at", expiresAt.ToString("o"));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating stored tokens");
        }
    }

    public async Task<bool> IsTokenValidAsync()
    {
        var token = await GetValidAccessTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<TokenInfo?> GetTokenInfoAsync()
    {
        var context = httpContextAccessor.HttpContext!;
        var user = await userManager.GetUserAsync(context.User);
        
        if (user == null)
            return null;

        var accessToken = await userManager.GetAuthenticationTokenAsync(user, "Google", "access_token");
        var refreshToken = await userManager.GetAuthenticationTokenAsync(user, "Google", "refresh_token");
        var expiresAtString = await userManager.GetAuthenticationTokenAsync(user, "Google", "expires_at");

        if (string.IsNullOrEmpty(accessToken))
            return null;

        DateTimeOffset.TryParse(expiresAtString, out var expiresAt);

        return new TokenInfo
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            IsExpired = expiresAt <= DateTimeOffset.UtcNow
        };
    }
}

public class TokenInfo
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
}