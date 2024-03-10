using Elsa.Tenants.Contexts;
using Elsa.Tenants.Contracts;
using Elsa.Tenants.Entities;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Services;

/// <inheritdoc />
public class TenantResolver(IOptions<TenantsOptions> options, ITenantsProvider tenantsProvider, IServiceProvider serviceProvider) : ITenantResolver
{
    /// <inheritdoc/>
    public async Task<Tenant?> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        var resolutionPipeline = options.Value.TenantResolutionPipelineBuilder.Build(serviceProvider);
        var tenantsDictionary = (await tenantsProvider.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var resolutionContext = new TenantResolutionContext(tenantsDictionary, cancellationToken);

        foreach (var strategy in resolutionPipeline)
        {
            var result = await strategy.ResolveAsync(resolutionContext);

            if (result.IsResolved)
                return tenantsDictionary!.GetValueOrDefault(result.TenantId);
        }

        return null;
    }
}