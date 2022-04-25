namespace Elsa.Management.Services;

public interface IActivityRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}