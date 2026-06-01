using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Diagnostics;
using Elsa.ModularPersistence.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Services;

public sealed class ModularPersistenceDiagnosticsService(
    IStorageManifestRegistry registry,
    IEnumerable<IStorageManifestMaterializer> materializers,
    IStorageManifestMaterializationTracker tracker,
    IOptions<ModularPersistenceOptions> options) : IModularPersistenceDiagnosticsService
{
    public ModularPersistenceDiagnostics GetDiagnostics()
    {
        var manifestList = registry.Manifests.ToArray();
        var materializerList = materializers.ToArray();
        var records = tracker.Records;
        var selectedProviderName = options.Value.ProviderName;

        var providers = materializerList
            .GroupBy(x => x.ProviderName, StringComparer.OrdinalIgnoreCase)
            .Select(x => new StorageProviderDiagnostic(
                x.First().ProviderName,
                IsSelectedProvider(x.First().ProviderName, selectedProviderName),
                true,
                manifestList.Count(manifest => x.Any(materializer => materializer.CanMaterialize(manifest)))))
            .OrderBy(x => x.ProviderName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var manifests = manifestList
            .Select(manifest => new StorageManifestDiagnostic(
                manifest.SchemaName,
                manifest.Version.ToString(),
                manifest.StorageUnits.Count,
                records.Where(record => string.Equals(record.SchemaName, manifest.SchemaName, StringComparison.OrdinalIgnoreCase) && string.Equals(record.Version, manifest.Version.ToString(), StringComparison.Ordinal)).ToArray()))
            .OrderBy(x => x.SchemaName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var failures = records
            .Where(x => !x.Succeeded)
            .Select(x => new StorageManifestMaterializationFailure(x.ProviderName, x.SchemaName, x.Version, x.RecordedAt, x.ErrorType ?? "Unknown", x.ErrorMessage ?? "Unknown materialization failure."))
            .OrderByDescending(x => x.FailedAt)
            .ToArray();

        return new ModularPersistenceDiagnostics(selectedProviderName, options.Value.MaterializeOnStartup, providers, manifests, failures);
    }

    private static bool IsSelectedProvider(string providerName, string? selectedProviderName) =>
        string.IsNullOrWhiteSpace(selectedProviderName) || string.Equals(providerName, selectedProviderName, StringComparison.OrdinalIgnoreCase);
}
