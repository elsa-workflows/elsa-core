using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Tenants.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.Middlewares;

/// <summary>
/// Middleware that sets the current tenant ID based on the authenticated user.
/// </summary>
public class HttpCurrentUserTenantResolverMiddleware(ITenantAccessor tenantAccessor, IUserProvider userProvider) : IMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        string? tenantId = null;

        var userName = httpContext.User.Identity?.Name;

        if (userName != null)
        {
            var user = await userProvider.FindAsync(new UserFilter { Name = userName });
            tenantId = user?.TenantId;
        }

        tenantAccessor.SetCurrentTenantId(tenantId);
        await next(httpContext);
    }
}
