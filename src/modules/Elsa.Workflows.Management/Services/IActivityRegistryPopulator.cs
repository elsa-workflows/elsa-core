namespace Elsa.Workflows.Management.Services;

public interface IActivityRegistryPopulator
{
    ValueTask PopulateRegistryAsync(CancellationToken cancellationToken);
}