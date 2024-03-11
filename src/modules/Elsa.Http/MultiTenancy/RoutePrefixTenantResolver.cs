using Elsa.Common.Abstractions;
using Elsa.Common.Contexts;
using Elsa.Common.Results;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.MultiTenancy;

/// <summary>
/// Resolves the tenant based on the route prefix in the request URL. The tenant ID is expected to be part of the route.
/// </summary>
public class RoutePrefixTenantResolver(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider) : TenantResolutionStrategyBase
{
    /// <inheritdoc />
    protected override async ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext == null)
            return Unresolved();
        
        var path = GetPath(httpContext);
        var routeValues = GetMatchingRouteValues(path);
        
        if (routeValues == null)
            return Unresolved();
        
        var tenantId = routeValues["tenantId"] as string;
        
        return AutoResolve(tenantId);
    }
    
    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value!.NormalizeRoute();
    private RouteValueDictionary? GetMatchingRouteValues(string path)
    {
        var routeMatcher = serviceProvider.GetRequiredService<IRouteMatcher>();
        var routeTable = serviceProvider.GetRequiredService<IRouteTable>();

        var matchingRouteQuery =
            from route in routeTable
            let routeValues = routeMatcher.Match(route, path)
            where routeValues != null
            select routeValues;

        return matchingRouteQuery.FirstOrDefault();
    }
}