using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
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
    IEnumerable<IPermissionRefreshSignal> refreshSignals) : ITokenProvider, IBackendRequestContextEpochSource
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
    private static readonly HashSet<string> LifecycleClaimTypes = new(StringComparer.Ordinal)
    {
        "exp",
        "iat",
        "nbf",
        "jti"
    };
    private readonly object _identityObservationLock = new();
    private byte[]? _observedIdentityFingerprint;
    private bool _hasObservedIdentity;

    /// <inheritdoc />
    public bool UsesPermissionRefreshSignal(IPermissionRefreshSignal signal) => refreshSignals.Any(x => ReferenceEquals(x, signal));

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        ObserveAccessToken(accessToken);

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
        ObserveAccessToken(refreshedToken);

        return refreshedToken;
    }

    private void ObserveAccessToken(string? accessToken)
    {
        byte[]? fingerprint = null;
        var parseFailed = false;

        if (!string.IsNullOrWhiteSpace(accessToken) && !TryCreateIdentityFingerprint(accessToken, out fingerprint))
            parseFailed = true;

        var changed = false;
        lock (_identityObservationLock)
        {
            changed = _hasObservedIdentity && !FingerprintsEqual(_observedIdentityFingerprint, fingerprint);
            _observedIdentityFingerprint = fingerprint;
            _hasObservedIdentity = true;
        }

        // Invalid tokens cannot establish a trustworthy identity epoch. Invalidate an active
        // native binding on every parse failure, even when the previous observation was also invalid.
        if (changed || parseFailed)
            NotifyRefresh();
    }

    private bool TryCreateIdentityFingerprint(string accessToken, out byte[]? fingerprint)
    {
        try
        {
            var claims = jwtParser.Parse(accessToken)?.ToArray();
            if (claims is not { Length: > 0 })
            {
                fingerprint = null;
                return false;
            }

            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            foreach (var claim in claims
                         .Where(x => !LifecycleClaimTypes.Contains(x.Type))
                         .OrderBy(x => x.Type, ClaimComparer)
                         .ThenBy(x => x.Value, ClaimComparer))
            {
                AppendCanonicalString(hash, claim.Type);
                AppendCanonicalString(hash, claim.Value);
            }

            fingerprint = hash.GetHashAndReset();
            return true;
        }
        catch
        {
            // Preserve ordinary token-provider behavior while making native request bindings fail closed.
            fingerprint = null;
            return false;
        }
    }

    private static void AppendCanonicalString(IncrementalHash hash, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        Span<byte> length = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
        hash.AppendData(length);
        hash.AppendData(bytes);
    }

    private static bool FingerprintsEqual(byte[]? left, byte[]? right) =>
        left == null ? right == null : right != null && CryptographicOperations.FixedTimeEquals(left, right);

    private void NotifyRefresh()
    {
        foreach (var signal in refreshSignals)
            signal.Raise();
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
