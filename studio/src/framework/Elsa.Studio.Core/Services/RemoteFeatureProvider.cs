using System.Net;
using System.Security.Cryptography;
using System.Text;
using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Refit;

namespace Elsa.Studio.Services;

/// <summary>
/// A feature service that uses a remote backend to retrieve feature flags.
/// </summary>
public class RemoteFeatureProvider(
    IBackendApiClientProvider remoteBackendApiClientProvider,
    AuthenticationStateProvider? authenticationStateProvider = null) : IRemoteFeatureProvider, IRemoteFeatureProviderWithFreshCheck
{
    private readonly SemaphoreSlim _catalogLock = new(1, 1);
    private IReadOnlyCollection<FeatureDescriptor>? _catalog;
    private string? _catalogContextKey;

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);
        return RemoteFeatureCatalog.Contains(catalog, featureName);
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken, forceRefresh: true);
        return RemoteFeatureCatalog.Contains(catalog, featureName);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
        await GetCatalogAsync(cancellationToken);

    private async Task<IReadOnlyCollection<FeatureDescriptor>> GetCatalogAsync(CancellationToken cancellationToken, bool forceRefresh = false)
    {
        var authChangeCount = 0;
        void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Interlocked.Increment(ref authChangeCount);
        if (authenticationStateProvider is not null)
            authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;

        try
        {
            var initialAuthChangeCount = Volatile.Read(ref authChangeCount);
            var contextKey = await GetContextKeyAsync();
            if (contextKey == null || Volatile.Read(ref authChangeCount) != initialAuthChangeCount)
                return [];
            if (!forceRefresh && _catalog is not null && _catalogContextKey == contextKey)
                return _catalog;

            await _catalogLock.WaitAsync(cancellationToken);
            try
            {
                contextKey = await GetContextKeyAsync();
                if (contextKey == null || Volatile.Read(ref authChangeCount) != initialAuthChangeCount)
                    return [];
                if (!forceRefresh && _catalog is not null && _catalogContextKey == contextKey)
                    return _catalog;

                var api = await remoteBackendApiClientProvider.GetApiAsync<IFeaturesApi>(cancellationToken);
                if (contextKey != await GetContextKeyAsync() || Volatile.Read(ref authChangeCount) != initialAuthChangeCount)
                    return [];

                try
                {
                    var response = await api.ListAsync(cancellationToken);
                    if (contextKey != await GetContextKeyAsync() || Volatile.Read(ref authChangeCount) != initialAuthChangeCount)
                        return [];

                    _catalog = response.Items.ToArray();
                    _catalogContextKey = contextKey;
                    return _catalog;
                }
                catch (ApiException e) when (e.StatusCode is HttpStatusCode.NotFound)
                {
                    if (contextKey != await GetContextKeyAsync() || Volatile.Read(ref authChangeCount) != initialAuthChangeCount)
                        return [];

                    _catalog = [];
                    _catalogContextKey = contextKey;
                    return _catalog;
                }
                catch (ApiException e) when (e.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    return [];
                }
            }
            finally
            {
                _catalogLock.Release();
            }
        }
        finally
        {
            if (authenticationStateProvider is not null)
                authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        async Task<string?> GetContextKeyAsync()
        {
            var backendContextKey = remoteBackendApiClientProvider.ContextKey;
            if (authenticationStateProvider is null)
                return backendContextKey;

            var state = await authenticationStateProvider.GetAuthenticationStateAsync();
            if (state.User.Identity?.IsAuthenticated != true)
                return null;

            return $"{backendContextKey}|{GetPrincipalFingerprint(state.User)}";
        }

        static string GetPrincipalFingerprint(System.Security.Claims.ClaimsPrincipal user)
        {
            var claims = user.Claims
                .Select(claim => $"{claim.Type}:{claim.Value}")
                .Order(StringComparer.Ordinal);
            var identity = string.Join("|", user.Identities.Select(x => $"{x.AuthenticationType}:{x.IsAuthenticated}"));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{identity}|{string.Join("|", claims)}")));
        }
    }
}
