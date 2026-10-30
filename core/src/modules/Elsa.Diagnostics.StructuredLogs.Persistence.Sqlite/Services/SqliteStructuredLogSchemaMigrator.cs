using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Migrations;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Options;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Services;

public class SqliteStructuredLogSchemaMigrator(IOptions<SqliteStructuredLogOptions> options) : IStructuredLogSchemaMigrator
{
    public ValueTask MigrateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var services = new ServiceCollection()
            .AddLogging()
            .AddFluentMigratorCore()
            .ConfigureRunner(builder => builder
                .AddSQLite()
                .WithGlobalConnectionString(options.Value.ConnectionString)
                .ScanIn(typeof(M001CreateStructuredLogTables).Assembly).For.Migrations())
            .BuildServiceProvider(false);

        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMigrationRunner>().MigrateUp();
        return ValueTask.CompletedTask;
    }
}
