using Elsa.Extensions;
using Elsa.Http.Contexts;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Services;

/// <summary>
/// Provides a route for an HTTP endpoint based on the specified context.
/// </summary>
[UsedImplicitly]
public class DefaultHttpEndpointRoutesProvider(IOptions<HttpActivityOptions> options) : IHttpEndpointRoutesProvider
{
    public Task<IEnumerable<string>> GetRoutesAsync(HttpEndpointRouteProviderContext context)
    {
        var routes = GetRoutes(context);
        return Task.FromResult(routes);
    }
    
    private IEnumerable<string> GetRoutes(HttpEndpointRouteProviderContext context)
    {
        var routes = new List<string>();
        var path = context.Stimulus.Path;

        if (string.IsNullOrWhiteSpace(path))
            return routes;

        routes.Add(new[]{options.Value.BasePath.ToString(), path}.JoinSegments());
        return routes;
    }
}