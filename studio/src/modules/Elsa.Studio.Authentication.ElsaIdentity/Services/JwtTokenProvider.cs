using System.Security.Claims;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <inheritdoc />
public class JwtTokenProvider(
    IJwtAccessor jwtAccessor,
    IJwtParser jwtParser,
    ISingleFlightCoordinator refreshCoordinator,
    IRefreshTokenService refreshTokenService,
    IEnumerable<IPermissionRefreshSignal> refreshSignals) : ITokenProvider
{
    public JwtTokenProvider(
        IJwtAccessor jwtAccessor,
        IJwtParser jwtParser,
        ISingleFlightCoordinator refreshCoordinator,
        IRefreshTokenService refreshTokenService)
        : this(jwtAccessor, jwtParser, refreshCoordinator, refreshTokenService, [])
    {
    }

    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(2);
    private static readonly StringComparer ClaimComparer = StringComparer.Ordinal;

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        if (!IsExpiredOrNearExpiry(accessToken))
            return accessToken;

        // Single-flight refresh: multiple concurrent API calls shouldn't trigger multiple refresh requests.
        var refreshResponse = await refreshCoordinator.RunAsync(refreshTokenService.RefreshTokenAsync, cancellationToken);

        if (!refreshResponse.IsAuthenticated)
        {
            // Refresh failed: clear local tokens so the app can transition to unauthenticated state.
            await jwtAccessor.ClearTokensAsync();
            NotifyRefresh();
            return null;
        }

        var refreshedToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        if (IdentityChanged(accessToken, refreshedToken))
            NotifyRefresh();

        return refreshedToken;
    }

    private void NotifyRefresh()
    {
        foreach (var signal in refreshSignals)
            signal.Raise();
    }

    private bool IdentityChanged(string previousToken, string? refreshedToken)
    {
        if (string.IsNullOrWhiteSpace(refreshedToken))
            return true;

        try
        {
            var previous = jwtParser.Parse(previousToken).ToList();
            var refreshed = jwtParser.Parse(refreshedToken).ToList();
            return !SameClaimValues(previous, refreshed, "sub") ||
                   !SameClaimValues(previous, refreshed, "permissions");
        }
        catch
        {
            return true;
        }
    }

    private static bool SameClaimValues(IReadOnlyCollection<Claim> previous, IReadOnlyCollection<Claim> refreshed, string type)
    {
        var previousValues = previous.Where(x => x.Type == type).Select(x => x.Value).OrderBy(x => x, ClaimComparer);
        var refreshedValues = refreshed.Where(x => x.Type == type).Select(x => x.Value).OrderBy(x => x, ClaimComparer);
        return previousValues.SequenceEqual(refreshedValues, ClaimComparer);
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
