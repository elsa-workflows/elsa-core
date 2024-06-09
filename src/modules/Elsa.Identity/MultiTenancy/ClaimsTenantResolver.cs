using Elsa.Framework.Tenants;
using Elsa.Identity.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.Identity.MultiTenancy;

/// <summary>
/// Resolves the tenant from the user's claims.
/// </summary>
public class ClaimsTenantResolver(IHttpContextAccessor httpContextAccessor, IOptions<IdentityTokenOptions> options) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override TenantResolutionResult Resolve(TenantResolutionContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Unresolved();

        var tenantId = httpContext.User.FindFirst(options.Value.TenantIdClaimsType)?.Value;
        return AutoResolve(tenantId);
    }
}