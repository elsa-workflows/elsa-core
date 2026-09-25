using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <inheritdoc />
public class JwtTokenProvider(
    IJwtAccessor jwtAccessor,
    IJwtParser jwtParser,
    ISingleFlightCoordinator refreshCoordinator,
    IRefreshTokenService refreshTokenService) : ITokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync("accessToken");

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        if (!IsExpiredOrNearExpiry(accessToken))
            return accessToken;

        // Single-flight refresh: multiple concurrent API calls shouldn't trigger multiple refresh requests.
        var refreshResponse = await refreshCoordinator.RunAsync(refreshTokenService.RefreshTokenAsync, cancellationToken);

        if (!refreshResponse.IsAuthenticated)
        {
            // Refresh failed: clear local tokens so the app can transition to unauthenticated state.
            await jwtAccessor.ClearTokenAsync("accessToken");
            await jwtAccessor.ClearTokenAsync("refreshToken");
            await jwtAccessor.ClearTokenAsync("idToken");
            return null;
        }

        return await jwtAccessor.ReadTokenAsync("accessToken");
    }

    private bool IsExpiredOrNearExpiry(string jwt)
    {
        try
        {
            var claims = jwtParser.Parse(jwt).ToList();
            var expString = claims.FirstOrDefault(x => x.Type == "exp")?.Value.Trim();
            if (string.IsNullOrWhiteSpace(expString) || !long.TryParse(expString, out var exp))
                return false;

            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp);
            return expiresAt <= DateTimeOffset.UtcNow.Add(RefreshSkew);
        }
        catch
        {
            // If parsing fails, don't attempt refresh here.
            return false;
        }
    }
}
