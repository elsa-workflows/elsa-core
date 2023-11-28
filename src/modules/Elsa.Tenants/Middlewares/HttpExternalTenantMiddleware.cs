using Elsa.Common.Contracts;
using Elsa.Tenants.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Elsa.Tenants.Middlewares;

public class HttpExternalTenantMiddleware : IMiddleware
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IOptions<TenantsOptions> _tenantOptions;

    public HttpExternalTenantMiddleware(ITenantAccessor tenantAccessor, IOptions<TenantsOptions> options)
    {
        _tenantAccessor = tenantAccessor;
        _tenantOptions = options;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        string? tenantId = httpContext?.User?.FindFirst(_tenantOptions.Value.CustomTenantIdClaimsType ?? ClaimConstants.TenantId)?.Value;
        _tenantAccessor.SetCurrentTenantId(tenantId);

        await next(httpContext);
        return;
    }
}
