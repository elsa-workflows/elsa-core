using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Populates the <see cref="IActivityRegistry"/> with descriptors provided by <see cref="IActivityProvider"/>s. 
/// </summary>
public class ActivityRegistryPopulator : IActivityRegistryPopulator
{
    private readonly IEnumerable<IActivityProvider> _providers;
    private readonly IActivityRegistry _registry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ActivityRegistryPopulator(IEnumerable<IActivityProvider> providers, IActivityRegistry registry)
    {
        _providers = providers;
        _registry = registry;
    }

    /// <inheritdoc />
    public async Task PopulateRegistryAsync(CancellationToken cancellationToken)
    {
        _registry.Clear();

        foreach (var provider in _providers)
            await PopulateRegistryAsync(provider, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PopulateRegistryAsync(Type providerType, CancellationToken cancellationToken = default)
    {
        _registry.ClearProvider(providerType);
        var provider = _providers.First(x => x.GetType() == providerType);
        await PopulateRegistryAsync(provider, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddToRegistry(Type providerType, string workflowDefinitionVersionId, CancellationToken cancellationToken = default)
    {
        var provider = _providers.First(x => x.GetType() == providerType);
        var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
        var descriptorToAdd = descriptors
            .SingleOrDefault(d => d.CustomProperties.TryGetValue("WorkflowDefinitionVersionId", out var val) && val.ToString() == workflowDefinitionVersionId);
        
        if (descriptorToAdd is not null)
            _registry.Add(providerType, descriptorToAdd!);
    }

    private async Task PopulateRegistryAsync(IActivityProvider provider, CancellationToken cancellationToken = default)
    {
        var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
        _registry.AddMany(provider.GetType(), descriptors);
    }
}