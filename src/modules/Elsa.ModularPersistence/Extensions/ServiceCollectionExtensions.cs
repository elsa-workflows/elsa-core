using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Runtime;
using Elsa.ModularPersistence.Services;
using Elsa.ModularPersistence.Validation;
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
        services.TryAddSingleton<IRuntimeStorageDefinitionStore, InMemoryRuntimeStorageDefinitionStore>();
        services.TryAddSingleton<IRuntimeSchemaAuditTrail, InMemoryRuntimeSchemaAuditTrail>();
        services.TryAddSingleton<IRuntimeEntityDocumentStoreFactoryRegistry, RuntimeEntityDocumentStoreFactoryRegistry>();
        services.TryAddSingleton<IRuntimeEntityDataService, RuntimeEntityDataService>();
        services.TryAddSingleton<IRuntimePhysicalizationOperations, RuntimePhysicalizationOperations>();
        services.TryAddSingleton<IStorageProviderCapabilitiesRegistry, StorageProviderCapabilitiesRegistry>();
        services.TryAddSingleton<IRuntimeStorageDefinitionManager, RuntimeStorageDefinitionManager>();
        services.AddStartupTask<ModularPersistenceMaterializationStartupTask>();

        return services;
    }

    public static IServiceCollection AddStorageProviderCapabilities(this IServiceCollection services, string providerName, ProviderCapabilities capabilities)
    {
        services.AddSingleton(new StorageProviderCapabilitiesRegistration(providerName, capabilities));
        services.AddSingleton<IStoragePhysicalizationPlanner>(new StoragePhysicalizationPlannerRegistration(providerName, capabilities));
        return services;
    }
}
