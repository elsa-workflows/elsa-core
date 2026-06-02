using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Features;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.Sqlite.Features;
using Elsa.ModularPersistence.Sqlite.Options;
using Elsa.ModularPersistence.Sqlite.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.ModularPersistence.Sqlite.Extensions;

public static class SqliteModularPersistenceExtensions
{
    public static ModularPersistenceFeature UseSqliteProvider(this ModularPersistenceFeature feature, string connectionString, Action<SqliteModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(SqliteDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<SqliteModularPersistenceFeature>(sqlite =>
        {
            sqlite.ConfigureOptions = options =>
            {
                options.ConnectionString = connectionString;
                configure?.Invoke(options);
            };
        });
        return feature;
    }

    public static ModularPersistenceFeature UseSqliteProvider(this ModularPersistenceFeature feature, Action<SqliteModularPersistenceOptions>? configure = null)
    {
        feature.UseProvider(SqliteDocumentSchemaMaterializer.ProviderNameValue);
        feature.Module.Use<SqliteModularPersistenceFeature>(sqlite => sqlite.ConfigureOptions = configure);
        return feature;
    }

    public static IServiceCollection AddSqliteModularPersistence(this IServiceCollection services, Action<SqliteModularPersistenceOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<SqliteModularPersistenceOptions>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<SqliteModularPersistenceOptions>>().Value);
        services.TryAddSingleton(sp => new SqliteModularPersistenceConnectionFactory(sp.GetRequiredService<SqliteModularPersistenceOptions>()));
        services.AddSingleton<IStorageManifestMaterializer>(sp => new SqliteDocumentSchemaMaterializer(sp.GetRequiredService<SqliteModularPersistenceConnectionFactory>()));
        services.PostConfigure<ModularPersistenceOptions>(options => options.ProviderName ??= SqliteDocumentSchemaMaterializer.ProviderNameValue);

        return services;
    }
}
