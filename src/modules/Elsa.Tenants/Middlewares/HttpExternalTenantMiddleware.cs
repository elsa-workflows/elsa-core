using Elsa.Common.Contracts;
using Elsa.Tenants.Contracts;
using Elsa.Tenants.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Elsa.Tenants.Middlewares;

/// <summary>
/// Middleware to set the current tenant id from the user's claims.
/// </summary>
public class HttpExternalTenantMiddleware(ITenantAccessor tenantAccessor, IOptions<TenantsOptions> options) : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        string? tenantId = httpContext.User.FindFirst(options.Value.CustomTenantIdClaimsType ?? ClaimConstants.TenantId)?.Value;
        tenantAccessor.SetCurrentTenantId(tenantId);

        await next(httpContext);
    }
}
