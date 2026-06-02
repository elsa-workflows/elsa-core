using CShells.Features;
using Elsa.ModularPersistence.PostgreSql.Extensions;
using Elsa.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.PostgreSql.ShellFeatures;

[ShellFeature(
    DisplayName = "PostgreSQL Modular Persistence",
    Description = "Provides PostgreSQL storage for modular persistence manifests.",
    DependsOn = ["ModularPersistence"])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores modular persistence documents in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlModularPersistenceFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Connection String",
        Description = "PostgreSQL connection string used by modular persistence.",
        Category = "Persistence",
        Required = true,
        DefaultValue = "Host=localhost;Database=ElsaModularPersistence;Username=postgres;Password=postgres",
        Sensitive = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Host=localhost;Database=ElsaModularPersistence;Username=postgres;Password=postgres";

    [ManifestSetting(
        DisplayName = "Use Optimized JSONB Indexes",
        Description = "Create additional JSONB expression indexes for declared storage indexes.",
        Category = "Persistence",
        DefaultValue = "false",
        RestartRequired = true)]
    public bool UseOptimizedJsonbIndexes { get; set; }

    [ManifestSetting(
        DisplayName = "Schema Lock Key",
        Description = "PostgreSQL transaction-scoped advisory lock key used during schema materialization.",
        Category = "Persistence",
        DefaultValue = "76060001",
        RestartRequired = true)]
    public long SchemaLockKey { get; set; } = 76060001;

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddPostgreSqlModularPersistence(options =>
        {
            options.ConnectionString = ConnectionString;
            options.UseOptimizedJsonbIndexes = UseOptimizedJsonbIndexes;
            options.SchemaLockKey = SchemaLockKey;
        });
    }
}
