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
    ITenantService tenantService,
    IServiceProvider serviceProvider,
    ILogger<DefaultTenantResolverPipelineInvoker> logger) : ITenantResolverPipelineInvoker, ITenantRequestHints
{
    public async Task<IReadOnlyCollection<string>> GetAsync(CancellationToken cancellationToken = default)
    {
        var context = new TenantResolverContext(tenantService, cancellationToken);
        var hints = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resolver in options.Value.TenantResolverPipelineBuilder.Build(serviceProvider).OfType<ITenantRequestHintResolver>())
            hints.UnionWith(await resolver.GetHintsAsync(context));

        return hints;
    }

    public async Task<Tenant?> InvokePipelineAsync(CancellationToken cancellationToken = default)
    {
        var resolutionPipeline = options.Value.TenantResolverPipelineBuilder.Build(serviceProvider);
        var context = new TenantResolverContext(tenantService, cancellationToken);

        foreach (var resolver in resolutionPipeline)
        {
            // The default resolver is the silent fallback to the default tenant that fail-closed hosts opt out of.
            if (options.Value.RejectUnresolvedTenants && resolver is DefaultTenantResolver)
            {
                continue;
            }

            var result = await resolver.ResolveAsync(context);

            if (result.IsResolved)
            {
                var resolvedTenantId = result.ResolveTenantId();
                var tenant = await tenantService.FindAsync(resolvedTenantId, cancellationToken);

                if (tenant != null)
                {
                    return tenant;
                }

                logger.LogWarning("Tenant with ID {TenantId} was resolved but could not be found in the tenant store.", resolvedTenantId);
                return null;
            }
        }

        return null;
    }
}
