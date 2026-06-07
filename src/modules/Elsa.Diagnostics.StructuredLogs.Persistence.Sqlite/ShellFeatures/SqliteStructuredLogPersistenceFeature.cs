using CShells.Features;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.ShellFeatures;
using Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Sqlite.ShellFeatures;

/// <summary>
/// Provides SQLite persistence for diagnostics structured logs.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Diagnostics)]
[ManifestFeatureCategory(ManifestFeatureCategories.Persistence)]
[ShellFeature(
    DisplayName = "SQLite Structured Log Persistence",
    Description = "Provides SQLite persistence for diagnostics structured logs",
    DependsOn = [typeof(StructuredLogRelationalPersistenceFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores structured log records in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteStructuredLogPersistenceFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Connection String",
        Description = "SQLite connection string used to store structured log records.",
        Category = "Persistence",
        Required = true,
        DefaultValue = "Data Source=elsa-structured-logs.db",
        Sensitive = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Data Source=elsa-structured-logs.db";

    [ManifestSetting(
        DisplayName = "Run Migrations On Startup",
        Description = "Run structured log SQLite schema migrations when the application starts.",
        Category = "Persistence",
        DefaultValue = "true",
        RestartRequired = true)]
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
