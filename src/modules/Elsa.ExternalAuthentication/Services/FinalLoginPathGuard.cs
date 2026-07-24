using System.Security.Claims;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Permissions;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>Prevents a management edit from removing the final ordinary sign-in path by accident.</summary>
public sealed class FinalLoginPathGuard(IIdentityProviderConnectionRegistry registry, IOptions<ExternalAuthenticationOptions> options)
{
    public async ValueTask<FinalLoginPathGuardResult> AuthorizeAsync(IdentityProviderConnection existing, IdentityProviderConnection candidate, string targetTenantId, ClaimsPrincipal actor, bool confirmedOverride, CancellationToken cancellationToken = default)
    {
        var guard = options.Value.FinalLoginPathGuard;
        if (!guard.IsEnabled || !guard.RequireRecoveryMethod || !IsNormal(existing) || IsNormal(candidate) || guard.HasBreakGlassAuthentication || options.Value.LocalLogin.IsEnabled)
            return FinalLoginPathGuardResult.Allowed;
        var effective = await registry.GetAsync(targetTenantId, cancellationToken);
        if (effective.Connections.Any(x => !string.Equals(x.Connection.Id, existing.Id, StringComparison.Ordinal) && IsNormal(x.Connection) && !x.IsShadowed && x.Validity != ConnectionValidity.Invalid))
            return FinalLoginPathGuardResult.Allowed;
        var canOverride = confirmedOverride && actor.FindAll(PermissionNames.ClaimType).Any(x => x.Value == PermissionNames.All || x.Value == guard.PrivilegedOverridePermission);
        return canOverride ? FinalLoginPathGuardResult.Allowed : FinalLoginPathGuardResult.Denied;
    }

    private static bool IsNormal(IdentityProviderConnection connection) => connection.IsEnabled && connection.ArchivedAt is null;
}

public enum FinalLoginPathGuardResult { Allowed, Denied }
