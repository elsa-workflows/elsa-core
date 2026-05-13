using Elsa.Diagnostics.StructuredLogs.Features;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Extensions;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Features;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Services;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Extensions;

public static class SqliteStructuredLogsModuleExtensions
{
    public static StructuredLogsFeature UseSqliteStorage(this StructuredLogsFeature feature, string connectionString, Action<SqliteStructuredLogOptions>? configure = null)
    {
        feature.Module.Use<SqliteStructuredLogPersistenceFeature>(sqlite =>
        {
            sqlite.ConfigureOptions = options =>
            {
                options.ConnectionString = connectionString;
                configure?.Invoke(options);
            };
        });

        return feature;
    }

    public static StructuredLogsFeature UseSqliteStorage(this StructuredLogsFeature feature, Action<SqliteStructuredLogOptions>? configure = null)
    {
        feature.Module.Use<SqliteStructuredLogPersistenceFeature>(sqlite => sqlite.ConfigureOptions = configure);
        return feature;
    }

    public static IServiceCollection AddSqliteStructuredLogPersistence(this IServiceCollection services, Action<SqliteStructuredLogOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.AddOptions<SqliteStructuredLogOptions>();
        services.AddOptions<RelationalStructuredLogOptions>().Configure<IOptions<SqliteStructuredLogOptions>>((relational, sqlite) => Copy(sqlite.Value.Relational, relational));

        services.AddRelationalStructuredLogPersistence();
        services.TryAddSingleton<IRelationalStructuredLogConnectionFactory, SqliteStructuredLogConnectionFactory>();
        services.TryAddSingleton<IRelationalStructuredLogDialect, SqliteStructuredLogDialect>();
        services.TryAddSingleton<IStructuredLogSchemaMigrator, SqliteStructuredLogSchemaMigrator>();
        services.AddHostedService<SqliteStructuredLogStartupService>();

        return services;
    }

    private static void Copy(RelationalStructuredLogOptions source, RelationalStructuredLogOptions target)
    {
        target.WriteQueue.Capacity = source.WriteQueue.Capacity;
        target.WriteQueue.BatchSize = source.WriteQueue.BatchSize;
        target.WriteQueue.FlushInterval = source.WriteQueue.FlushInterval;
        target.WriteQueue.ShutdownFlushTimeout = source.WriteQueue.ShutdownFlushTimeout;
        target.Retention.MaxAge = source.Retention.MaxAge;
        target.Retention.MaxRows = source.Retention.MaxRows;
        target.Retention.CleanupOnStartup = source.Retention.CleanupOnStartup;
    }
}
