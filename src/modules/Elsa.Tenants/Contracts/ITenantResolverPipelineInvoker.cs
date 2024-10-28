using Elsa.Common.Multitenancy;

namespace Elsa.Tenants;

public interface ITenantResolverPipelineInvoker
{
    Task<Tenant?> InvokePipelineAsync(CancellationToken cancellationToken = default);
}