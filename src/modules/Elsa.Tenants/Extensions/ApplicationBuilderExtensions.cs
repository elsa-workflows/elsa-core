using Elsa.Tenants.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Elsa.Tenants.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IApplicationBuilder"/> related to workflow middleware components.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Installs the <see cref="HttpCurrentUserTenantResolverMiddleware"/> component.
    /// </summary>
    public static IApplicationBuilder UseHttpTenantMiddleware(this IApplicationBuilder app) => app.UseMiddleware<HttpCurrentUserTenantResolverMiddleware>();

    /// <summary>
    /// Installs the <see cref="HttpClaimsTenantResolverMiddleware"/> component.
    /// </summary>
    public static IApplicationBuilder UseHttpExternalTenantMiddleware(this IApplicationBuilder app) => app.UseMiddleware<HttpClaimsTenantResolverMiddleware>();
}
