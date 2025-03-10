namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Populates the <see cref="IActivityRegistry"/> with descriptors provided by <see cref="IActivityProvider"/>s. 
/// </summary>
public class ActivityRegistryPopulator(IEnumerable<IActivityProvider> providers, IActivityRegistry registry) : IActivityRegistryPopulator
{
    /// <inheritdoc />
    public async Task PopulateRegistryAsync(CancellationToken cancellationToken)
    {
        await registry.RefreshDescriptorsAsync(providers, cancellationToken);
    }
}