using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Authorization;

/// <summary>
/// Reads the <c>permissions</c> claims Elsa issues into its access tokens (Elsa Identity, and the external
/// authentication broker, which issues Elsa tokens).
/// </summary>
/// <remarks>
/// Fails open: when there is no authentication state, the user is not authenticated, or the principal carries no
/// <c>permissions</c> claims at all, the result is <see cref="UserPermissions.Unknown"/> and Studio renders
/// everything, exactly as it did before permission gating. Providers that do not put Elsa permissions into the
/// token (for example a third-party OpenID Connect provider) therefore keep today's behavior, and route-level
/// authentication remains the job of <c>AuthorizeRouteView</c>.
/// </remarks>
public sealed class ClaimsPermissionService(AuthenticationStateProvider? authenticationStateProvider = null) : IPermissionService
{
    /// <summary>The claim type Elsa uses for permission grants.</summary>
    public const string ClaimType = "permissions";

    /// <inheritdoc />
    public async ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        if (authenticationStateProvider is null)
            return UserPermissions.Unknown;

        var user = (await authenticationStateProvider.GetAuthenticationStateAsync()).User;

        if (user.Identity?.IsAuthenticated != true)
            return UserPermissions.Unknown;

        var claims = user.FindAll(ClaimType).ToList();

        if (claims.Count == 0)
            return UserPermissions.Unknown;

        // Malformed values are skipped rather than failing the whole set, matching the backend evaluator.
        var grants = claims
            .Select(claim => Permission.TryParse(claim.Value, out var permission) ? permission : (Permission?)null)
            .OfType<Permission>();

        return UserPermissions.FromGrants(grants);
    }
}
