using Elsa.Framework.Tenants;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.MultiTenancy;

/// <summary>
/// Resolves the tenant based on the route prefix in the request URL. The tenant ID is expected to be part of the route.
/// </summary>
public class RoutePrefixTenantResolver(IHttpContextAccessor httpContextAccessor, ITenantsProvider tenantsProvider, IServiceProvider serviceProvider) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override async ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext == null)
            return Unresolved();
        
        var tenantId = httpContext.Items["TenantId"] as string;
        return AutoResolve(tenantId);
    }
}