using Elsa.Framework.Tenants;
using Elsa.Framework.Tenants.Contracts;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants.Services;

/// <inheritdoc />
public class PipelinedTenantResolver(IOptions<MultitenancyOptions> options, ITenantsProvider tenantsProvider, IServiceProvider serviceProvider) : ITenantResolver
{
    private Tenant? _currentTenant;

    /// <inheritdoc/>
    public async Task<Tenant> GetTenantAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTenant != null)
            return _currentTenant;

        var resolutionPipeline = options.Value.TenantResolutionPipelineBuilder.Build(serviceProvider);
        var tenantsDictionary = (await tenantsProvider.ListAsync(cancellationToken)).ToDictionary(x => x.Id);
        var resolutionContext = new TenantResolutionContext(tenantsDictionary, cancellationToken);

        foreach (var strategy in resolutionPipeline)
        {
            var result = await strategy.ResolveAsync(resolutionContext);

            if (result.IsResolved)
            {
                _currentTenant = tenantsDictionary[result.TenantId!];
                return _currentTenant;
            }
        }

        return _currentTenant ??= Tenant.DefaultTenant;
    }
}