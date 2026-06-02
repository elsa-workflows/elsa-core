using Elsa.Common;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Services;

public sealed class ModularPersistenceMaterializationStartupTask(
    IStorageManifestRegistry registry,
    IEnumerable<IStorageManifestMaterializer> materializers,
    IOptions<ModularPersistenceOptions> options,
    IStorageManifestMaterializationTracker materializationTracker) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.MaterializeOnStartup)
            return;

        var materializerList = materializers.ToArray();
        foreach (var manifest in registry.Manifests)
        {
            var matchingMaterializers = materializerList
                .Where(x => IsSelectedProvider(x, options.Value.ProviderName) && x.CanMaterialize(manifest))
                .ToArray();
            if (matchingMaterializers.Length == 0)
            {
                var providerText = string.IsNullOrWhiteSpace(options.Value.ProviderName) ? "any provider" : $"provider '{options.Value.ProviderName}'";
                throw new InvalidOperationException($"No modular persistence materializer is registered for manifest '{manifest.SchemaName}' version '{manifest.Version}' using {providerText}.");
            }

            foreach (var materializer in matchingMaterializers)
                await MaterializeWithRetryAsync(materializer, manifest, cancellationToken);
        }
    }

    private async Task MaterializeWithRetryAsync(IStorageManifestMaterializer materializer, StorageManifestDescriptor manifest, CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Max(0, options.Value.MaterializationRetryCount) + 1;
        var retryDelay = options.Value.MaterializationRetryDelay < TimeSpan.Zero ? TimeSpan.Zero : options.Value.MaterializationRetryDelay;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await materializer.MaterializeAsync(manifest, cancellationToken);
                materializationTracker.RecordApplied(materializer.ProviderName, manifest.SchemaName, manifest.Version.ToString(), DateTimeOffset.UtcNow);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                materializationTracker.RecordFailed(materializer.ProviderName, manifest.SchemaName, manifest.Version.ToString(), DateTimeOffset.UtcNow, e);
                if (attempt == maxAttempts)
                    throw new StorageManifestMaterializationException(materializer.ProviderName, manifest, attempt, e);

                if (retryDelay > TimeSpan.Zero)
                    await Task.Delay(retryDelay, cancellationToken);
            }
        }
    }

    private static bool IsSelectedProvider(IStorageManifestMaterializer materializer, string? providerName) =>
        string.IsNullOrWhiteSpace(providerName) || string.Equals(materializer.ProviderName, providerName, StringComparison.OrdinalIgnoreCase);
}
