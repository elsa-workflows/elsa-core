using System.Net;
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
    AuthenticationStateProvider? authenticationStateProvider = null) : IRemoteFeatureProvider
{
    private readonly SemaphoreSlim _catalogLock = new(1, 1);
    private IReadOnlyCollection<FeatureDescriptor>? _catalog;
    private Uri? _catalogUrl;

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var catalog = await GetCatalogAsync(cancellationToken);
        return catalog.Any(feature => string.Equals(feature.FullName, featureName, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
        await GetCatalogAsync(cancellationToken);

    private async Task<IReadOnlyCollection<FeatureDescriptor>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        if (authenticationStateProvider is not null)
        {
            var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
            if (authenticationState.User.Identity?.IsAuthenticated != true)
                return [];
        }

        if (_catalog is not null && _catalogUrl == remoteBackendApiClientProvider.Url)
            return _catalog;

        await _catalogLock.WaitAsync(cancellationToken);
        try
        {
            var api = await remoteBackendApiClientProvider.GetApiAsync<IFeaturesApi>(cancellationToken);
            var backendUrl = remoteBackendApiClientProvider.Url;

            if (_catalog is not null && _catalogUrl == backendUrl)
                return _catalog;

            try
            {
                var response = await api.ListAsync(cancellationToken);
                _catalog = response.Items.ToArray();
                _catalogUrl = backendUrl;
                return _catalog;
            }
            catch (ApiException e) when (e.StatusCode is HttpStatusCode.NotFound)
            {
                _catalog = [];
                _catalogUrl = backendUrl;
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
}
