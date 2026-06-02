using Elsa.Extensions;
using Elsa.ModularPersistence.Contracts;
using Elsa.ModularPersistence.Features;
using Elsa.ModularPersistence.Options;
using Elsa.ModularPersistence.SqlServer.Features;
using Elsa.ModularPersistence.SqlServer.Options;
using Elsa.ModularPersistence.SqlServer.Services;
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
        services.AddSingleton<IStorageManifestMaterializer>(sp =>
        {
            var options = sp.GetRequiredService<SqlServerModularPersistenceOptions>();
            return new SqlServerDocumentSchemaMaterializer(sp.GetRequiredService<SqlServerModularPersistenceConnectionFactory>(), options);
        });
        services.PostConfigure<ModularPersistenceOptions>(options => options.ProviderName ??= SqlServerDocumentSchemaMaterializer.ProviderNameValue);

        return services;
    }
}
