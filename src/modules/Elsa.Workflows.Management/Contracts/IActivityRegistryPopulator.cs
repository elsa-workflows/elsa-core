namespace Elsa.Workflows.Management.Contracts;

public interface IActivityRegistryPopulator
{
    Task PopulateRegistryAsync(CancellationToken cancellationToken = default);
    Task PopulateRegistryAsync(Type providerType, CancellationToken cancellationToken = default);
}