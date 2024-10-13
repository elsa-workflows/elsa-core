using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.AspNetCore;

/// <summary>
/// Resolves the tenant based on the header in the request.
/// </summary>
/// <param name="httpContextAccessor"></param>
public class HeaderTenantResolver(IHttpContextAccessor httpContextAccessor) : TenantResolverBase
{
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault();

        return AutoResolve(tenantId);
    }
}