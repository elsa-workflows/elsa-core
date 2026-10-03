using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Loads the current caller's effective permissions once per Studio scope.
/// Forbidden snapshots are cached until <see cref="Invalidate"/>; Unavailable is never cached.
/// This type is the single owner of the permission snapshot: it subscribes to authentication,
/// environment, and silent-refresh signals, drops the cache, then raises <see cref="Changed"/>
/// so UI re-resolves after the snapshot is gone.
/// </summary>
public sealed class IdentityPermissionContext : IIdentityPermissionContext, IPermissionSnapshotCache, IDisposable
{
    private readonly IBackendApiClientProvider _apiClientProvider;
    private readonly ILogger<IdentityPermissionContext> _logger;
    private readonly AuthenticationStateProvider[] _authenticationStateProviders;
    private readonly IPermissionRefreshSignal[] _refreshSignals;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private IdentityPermissionSnapshot? _snapshot;
    private int _generation;

    public IdentityPermissionContext(
        IBackendApiClientProvider apiClientProvider,
        ILogger<IdentityPermissionContext> logger,
        IEnumerable<AuthenticationStateProvider>? authenticationStateProviders = null,
        IEnumerable<IPermissionRefreshSignal>? refreshSignals = null)
    {
        _apiClientProvider = apiClientProvider;
        _logger = logger;
        _authenticationStateProviders = authenticationStateProviders?.ToArray() ?? [];
        _refreshSignals = refreshSignals?.ToArray() ?? [];

        foreach (var provider in _authenticationStateProviders)
            provider.AuthenticationStateChanged += OnAuthenticationStateChanged;

        foreach (var signal in _refreshSignals)
            signal.Raised += OnBackendContextChanged;
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    public async Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (HasCachedSnapshot)
            return _snapshot!;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (HasCachedSnapshot)
                return _snapshot!;

            var generation = Volatile.Read(ref _generation);
            var loaded = await LoadAsync(cancellationToken);

            if (generation != Volatile.Read(ref _generation))
            {
                return HasCachedSnapshot ? _snapshot! : loaded;
            }

            if (loaded.State != IdentityPermissionSnapshotState.Unavailable)
                _snapshot = loaded;

            return loaded;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref _generation);
        _snapshot = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        foreach (var provider in _authenticationStateProviders)
            provider.AuthenticationStateChanged -= OnAuthenticationStateChanged;

        foreach (var signal in _refreshSignals)
            signal.Raised -= OnBackendContextChanged;

        _loadLock.Dispose();
    }

    private bool HasCachedSnapshot =>
        _snapshot is { State: not IdentityPermissionSnapshotState.Unavailable };

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Invalidate();

    private void OnBackendContextChanged() => Invalidate();

    private async Task<IdentityPermissionSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
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

            return new IdentityPermissionSnapshot(IdentityPermissionSnapshotState.Ready, grants);
        }
        catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return IdentityPermissionSnapshot.Forbidden;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Loading the current Identity permissions timed out");
            return IdentityPermissionSnapshot.Unavailable;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Loading the current Identity permissions failed");
            return IdentityPermissionSnapshot.Unavailable;
        }
    }
}
