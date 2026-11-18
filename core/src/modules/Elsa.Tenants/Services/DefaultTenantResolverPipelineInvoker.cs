using Elsa.Common.Multitenancy;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Tenants;

/// <summary>
/// Resolves the tenant using a pipeline of resolvers.
/// </summary>
public class DefaultTenantResolverPipelineInvoker(
    IOptions<MultitenancyOptions> options,
    ITenantsProvider tenantsProvider,
    IServiceProvider serviceProvider,
    ILogger<DefaultTenantResolverPipelineInvoker> logger) : ITenantResolverPipelineInvoker, ITenantRequestHints
{
    public async Task<IReadOnlyCollection<string>> GetAsync(CancellationToken cancellationToken = default)
    {
        var resolvers = options.Value.TenantResolverPipelineBuilder.Build(serviceProvider).OfType<ITenantRequestHintResolver>().ToArray();
        if (resolvers.Length == 0)
            return [];

        var tenants = await tenantsProvider.ListAsync(cancellationToken);
        var context = new TenantResolverContext(tenants.ToDictionary(tenant => tenant.Id.NormalizeTenantId()), cancellationToken);
        var hints = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resolver in resolvers)
            hints.UnionWith(await resolver.GetHintsAsync(context));

        return hints;
    }

    public async Task<Tenant?> InvokePipelineAsync(CancellationToken cancellationToken = default)
    {
        var resolutionPipeline = options.Value.TenantResolverPipelineBuilder.Build(serviceProvider);
        var tenants = await tenantsProvider.ListAsync(cancellationToken);
        var tenantsDictionary = tenants.ToDictionary(x => x.Id.NormalizeTenantId());
        var context = new TenantResolverContext(tenantsDictionary, cancellationToken);

        foreach (var resolver in resolutionPipeline)
        {
            var result = await resolver.ResolveAsync(context);

            if (result.IsResolved)
            {
                var resolvedTenantId = result.ResolveTenantId();

                if (tenantsDictionary.TryGetValue(resolvedTenantId, out var tenant))
                    return tenant;

                logger.LogWarning("Tenant with ID {TenantId} was resolved but could not be found in the tenant store.", resolvedTenantId);
                return null;
            }
        }

        return null;
    }
}
