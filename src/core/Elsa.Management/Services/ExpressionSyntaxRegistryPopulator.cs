using Elsa.Management.Contracts;

namespace Elsa.Management.Services;

public class ExpressionSyntaxRegistryPopulator : IExpressionSyntaxRegistryPopulator
{
    private readonly IEnumerable<IExpressionSyntaxProvider> _providers;
    private readonly IExpressionSyntaxRegistry _registry;

    public ExpressionSyntaxRegistryPopulator(IEnumerable<IExpressionSyntaxProvider> providers, IExpressionSyntaxRegistry registry)
    {
        _providers = providers;
        _registry = registry;
    }

    public async ValueTask PopulateRegistryAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
            _registry.AddMany(descriptors);
        }
    }
}