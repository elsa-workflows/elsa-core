using Elsa.Http.Extensions;
using Elsa.Tenants.Abstractions;
using Elsa.Tenants.Contexts;
using Elsa.Tenants.Results;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.TenantResolvers;

/// <summary>
/// Resolves the tenant based on the Items collection of the current <see cref="HttpContext"/>.
/// </summary>
public class HttpContextTenantResolver(IHttpContextAccessor httpContextAccessor) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var tenantId = httpContext.GetTenantId();
        return AutoResolve(tenantId);
    }
}