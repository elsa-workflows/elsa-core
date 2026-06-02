using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Features;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Planning;
using Elsa.ModularPersistence.Runtime;
using Elsa.ModularPersistence.SqlServer;
using Elsa.ModularPersistence.SqlServer.Features;
using Elsa.ModularPersistence.SqlServer.Options;
using Elsa.ModularPersistence.SqlServer.Services;
using Elsa.ModularPersistence.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.SqlServer.Extensions;

public static class SqlServerModularPersistenceExtensions
{
    public static ModularPersistenceFeature UseSqlServerProvider(this ModularPersistenceFeature feature, string connectionString, Action<SqlServerModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(SqlServerDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<SqlServerModularPersistenceFeature>(sqlServer =>
        {
            sqlServer.ConfigureOptions = options =>
            {
                options.ConnectionString = connectionString;
                configure?.Invoke(options);
            };
        });
        return feature;
    }

    public static ModularPersistenceFeature UseSqlServerProvider(this ModularPersistenceFeature feature, Action<SqlServerModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(SqlServerDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<SqlServerModularPersistenceFeature>(sqlServer => sqlServer.ConfigureOptions = configure);
        return feature;
    }

    public static IServiceCollection AddSqlServerModularPersistence(this IServiceCollection services, Action<SqlServerModularPersistenceOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<SqlServerModularPersistenceOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<SqlServerModularPersistenceOptions>>().Value);
        services.TryAddSingleton(sp => new SqlServerModularPersistenceConnectionFactory(sp.GetRequiredService<SqlServerModularPersistenceOptions>()));
        services.TryAddSingleton<IRuntimeEntityDocumentStoreFactory, SqlServerRuntimeEntityDocumentStoreFactory>();
        services.AddSingleton<IStorageManifestMaterializer>(sp =>
        {
            var options = sp.GetRequiredService<SqlServerModularPersistenceOptions>();
            return new SqlServerDocumentSchemaMaterializer(sp.GetRequiredService<SqlServerModularPersistenceConnectionFactory>(), options);
        });
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<SqlServerModularPersistenceOptions>();
            return new StorageProviderCapabilitiesRegistration(SqlServerDocumentSchemaMaterializer.ProviderNameValue, SqlServerDocumentProviderCapabilities.Create(options.UseOptimizedIndexes));
        });
        services.AddSingleton<IStoragePhysicalizationPlanner>(sp =>
        {
            var options = sp.GetRequiredService<SqlServerModularPersistenceOptions>();
            return new StoragePhysicalizationPlannerRegistration(SqlServerDocumentSchemaMaterializer.ProviderNameValue, SqlServerDocumentProviderCapabilities.Create(options.UseOptimizedIndexes));
        });
        services.PostConfigure<ModularPersistenceOptions>(options => options.ProviderName ??= SqlServerDocumentSchemaMaterializer.ProviderNameValue);

        return services;
    }
}
