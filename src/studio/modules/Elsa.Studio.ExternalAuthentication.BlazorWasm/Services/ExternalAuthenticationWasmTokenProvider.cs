using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.BlazorWasm.Models;
using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Provides access tokens and performs single-use refresh-token rotation through the anonymous broker client.</summary>
public sealed class ExternalAuthenticationWasmTokenProvider(
    IExternalAuthenticationBrowserTokenStore tokenStore,
    IAnonymousBackendApiClientProvider anonymousBackendApiClientProvider,
    ExternalAuthenticationWasmOptions options) : IExternalAuthenticationTokenProvider
{
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromMinutes(1);
    private readonly SemaphoreSlim refreshLock = new(1, 1);

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await tokenStore.GetAsync(cancellationToken);
        if (tokens == null)
            return null;
        if (!RequiresRefresh(tokens))
            return tokens.AccessToken;

        await refreshLock.WaitAsync(cancellationToken);

        try
        {
            // Another request may have completed the rotation while this request waited.
            tokens = await tokenStore.GetAsync(cancellationToken);
            if (tokens == null)
                return null;
            if (!RequiresRefresh(tokens))
                return tokens.AccessToken;

            if (IsExternalSessionExpired(tokens) || tokens.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
            {
                await tokenStore.ClearAsync(cancellationToken);
                TokensChanged?.Invoke();
                return null;
            }

            var api = await anonymousBackendApiClientProvider.GetApiAsync<IExternalAuthenticationBrokerApi>(cancellationToken);
            var refreshed = await api.ExchangeAsync(
                new("refresh_token", options.ClientId, RefreshToken: tokens.RefreshToken),
                authorization: null,
                cancellationToken);
            await tokenStore.SetAsync(ToTokenSet(refreshed), cancellationToken);
            TokensChanged?.Invoke();
            return refreshed.AccessToken;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // A failed refresh may mean rotation reuse/revocation. Do not retain a potentially unusable browser session.
            await tokenStore.ClearAsync(cancellationToken);
            TokensChanged?.Invoke();
            return null;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    /// <summary>Stores an exchanged token response and notifies UI authentication state consumers.</summary>
    public async Task SetAsync(BrokerTokenResponse response, CancellationToken cancellationToken = default)
    {
        await tokenStore.SetAsync(ToTokenSet(response), cancellationToken);
        TokensChanged?.Invoke();
    }

    /// <summary>Clears the local credential copy after logout or failed callback processing.</summary>
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await tokenStore.ClearAsync(cancellationToken);
        TokensChanged?.Invoke();
    }

    /// <summary>Raised after a local credential change.</summary>
    public event Action? TokensChanged;

    private static Elsa.Studio.ExternalAuthentication.BlazorWasm.Models.ExternalAuthenticationTokenSet ToTokenSet(BrokerTokenResponse response)
    {
        var now = DateTimeOffset.UtcNow;
        var externalSessionLifetime = PositiveLifetime(response.ExternalSessionExpiresIn, response.ExpiresIn);
        var accessLifetime = Math.Min(PositiveLifetime(response.ExpiresIn), externalSessionLifetime);
        var refreshLifetime = response.RefreshExpiresIn > 0
            ? Math.Min(response.RefreshExpiresIn, externalSessionLifetime)
            : accessLifetime;
        return new(
            response.AccessToken,
            response.RefreshToken,
            now.AddSeconds(accessLifetime),
            now.AddSeconds(refreshLifetime),
            now.AddSeconds(externalSessionLifetime));
    }

    private static long PositiveLifetime(long primary, long fallback = 1) => Math.Max(1, primary > 0 ? primary : fallback);

    private static bool RequiresRefresh(ExternalAuthenticationTokenSet tokens) =>
        IsExternalSessionExpired(tokens) ||
        tokens.AccessTokenExpiresAt <= DateTimeOffset.UtcNow.Add(RefreshSkew);

    private static bool IsExternalSessionExpired(ExternalAuthenticationTokenSet tokens) =>
        tokens.ExternalSessionExpiresAt <= DateTimeOffset.UtcNow;
}
