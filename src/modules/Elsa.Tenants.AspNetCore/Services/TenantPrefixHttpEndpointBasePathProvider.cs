using Elsa.Common.Multitenancy;
using Elsa.Http;
using Elsa.Http.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.AspNetCore.Services;

[UsedImplicitly]
public class TenantPrefixHttpEndpointBasePathProvider(
    IOptions<HttpActivityOptions> options, 
    IEnumerable<ITenantResolver> tenantResolvers, 
    ITenantAccessor tenantAccessor) : IHttpEndpointBasePathProvider
{
    public string GetBasePath()
    {
        var baseUrl = options.Value.BaseUrl.ToString();
        var basePath = options.Value.BasePath?.ToString().TrimStart('/');
        var routePrefixTenantResolverIsEnabled = tenantResolvers.Any(x => x is RoutePrefixTenantResolver);
        
        if(!routePrefixTenantResolverIsEnabled)
            return (baseUrl + basePath).TrimEnd('/') + '/';
        
        var tenant = tenantAccessor.Tenant;
        var tenantPrefix = tenant?.GetRoutePrefix();
        
        if(string.IsNullOrWhiteSpace(tenantPrefix))
            return (baseUrl + basePath).TrimEnd('/') + '/';
        
        var completeBaseUrl = new Uri(baseUrl + tenantPrefix + "/") + basePath;
        return completeBaseUrl.TrimEnd('/') + '/';
    }
}