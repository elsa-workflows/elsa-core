using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.AspNetCore;

/// <summary>
/// Resolves the tenant based on the route prefix in the request URL. The tenant ID is expected to be part of the route.
/// </summary>
public class RoutePrefixTenantResolver(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider) : TenantResolverBase
{
    private Dictionary<string, Tenant>? _tenantRoutePrefixLookup;
    
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
        
        var tenant = await FindTenantByPrefix(tenantPrefix);
        var tenantId = tenant?.Id;
        return AutoResolve(tenantId);
    }
    
    private async Task<Tenant?> FindTenantByPrefix(string tenantPrefix)
    {
        _tenantRoutePrefixLookup ??= await GetTenantRoutePrefixLookup();
        return _tenantRoutePrefixLookup.TryGetValue(tenantPrefix, out var tenant) ? tenant : null;
    }

    private async Task<Dictionary<string,Tenant>> GetTenantRoutePrefixLookup()
    {
        var tenantsProvider = serviceProvider.GetRequiredService<ITenantsProvider>();
        var tenants = await tenantsProvider.ListAsync();
        return tenants.ToDictionary(x => x.GetRoutePrefix()!);
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