using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Features;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Runtime;
using Elsa.ModularPersistence.PostgreSql;
using Elsa.ModularPersistence.PostgreSql.Features;
using Elsa.ModularPersistence.PostgreSql.Options;
using Elsa.ModularPersistence.PostgreSql.Services;
using Elsa.ModularPersistence.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.PostgreSql.Extensions;

public static class PostgreSqlModularPersistenceExtensions
{
    public static ModularPersistenceFeature UsePostgreSqlProvider(this ModularPersistenceFeature feature, string connectionString, Action<PostgreSqlModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(PostgreSqlDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<PostgreSqlModularPersistenceFeature>(postgresql =>
        {
            postgresql.ConfigureOptions = options =>
            {
                options.ConnectionString = connectionString;
                configure?.Invoke(options);
            };
        });
        return feature;
    }

    public static ModularPersistenceFeature UsePostgreSqlProvider(this ModularPersistenceFeature feature, Action<PostgreSqlModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(PostgreSqlDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<PostgreSqlModularPersistenceFeature>(postgresql => postgresql.ConfigureOptions = configure);
        return feature;
    }

    public static IServiceCollection AddPostgreSqlModularPersistence(this IServiceCollection services, Action<PostgreSqlModularPersistenceOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<PostgreSqlModularPersistenceOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<PostgreSqlModularPersistenceOptions>>().Value);
        services.TryAddSingleton(sp => new PostgreSqlModularPersistenceConnectionFactory(sp.GetRequiredService<PostgreSqlModularPersistenceOptions>()));
        services.TryAddSingleton<IRuntimeEntityDocumentStoreFactory, PostgreSqlRuntimeEntityDocumentStoreFactory>();
        services.AddSingleton<IStorageManifestMaterializer>(sp =>
        {
            var options = sp.GetRequiredService<PostgreSqlModularPersistenceOptions>();
            return new PostgreSqlDocumentSchemaMaterializer(sp.GetRequiredService<PostgreSqlModularPersistenceConnectionFactory>(), options);
        });
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<PostgreSqlModularPersistenceOptions>();
            return new StorageProviderCapabilitiesRegistration(PostgreSqlDocumentSchemaMaterializer.ProviderNameValue, PostgreSqlDocumentProviderCapabilities.Create(options.UseOptimizedJsonbIndexes));
        });
        services.AddSingleton<IStoragePhysicalizationPlanner>(sp =>
        {
            var options = sp.GetRequiredService<PostgreSqlModularPersistenceOptions>();
            return new StoragePhysicalizationPlannerRegistration(PostgreSqlDocumentSchemaMaterializer.ProviderNameValue, PostgreSqlDocumentProviderCapabilities.Create(options.UseOptimizedJsonbIndexes));
        });
        services.PostConfigure<ModularPersistenceOptions>(options => options.ProviderName ??= PostgreSqlDocumentSchemaMaterializer.ProviderNameValue);

        return services;
    }
}
