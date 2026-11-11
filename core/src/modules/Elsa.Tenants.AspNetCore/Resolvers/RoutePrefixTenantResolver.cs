using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Tenants.AspNetCore;

/// <summary>
/// Resolves the tenant based on the route prefix in the request URL. The tenant ID is expected to be part of the route.
/// </summary>
public class RoutePrefixTenantResolver(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider) : TenantResolverBase
{
    /// <inheritdoc />
    protected override async Task<TenantResolverResult> ResolveAsync(TenantResolverContext context)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext == null)
            return Unresolved();
        
        var path = GetPath(httpContext);
        var routeData = GetMatchingRouteValues(path);
        
        if (routeData == null)
            return Unresolved();
        
        var routeValues = routeData.RouteValues;
        var tenantPrefix = routeValues["tenantPrefix"] as string;
        
        if (string.IsNullOrWhiteSpace(tenantPrefix))
            return Unresolved();
        
        var tenants = (await serviceProvider.GetRequiredService<ITenantService>().ListAsync(context.CancellationToken))
            .Where(x => x.GetRoutePrefix() == tenantPrefix)
            .Take(2)
            .ToList();

        if (tenants.Count > 1)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<RoutePrefixTenantResolver>>();
            logger.LogWarning("Route prefix {TenantPrefix} is used by more than one tenant ({TenantIds}). No tenant is resolved.", tenantPrefix, string.Join(", ", tenants.Select(x => x.Id)));
            return Unresolved();
        }

        return AutoResolve(tenants.SingleOrDefault()?.Id);
    }
    
    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value!.NormalizeRoute();
    
    private HttpRouteData? GetMatchingRouteValues(string path)
    {
        var routeMatcher = serviceProvider.GetRequiredService<IRouteMatcher>();
        var routeTable = serviceProvider.GetRequiredService<IRouteTable>();

        var matchingRouteQuery =
            from routeData in routeTable
            let routeValues = routeMatcher.Match(routeData.Route, path)
            where routeValues != null
            select new HttpRouteData(routeData.Route, routeData.DataTokens, routeValues);

        return matchingRouteQuery.FirstOrDefault();
    }
}