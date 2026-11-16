using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
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
    /// <param name="tenantResolverPipelineInvoker">The tenant resolver pipeline invoker.</param>
    /// <param name="options">The multitenancy options.</param>
    public async Task InvokeAsync(HttpContext context, ITenantResolverPipelineInvoker tenantResolverPipelineInvoker, IOptions<MultitenancyOptions> options)
    {
        var tenant = await tenantResolverPipelineInvoker.InvokePipelineAsync();

        if (tenant == null && options.Value.RejectUnresolvedTenants)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("The tenant could not be resolved.");
            return;
        }

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

        await using var tenantScope = tenantScopeFactory.CreateScope(tenant);
        var originalServiceProvider = context.RequestServices;
        context.RequestServices = tenantScope.ServiceProvider;

        try
        {
            await next(context);
        }
        finally
        {
            context.RequestServices = originalServiceProvider;
        }
    }
}
