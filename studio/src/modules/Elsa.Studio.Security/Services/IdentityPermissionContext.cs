using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
/// <remarks>
/// A snapshot is stored only for the generation it was loaded under, and <see cref="GetAsync"/>
/// returns only current-generation grants. Generation and snapshot are published together.
/// An overtaken load is retried at most <see cref="MaxOvertakenReloads"/> times, then
/// <see cref="IdentityPermissionSnapshot.Unavailable"/> is returned without being cached.
/// </remarks>
public sealed class IdentityPermissionContext : IIdentityPermissionContext, IPermissionSnapshotCache, IDisposable
{
    internal const int MaxOvertakenReloads = 3;
    private readonly IBackendApiClientProvider _apiClientProvider;
    private readonly ILogger<IdentityPermissionContext> _logger;
    private readonly AuthenticationStateProvider[] _authenticationStateProviders;
    private readonly IPermissionRefreshSignal[] _refreshSignals;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly object _sync = new();
    private CacheState _state = new(0, null);

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

    /// <summary>
    /// Test-only hook invoked after a load returns and before the generation check-and-store.
    /// Production code must not set this.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal Action? AfterLoad { get; set; }

    public async Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        if (TryReadCachedSnapshot(out var cached))
            return cached;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            var overtakenReloads = 0;
            while (true)
            {
                int generation;
                lock (_sync)
                {
                    if (IsUsable(_state.Snapshot))
                        return _state.Snapshot!;

                    generation = _state.Generation;
                }

                var loaded = await LoadAsync(cancellationToken);
                AfterLoad?.Invoke();

                lock (_sync)
                {
                    if (generation != _state.Generation)
                    {
                        if (IsUsable(_state.Snapshot))
                            return _state.Snapshot!;

                        if (++overtakenReloads >= MaxOvertakenReloads)
                            return IdentityPermissionSnapshot.Unavailable;

                        continue;
                    }

                    if (loaded.State != IdentityPermissionSnapshotState.Unavailable)
                        _state = new CacheState(generation, loaded);

                    return loaded;
                }
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate()
    {
        lock (_sync)
            _state = new CacheState(_state.Generation + 1, null);

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

    private bool TryReadCachedSnapshot(out IdentityPermissionSnapshot snapshot)
    {
        var cached = Volatile.Read(ref _state).Snapshot;
        if (IsUsable(cached))
        {
            snapshot = cached;
            return true;
        }

        snapshot = null!;
        return false;
    }

    private static bool IsUsable([NotNullWhen(true)] IdentityPermissionSnapshot? snapshot) =>
        snapshot is { State: not IdentityPermissionSnapshotState.Unavailable };

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

    private sealed record CacheState(int Generation, IdentityPermissionSnapshot? Snapshot);
}
