namespace Elsa.Management.Contracts;

public interface ITriggerRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}