using Elsa.Common.Abstractions;
using Elsa.Common.Contexts;
using Elsa.Common.Results;
using Elsa.Http.Extensions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.MultiTenancy;

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