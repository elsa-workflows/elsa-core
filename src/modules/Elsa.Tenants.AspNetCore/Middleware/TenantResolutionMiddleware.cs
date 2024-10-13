using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

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

        if (tenant != null)
        {
            var tenantPrefix = $"/{tenant.GetRoutePrefix()}";

            if (context.Request.Path.StartsWithSegments(tenantPrefix))
            {
                context.Request.PathBase = tenantPrefix;
                context.Request.Path = context.Request.Path.Value![tenantPrefix.Length..];
            }
        }

        using (tenantScopeFactory.CreateScope(tenant))
        {
            await next(context);
        }
    }
}