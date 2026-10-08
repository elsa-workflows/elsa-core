using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Components;

/// <summary>
/// Base class for components that resolve the current user's permissions themselves. Once tracking starts, the
/// permissions are resolved again whenever the permission snapshot is invalidated (sign-in change, environment
/// switch, silent token refresh), and a resolution overtaken by a newer one is ignored.
/// </summary>
/// <remarks>
/// When no <see cref="IPermissionService"/> is registered, resolves to <see cref="UserPermissions.Unknown"/>
/// so hosts without Security keep rendering. The production Identity adapter never returns Unknown.
/// Re-resolution is driven by <see cref="IPermissionSnapshotCache.Changed"/> so the cache is already empty
/// when the component reloads, regardless of which object constructed the cache first. Hosts without a cache
/// still follow <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/>.
/// </remarks>
public abstract class PermissionTrackingComponentBase : ComponentBase, IDisposable
{
    private AuthenticationStateProvider? _authenticationStateProvider;
    private IPermissionSnapshotCache[] _caches = [];
    private bool _isTracking;
    private int _resolution;

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The resolved permissions, or <c>null</c> until tracking has resolved them.</summary>
    protected UserPermissions? TrackedPermissions { get; private set; }

    /// <summary>Resolves the permissions and keeps <see cref="TrackedPermissions"/> current until the component is disposed.</summary>
    protected async Task TrackPermissionsAsync()
    {
        if (!_isTracking)
        {
            _isTracking = true;
            _caches = Services.GetServices<IPermissionSnapshotCache>().ToArray();
            if (_caches.Length > 0)
            {
                foreach (var cache in _caches)
                    cache.Changed += OnSnapshotChanged;
            }
            else
            {
                _authenticationStateProvider = Services.GetService<AuthenticationStateProvider>();
                if (_authenticationStateProvider != null)
                    _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
            }
        }

        await ResolvePermissionsAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var cache in _caches)
            cache.Changed -= OnSnapshotChanged;

        if (_authenticationStateProvider != null)
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private async Task ResolvePermissionsAsync()
    {
        var resolution = ++_resolution;
        var permissionService = Services.GetService<IPermissionService>();
        var permissions = permissionService != null
            ? await permissionService.GetPermissionsAsync()
            : UserPermissions.Unknown;

        // Ignore the result if permissions were resolved again while this one was resolving.
        if (resolution == _resolution)
        {
            TrackedPermissions = permissions;
        }
    }

    private async void OnSnapshotChanged(object? sender, EventArgs e) => await RefreshUiAsync();

    // Keeps the current permissions while resolving, so permitted content is not unmounted while it runs.
    private async void OnAuthenticationStateChanged(Task<AuthenticationState> state) => await RefreshUiAsync();

    private async Task RefreshUiAsync()
    {
        try
        {
            await InvokeAsync(async () =>
            {
                await ResolvePermissionsAsync();
                StateHasChanged();
            });
        }
        catch
        {
            // A permission notification must not become an unobserved exception.
        }
    }
}
