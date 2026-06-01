using Elsa.Common;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Services;

public sealed class ModularPersistenceMaterializationStartupTask(
    IStorageManifestRegistry registry,
    IEnumerable<IStorageManifestMaterializer> materializers,
    IOptions<ModularPersistenceOptions> options) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MaterializeOnStartup)
            return;

        var materializerList = materializers.ToArray();
        foreach (var manifest in registry.Manifests)
        {
            var matchingMaterializers = materializerList.Where(x => x.CanMaterialize(manifest)).ToArray();
            if (matchingMaterializers.Length == 0)
                throw new InvalidOperationException($"No modular persistence materializer is registered for manifest '{manifest.SchemaName}' version '{manifest.Version}'.");

            foreach (var materializer in matchingMaterializers)
                await materializer.MaterializeAsync(manifest, cancellationToken);
        }
    }
}
