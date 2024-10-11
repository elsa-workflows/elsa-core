using Elsa.Common.Multitenancy;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.AspNetCore.Middleware;

/// <summary>
/// Middleware to initialize the tenant for each incoming HTTP request.
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next, ITenantResolverPipelineInvoker tenantResolverPipelineInvoker, ITenantAccessor tenantAccessor)
{
    /// <summary>
    /// Invokes the middleware to ensure the tenant is initialized.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var tenant = await tenantResolverPipelineInvoker.InvokePipelineAsync();
        tenantAccessor.CurrentTenant = tenant;
        await next(context);
    }
}