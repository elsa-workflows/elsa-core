using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Elsa.Tenants.AspNetCore.Middleware;

/// <summary>
/// Middleware to initialize the tenant for each incoming HTTP request.
/// </summary>
[UsedImplicitly]
public class TenantResolutionMiddleware(RequestDelegate next, ITenantScopeFactory tenantScopeFactory)
{
    /// <summary>
    /// Invokes the middleware to ensure the tenant is initialized.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="tenantResolverPipelineInvoker"></param>
    public async Task InvokeAsync(HttpContext context, ITenantResolverPipelineInvoker tenantResolverPipelineInvoker)
    {
        var tenant = await tenantResolverPipelineInvoker.InvokePipelineAsync();

        using (tenantScopeFactory.CreateScope(tenant)) 
            await next(context);
    }
}