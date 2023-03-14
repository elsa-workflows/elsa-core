using Elsa.Expressions.Contracts;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class ExpressionSyntaxRegistryPopulator : IExpressionSyntaxRegistryPopulator
{
    private readonly IEnumerable<IExpressionSyntaxProvider> _providers;
    private readonly IExpressionSyntaxRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionSyntaxRegistryPopulator"/> class.
    /// </summary>
    public ExpressionSyntaxRegistryPopulator(IEnumerable<IExpressionSyntaxProvider> providers, IExpressionSyntaxRegistry registry)
    {
        _providers = providers;
        _registry = registry;
    }

    /// <inheritdoc />
    public async ValueTask PopulateRegistryAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
            _registry.AddMany(descriptors);
        }
    }
}