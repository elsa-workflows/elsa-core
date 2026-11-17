using System.Security.Claims;
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Endpoints.Tenants;

internal static class TenantAdministration
{
    public static bool HasHostAuthority(ClaimsPrincipal user) =>
        user.Identities.Any(identity => identity.IsAuthenticated &&
            !identity.HasClaim(claim => claim.Type == TenantPrincipalClaims.BoundTenant) &&
            identity.HasClaim(TenantPrincipalClaims.HostAdministrator, "true"));

    public static IDisposable? BeginHostScope(IServiceProvider services) => services.GetService<ITenantAccessor>()?.PushContext(null);

    public static string NormalizeRouteId(string id) => id == "-" ? Tenant.DefaultTenantId : id;

    public static Tenant ToDirectoryEntry(Tenant tenant) => new()
    {
        Id = tenant.Id,
        TenantId = tenant.TenantId,
        Name = tenant.Name
    };
}
