using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.AspNetCore;

/// <summary>
/// Resolves the tenant based on the host in the request.
/// </summary>
public class HostTenantResolver(IHttpContextAccessor httpContextAccessor) : TenantResolverBase
{
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var host = httpContext.Request.Host;
        var inboundHostAndPort = host.Host + (host.Port.HasValue ? ":" + host.Port : null);
        var tenant = context.FindTenant(x =>
        {
            var tenantHost = x.GetHost();
            return tenantHost == inboundHostAndPort;
        });

        var tenantId = tenant?.Id;

        return AutoResolve(tenantId);
    }
}