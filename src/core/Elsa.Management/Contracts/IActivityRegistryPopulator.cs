namespace Elsa.Management.Contracts;

public interface IActivityRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}