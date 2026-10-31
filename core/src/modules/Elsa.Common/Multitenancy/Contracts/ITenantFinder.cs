namespace Elsa.Common.Multitenancy;

public interface ITenantFinder
{
    Task<Tenant?> FindByIdAsync(string tenantId, CancellationToken cancellationToken = default);
}