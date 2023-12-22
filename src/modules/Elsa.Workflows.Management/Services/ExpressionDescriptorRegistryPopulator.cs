// using Elsa.Expressions.Contracts;
//
// namespace Elsa.Workflows.Management.Services;
//
// /// <inheritdoc />
// public class ExpressionDescriptorRegistryPopulator : IExpressionDescriptorRegistryPopulator
// {
//     private readonly IEnumerable<IExpressionDescriptorProvider> _providers;
//     private readonly IExpressionDescriptorRegistry _registry;
//
//     /// <summary>
//     /// Initializes a new instance of the <see cref="ExpressionDescriptorRegistryPopulator"/> class.
//     /// </summary>
//     public ExpressionDescriptorRegistryPopulator(IEnumerable<IExpressionDescriptorProvider> providers, IExpressionDescriptorRegistry registry)
//     {
//         _providers = providers;
//         _registry = registry;
//     }
//
//     /// <inheritdoc />
//     public async ValueTask PopulateRegistryAsync(CancellationToken cancellationToken)
//     {
//         foreach (var provider in _providers)
//         {
//             var descriptors = await provider.GetDescriptorsAsync(cancellationToken);
//             _registry.AddRange(descriptors);
//         }
//     }
// }