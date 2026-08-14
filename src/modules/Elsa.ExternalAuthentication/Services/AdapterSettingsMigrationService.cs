using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Orchestrates adapter-owned, forward-only settings migrations while preserving
/// the protocol-neutral connection envelope.
/// </summary>
public sealed class AdapterSettingsMigrationService(
    IExternalAuthenticationAdapterRegistry adapters,
    IEnumerable<IAdapterSettingsMigration> migrations) : IAdapterSettingsMigrationService
{
    private readonly IReadOnlyDictionary<(string AdapterType, int FromVersion), IAdapterSettingsMigration> _migrations =
        BuildMigrationIndex(migrations);

    public async ValueTask<AdapterSettingsMigrationResult> MigrateAsync(
        string adapterType,
        int settingsVersion,
        JsonElement settings,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!adapters.TryGet(adapterType, out var adapter))
            throw new InvalidOperationException($"The adapter type '{adapterType}' is not installed or deployment-allowed.");

        var currentVersion = adapter.Describe().SettingsVersion;
        if (settingsVersion <= 0 || settingsVersion > currentVersion)
            throw new InvalidOperationException($"Settings version {settingsVersion} is not compatible with adapter '{adapterType}' version {currentVersion}.");
        if (settingsVersion == currentVersion)
            return new AdapterSettingsMigrationResult(currentVersion, settings.Clone(), false);

        var migrated = settings.Clone();
        var version = settingsVersion;
        var stepCount = 0;
        while (version < currentVersion)
        {
            if (!_migrations.TryGetValue((adapterType, version), out var migration))
                throw new InvalidOperationException($"Adapter '{adapterType}' does not provide a settings migration from version {version}.");
            if (migration.ToVersion <= version || migration.ToVersion > currentVersion)
                throw new InvalidOperationException($"Adapter '{adapterType}' has an invalid settings migration from version {version} to {migration.ToVersion}.");
            if (++stepCount > 64)
                throw new InvalidOperationException($"Adapter '{adapterType}' settings migration contains a cycle.");

            migrated = (await migration.MigrateAsync(migrated, cancellationToken)).Clone();
            version = migration.ToVersion;
        }

        return new AdapterSettingsMigrationResult(version, migrated, true);
    }

    private static IReadOnlyDictionary<(string AdapterType, int FromVersion), IAdapterSettingsMigration> BuildMigrationIndex(
        IEnumerable<IAdapterSettingsMigration> migrations)
    {
        var result = new Dictionary<(string AdapterType, int FromVersion), IAdapterSettingsMigration>();
        foreach (var migration in migrations)
        {
            if (string.IsNullOrWhiteSpace(migration.AdapterType) || migration.FromVersion <= 0)
                throw new InvalidOperationException("Adapter settings migrations must define an adapter type and a positive source version.");
            if (!result.TryAdd((migration.AdapterType, migration.FromVersion), migration))
                throw new InvalidOperationException($"Adapter '{migration.AdapterType}' registers more than one migration from version {migration.FromVersion}.");
        }
        return result;
    }
}
