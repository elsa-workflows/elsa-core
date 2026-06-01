using CShells.Features;
using Elsa.ModularPersistence.Sqlite.Extensions;
using Elsa.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.Sqlite.ShellFeatures;

[ShellFeature(
    DisplayName = "SQLite Modular Persistence",
    Description = "Provides SQLite storage for modular persistence manifests.",
    DependsOn = ["ModularPersistence"])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores modular persistence documents in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteModularPersistenceFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Connection String",
        Description = "SQLite connection string used by modular persistence.",
        Category = "Persistence",
        Required = true,
        DefaultValue = "Data Source=modular-persistence.db",
        Sensitive = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Data Source=modular-persistence.db";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSqliteModularPersistence(options => options.ConnectionString = ConnectionString);
    }
}
