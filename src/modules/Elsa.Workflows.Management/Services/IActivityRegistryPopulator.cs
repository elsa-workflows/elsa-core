namespace Elsa.Workflows.Management.Services;

public interface IActivityRegistryPopulator
{
    Task PopulateRegistryAsync(CancellationToken cancellationToken = default);
    Task PopulateRegistryAsync(Type providerType, CancellationToken cancellationToken = default);
}