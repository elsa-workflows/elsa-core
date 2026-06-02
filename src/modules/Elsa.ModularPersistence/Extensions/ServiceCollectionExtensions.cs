using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.ModularPersistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModularPersistenceServices(this IServiceCollection services, Action<ModularPersistenceOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.TryAddSingleton<IStorageManifestRegistry, StorageManifestRegistry>();
        services.TryAddSingleton<IStorageManifestMaterializationTracker, StorageManifestMaterializationTracker>();
        services.TryAddSingleton<IModularPersistenceDiagnosticsService, ModularPersistenceDiagnosticsService>();
        services.AddStartupTask<ModularPersistenceMaterializationStartupTask>();

        return services;
    }
}
