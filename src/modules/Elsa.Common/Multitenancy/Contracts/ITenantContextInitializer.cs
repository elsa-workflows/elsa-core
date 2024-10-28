namespace Elsa.Common.Multitenancy;

public interface ITenantContextInitializer
{
    Task InitializeAsync(string tenantId, CancellationToken cancellationToken = default);
}