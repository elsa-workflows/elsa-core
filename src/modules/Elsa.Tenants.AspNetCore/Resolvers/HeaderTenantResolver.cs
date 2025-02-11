using Elsa.Common.Multitenancy;
using Elsa.Tenants.AspNetCore.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.AspNetCore;

/// <summary>
/// Resolves the tenant based on the header in the request.
/// </summary>
public class HeaderTenantResolver(IHttpContextAccessor httpContextAccessor, IOptions<MultitenancyHttpOptions> options) : TenantResolverBase
{
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var headerName = options.Value.TenantHeaderName;
        var tenantId = httpContext.Request.Headers[headerName].FirstOrDefault();

        return AutoResolve(tenantId);
    }
}