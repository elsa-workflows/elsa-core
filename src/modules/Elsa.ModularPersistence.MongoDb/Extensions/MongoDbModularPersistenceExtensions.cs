using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Features;
using Elsa.ModularPersistence.MongoDb.Features;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.MongoDb.Services;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Runtime;
using Elsa.ModularPersistence.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.MongoDb.Extensions;

public static class MongoDbModularPersistenceExtensions
{
    public static ModularPersistenceFeature UseMongoDbProvider(this ModularPersistenceFeature feature, string connectionString, string databaseName, Action<MongoDbModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(MongoDbDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<MongoDbModularPersistenceFeature>(mongoDb =>
        {
            mongoDb.ConfigureOptions = options =>
            {
                options.ConnectionString = connectionString;
                options.DatabaseName = databaseName;
                configure?.Invoke(options);
            };
        });
        return feature;
    }

    public static ModularPersistenceFeature UseMongoDbProvider(this ModularPersistenceFeature feature, Action<MongoDbModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(MongoDbDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<MongoDbModularPersistenceFeature>(mongoDb => mongoDb.ConfigureOptions = configure);
        return feature;
    }

    public static IServiceCollection AddMongoDbModularPersistence(this IServiceCollection services, Action<MongoDbModularPersistenceOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<MongoDbModularPersistenceOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<MongoDbModularPersistenceOptions>>().Value);
        services.TryAddSingleton<MongoDbCollectionResolver>();
        services.TryAddSingleton<IRuntimeEntityDocumentStoreFactory, MongoDbRuntimeEntityDocumentStoreFactory>();
        services.AddSingleton<IStorageManifestMaterializer>(sp => new MongoDbDocumentSchemaMaterializer(sp.GetRequiredService<MongoDbModularPersistenceOptions>()));
        services.AddSingleton(new StorageProviderCapabilitiesRegistration(MongoDbDocumentSchemaMaterializer.ProviderNameValue, MongoDbDocumentProviderCapabilities.Value));
        services.PostConfigure<ModularPersistenceOptions>(options => options.ProviderName ??= MongoDbDocumentSchemaMaterializer.ProviderNameValue);

        return services;
    }
}
