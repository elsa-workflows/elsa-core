using Elsa.Extensions;
using Elsa.ModularPersistence.Documents;
using Elsa.Secrets.Features;
using Elsa.Secrets.Storage;

namespace Elsa.Secrets.Extensions;

public static class SecretsFeatureModularPersistenceExtensions
{
    public static SecretsFeature UseModularPersistence(this SecretsFeature feature, Func<IServiceProvider, IDocumentStore> documentStoreFactory)
    {
        feature.Module.UseModularPersistence(modularPersistence => modularPersistence.RegisterManifest(SecretsStorageManifest.Create()));
        feature.Module.Services.AddModularPersistenceSecretRepository(documentStoreFactory);
        return feature;
    }
}
