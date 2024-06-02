using Elsa.Tenants.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Tenants.Extensions;

/// <summary>
/// Provides extension methods for configuring the application builder.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds multitenancy support to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseMultitenancy(this IApplicationBuilder app)
    {
        app.UseMiddleware<TenantContainerMiddleware>();
        return app;
    }
}