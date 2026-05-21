using Elsa.Http.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IApplicationBuilder"/> related to workflow middleware components.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Installs the <see cref="HttpWorkflowsMiddleware"/> component.
    /// </summary>
    public static IApplicationBuilder UseWorkflows(this IApplicationBuilder app) => app.UseMiddleware<HttpWorkflowsMiddleware>();

    /// <summary>
    /// Applies an ASP.NET Core rate limiting policy to inbound HTTP workflow trigger routes.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="basePath">The HTTP workflow trigger base path to protect.</param>
    /// <param name="policyName">The registered ASP.NET Core rate limiting policy name. Leave empty to skip rate limiting.</param>
    public static IApplicationBuilder UseWorkflowsRateLimiting(this IApplicationBuilder app, PathString? basePath, string? policyName)
    {
        var normalizedBasePath = basePath?.ToString();
        return app.UseWorkflowsRateLimiting(normalizedBasePath, policyName);
    }

    /// <summary>
    /// Applies an ASP.NET Core rate limiting policy to inbound HTTP workflow trigger routes.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="basePath">The HTTP workflow trigger base path to protect.</param>
    /// <param name="policyName">The registered ASP.NET Core rate limiting policy name. Leave empty to skip rate limiting.</param>
    public static IApplicationBuilder UseWorkflowsRateLimiting(this IApplicationBuilder app, string? basePath, string? policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(basePath))
            return app;

        var pathPrefix = NormalizeBasePath(basePath);
        return pathPrefix.HasValue
            ? app.UseRateLimitingPolicyForPath(pathPrefix, policyName, "Elsa HTTP workflow trigger rate limiting endpoint")
            : app;
    }

    private static PathString NormalizeBasePath(string basePath)
    {
        var value = basePath.Trim();

        if (string.IsNullOrEmpty(value))
            return PathString.Empty;

        value = value.Trim('/');

        return string.IsNullOrEmpty(value) ? PathString.Empty : new PathString("/" + value);
    }
}
