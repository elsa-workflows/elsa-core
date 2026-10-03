using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Loads the current caller's effective permissions once per Studio scope.
/// Forbidden snapshots are cached until <see cref="Invalidate"/>; Unavailable is never cached.
/// Auth-state changes invalidate the cache so a later principal cannot keep the previous grants.
/// </summary>
public sealed class IdentityPermissionContext : IIdentityPermissionContext, IPermissionSnapshotCache, IDisposable
{
    private readonly IBackendApiClientProvider _apiClientProvider;
    private readonly ILogger<IdentityPermissionContext> _logger;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IdentityPermissionSnapshot? _snapshot;

    public IdentityPermissionContext(
        IBackendApiClientProvider apiClientProvider,
        ILogger<IdentityPermissionContext> logger,
        IServiceProvider? services = null)
    {
        _apiClientProvider = apiClientProvider;
        _logger = logger;
        _authenticationStateProvider = services?.GetService<AuthenticationStateProvider>();

        if (_authenticationStateProvider != null)
        {
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }
    }

    public async Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (HasCachedSnapshot)
            return _snapshot!;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (HasCachedSnapshot)
                return _snapshot!;

            try
            {
                var api = await _apiClientProvider.GetApiAsync<IMePermissionsApi>(cancellationToken);
                var response = await api.GetAsync(cancellationToken);
                var grants = response.Grants
                    .GroupBy(x => x.Resource, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlySet<string>)group
                            .SelectMany(x => x.Verbs)
                            .ToHashSet(StringComparer.Ordinal),
                        StringComparer.Ordinal);

                _snapshot = new IdentityPermissionSnapshot(IdentityPermissionSnapshotState.Ready, grants);
            }
            catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _snapshot = IdentityPermissionSnapshot.Forbidden;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Loading the current Identity permissions timed out");
                _snapshot = IdentityPermissionSnapshot.Unavailable;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Loading the current Identity permissions failed");
                _snapshot = IdentityPermissionSnapshot.Unavailable;
            }

            return _snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate() => _snapshot = null;

    public void Dispose()
    {
        if (_authenticationStateProvider != null)
        {
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        _loadLock.Dispose();
    }

    private bool HasCachedSnapshot =>
        _snapshot is { State: not IdentityPermissionSnapshotState.Unavailable };

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Invalidate();
}
