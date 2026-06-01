using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Runtime;

public sealed class RuntimeEntityDocumentStoreFactoryRegistry(
    IEnumerable<IRuntimeEntityDocumentStoreFactory> factories,
    IOptions<ModularPersistenceOptions> options) : IRuntimeEntityDocumentStoreFactoryRegistry
{
    public IDocumentStore CreateStore(StorageManifestDescriptor manifest, string? providerName)
    {
        var selectedProviderName = string.IsNullOrWhiteSpace(providerName) ? options.Value.ProviderName : providerName;
        var factory = factories.FirstOrDefault(x => string.IsNullOrWhiteSpace(selectedProviderName) || string.Equals(x.ProviderName, selectedProviderName, StringComparison.OrdinalIgnoreCase));
        if (factory == null)
        {
            var providerText = string.IsNullOrWhiteSpace(selectedProviderName) ? "the selected provider" : $"provider '{selectedProviderName}'";
            throw new InvalidOperationException($"No runtime entity document store factory is registered for {providerText}.");
        }

        return factory.CreateStore(manifest);
    }
}
