using Elsa.Extensions;
using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.AspNetCore.Services;

/// <summary>
/// Provides a route for an HTTP endpoint based on the specified context and tenant prefix.
/// </summary>
[UsedImplicitly]
public class TenantPrefixHttpEndpointRoutesProvider(IOptions<HttpActivityOptions> options) : IHttpEndpointRoutesProvider
{
    public Task<IEnumerable<string>> GetRoutesAsync(HttpEndpointRouteProviderContext context)
    {
        var routes = GetRoutes(context);
        return Task.FromResult(routes);
    }

    private IEnumerable<string> GetRoutes(HttpEndpointRouteProviderContext context)
    {
        var routes = new List<string>();
        var path = context.Payload.Path;

        if (string.IsNullOrWhiteSpace(path))
            return routes;

        var segments = new List<string?>();

        if (!string.IsNullOrEmpty(context.TenantId))
            segments.Add("{tenantId}");

        segments.AddRange(options.Value.BasePath.ToString(), path);
        routes.Add(segments.JoinSegments());
        return routes;
    }
}