using Elsa.Tenants.Abstractions;
using Elsa.Tenants.Constants;
using Elsa.Tenants.Options;
using Elsa.Tenants.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Resolvers;

/// <summary>
/// Resolves the tenant from the user's claims.
/// </summary>
public class ClaimsTenantResolver(IHttpContextAccessor httpContextAccessor, IOptions<TenantsOptions> options) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override TenantResolutionResult Resolve()
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var tenantId = httpContext.User.FindFirst(options.Value.TenantIdClaimsType ?? ClaimConstants.TenantId)?.Value;
        return AutoResolve(tenantId);
    }
}