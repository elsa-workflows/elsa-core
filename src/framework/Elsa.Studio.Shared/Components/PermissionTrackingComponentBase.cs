using Elsa.Studio.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Components;

/// <summary>
/// Base class for components that resolve the current user's permissions themselves. Once tracking starts, the
/// permissions are resolved again whenever the authentication state changes (e.g. a refreshed token carrying new
/// grants), and a resolution overtaken by a newer one is ignored.
/// </summary>
/// <remarks>
/// Resolves to <see cref="UserPermissions.Unknown"/>, which permits everything, when no
/// <see cref="IPermissionService"/> is registered.
/// </remarks>
public abstract class PermissionTrackingComponentBase : ComponentBase, IDisposable
{
    private AuthenticationStateProvider? _authenticationStateProvider;
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
            _authenticationStateProvider = Services.GetService<AuthenticationStateProvider>();
            if (_authenticationStateProvider != null)
                _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        await ResolvePermissionsAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_authenticationStateProvider != null)
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private async Task ResolvePermissionsAsync()
    {
        var resolution = ++_resolution;
        var permissionService = Services.GetService<IPermissionService>();
        var permissions = permissionService != null ? await permissionService.GetPermissionsAsync() : UserPermissions.Unknown;

        // Ignore the result if permissions were resolved again while this one was resolving.
        if (resolution == _resolution)
            TrackedPermissions = permissions;
    }

    // Keeps the current permissions while resolving, so permitted content is not unmounted while it runs.
    private async void OnAuthenticationStateChanged(Task<AuthenticationState> state) =>
        await InvokeAsync(async () =>
        {
            await ResolvePermissionsAsync();
            StateHasChanged();
        });
}
