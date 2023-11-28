using Elsa.Common.Contracts;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Middlewares;

public class HttpTenantMiddleware : IMiddleware
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IUserProvider _userProvider;

    public HttpTenantMiddleware(ITenantAccessor tenantAccessor, IUserProvider userProvider)
    {
        _tenantAccessor = tenantAccessor;
        _userProvider = userProvider;
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        string? tenantId = null;

        var userName = httpContext?.User?.Identity?.Name;

        if (userName != null)
        {
            var user = await _userProvider.FindAsync(new UserFilter { Name = userName });
            tenantId = user?.TenantId;
        }

        _tenantAccessor.SetCurrentTenantId(tenantId);

        await next(httpContext);
        return;
    }
}
