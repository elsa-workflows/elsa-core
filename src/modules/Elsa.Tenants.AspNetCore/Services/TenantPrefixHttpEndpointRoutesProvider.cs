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
public class TenantPrefixHttpEndpointRoutesProvider(ITenantsProvider tenantsProvider, IOptions<HttpActivityOptions> options) : IHttpEndpointRoutesProvider
{
    public async Task<IEnumerable<string>> GetRoutesAsync(HttpEndpointRouteProviderContext context)
    {
        var routes = new List<string>();
        var path = context.Stimulus.Path;

        if (string.IsNullOrWhiteSpace(path))
            return routes;

        var cancellationToken = context.CancellationToken;
        var tenantId = context.TenantId;
        var tenant = !string.IsNullOrEmpty(tenantId) ? await tenantsProvider.FindByIdAsync(tenantId, cancellationToken: cancellationToken) : null;
        var tenantPrefix = tenant != null ? tenant.Configuration.GetSection("Http")["Prefix"] ?? tenantId : null;

        routes.Add(new[]
        {
            tenantPrefix, options.Value.BasePath.ToString(), path
        }.JoinSegments());
        return routes;
    }
}