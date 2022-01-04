using Elsa.Management.Contracts;

namespace Elsa.Management.Services;

/// <summary>
/// Populates the <see cref="ITriggerRegistry"/> with descriptors provided by <see cref="ITriggerProvider"/>s. 
/// </summary>
public class TriggerRegistryPopulator : ITriggerRegistryPopulator
{
    private readonly IEnumerable<ITriggerProvider> _providers;
    private readonly ITriggerRegistry _registry;

    public TriggerRegistryPopulator(IEnumerable<ITriggerProvider> providers, ITriggerRegistry registry)
    {
        _providers = providers;
        _registry = registry;
    }

    public async ValueTask PopulateRegistryAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
            _registry.AddMany(provider, descriptors);
        }
    }
}