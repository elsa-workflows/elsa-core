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

        if (tenant != null)
        {
            var tenantPrefix = tenant.GetRoutePrefix();

            if (!string.IsNullOrWhiteSpace(tenantPrefix))
            {
                var tenantPath = $"/{tenantPrefix}";
                if (context.Request.Path.StartsWithSegments(tenantPath))
                {
                    context.Request.PathBase = tenantPath;
                    context.Request.Path = context.Request.Path.Value![tenantPath.Length..];
                }
            }
        }

        using var tenantScope = tenantScopeFactory.CreateScope(tenant);
        var originalServiceProvider = context.RequestServices;
        context.RequestServices = tenantScope.ServiceProvider;
        await next(context);
        context.RequestServices = originalServiceProvider;
    }
}