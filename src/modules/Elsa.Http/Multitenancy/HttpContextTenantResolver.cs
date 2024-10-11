using Elsa.Common.Multitenancy;
using Elsa.Http.Extensions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.Multitenancy;

/// <summary>
/// Resolves the tenant based on the Items collection of the current <see cref="HttpContext"/>.
/// </summary>
public class HttpContextTenantResolver(IHttpContextAccessor httpContextAccessor) : TenantResolverBase
{
    /// <inheritdoc />
    protected override TenantResolverResult Resolve(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var tenantId = httpContext.GetTenantId();
        return AutoResolve(tenantId);
    }
}