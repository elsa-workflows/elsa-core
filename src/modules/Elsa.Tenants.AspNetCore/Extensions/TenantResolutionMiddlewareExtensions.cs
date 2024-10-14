using Elsa.Tenants.AspNetCore.Middleware;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

[UsedImplicitly]
public static class TenantResolutionMiddlewareExtensions
{
    /// <summary>
    /// Adds the tenant resolution middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    public static IApplicationBuilder UseTenants(this IApplicationBuilder builder) => builder.UseMiddleware<TenantResolutionMiddleware>();
}