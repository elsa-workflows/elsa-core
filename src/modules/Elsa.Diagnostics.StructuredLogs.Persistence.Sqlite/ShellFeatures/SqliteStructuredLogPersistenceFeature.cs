using CShells.Features;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.ShellFeatures;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.ShellFeatures;

/// <summary>
/// Provides SQLite persistence for diagnostics structured logs.
/// </summary>
[ShellFeature(
    DisplayName = "SQLite Structured Log Persistence",
    Description = "Provides SQLite persistence for diagnostics structured logs",
    DependsOn = [typeof(StructuredLogRelationalPersistenceFeature)])]
[UsedImplicitly]
public class SqliteStructuredLogPersistenceFeature : IShellFeature
{
    public string ConnectionString { get; set; } = "Data Source=elsa-structured-logs.db";
    public bool RunMigrationsOnStartup { get; set; } = true;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSqliteStructuredLogPersistence(options =>
        {
            options.ConnectionString = ConnectionString;
            options.RunMigrationsOnStartup = RunMigrationsOnStartup;
        });
    }
}
