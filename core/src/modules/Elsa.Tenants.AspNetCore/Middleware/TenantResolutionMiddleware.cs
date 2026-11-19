using Elsa.Common.Multitenancy;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

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
        var boundTenantIds = context.User.Identities.Where(identity => identity.IsAuthenticated)
            .SelectMany(identity => identity.FindAll(TenantPrincipalClaims.BoundTenant))
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var hasBinding = boundTenantIds.Length != 0;
        var tenant = hasBinding
            ? await ResolveBoundTenantAsync(context, boundTenantIds)
            : await tenantResolverPipelineInvoker.InvokePipelineAsync(context.RequestAborted);

        if (tenant == null && hasBinding)
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

    private static async Task<Tenant?> ResolveBoundTenantAsync(HttpContext context, string[] boundTenantIds)
    {
        if (boundTenantIds.Length != 1 || boundTenantIds[0] == Tenant.AgnosticTenantId)
            return null;

        var boundTenantId = boundTenantIds[0];
        var services = context.RequestServices;
        if (services.GetService<ITenantRequestHints>() is { } hintProvider)
        {
            var hints = await hintProvider.GetAsync(context.RequestAborted);
            if (hints.Any(hint => !string.Equals(hint, boundTenantId, StringComparison.Ordinal)))
                return null;
        }

        return await services.GetRequiredService<ITenantService>().FindAsync(boundTenantId, context.RequestAborted);
    }
}
