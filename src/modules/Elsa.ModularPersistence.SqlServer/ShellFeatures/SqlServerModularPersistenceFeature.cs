using CShells.Features;
using Elsa.ModularPersistence.SqlServer.Extensions;
using Elsa.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ModularPersistence.SqlServer.ShellFeatures;

[ShellFeature(
    DisplayName = "SQL Server Modular Persistence",
    Description = "Provides SQL Server storage for modular persistence manifests.",
    DependsOn = ["ModularPersistence"])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores modular persistence documents in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqlServerModularPersistenceFeature : IShellFeature
{
    [ManifestSetting(
        DisplayName = "Connection String",
        Description = "SQL Server connection string used by modular persistence.",
        Category = "Persistence",
        Required = true,
        DefaultValue = "Server=(localdb)\\MSSQLLocalDB;Database=ElsaModularPersistence;Trusted_Connection=True;TrustServerCertificate=True",
        Sensitive = true,
        RestartRequired = true)]
    public string ConnectionString { get; set; } = "Server=(localdb)\\MSSQLLocalDB;Database=ElsaModularPersistence;Trusted_Connection=True;TrustServerCertificate=True";

    [ManifestSetting(
        DisplayName = "Use Optimized Indexes",
        Description = "Create additional filtered indexes for declared storage indexes.",
        Category = "Persistence",
        DefaultValue = "false",
        RestartRequired = true)]
    public bool UseOptimizedIndexes { get; set; }

    [ManifestSetting(
        DisplayName = "Schema Lock Timeout",
        Description = "Maximum time to wait for the transactional schema materialization lock.",
        Category = "Persistence",
        DefaultValue = "00:01:00",
        RestartRequired = true)]
    public TimeSpan SchemaLockTimeout { get; set; } = TimeSpan.FromMinutes(1);

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSqlServerModularPersistence(options =>
        {
            options.ConnectionString = ConnectionString;
            options.UseOptimizedIndexes = UseOptimizedIndexes;
            options.SchemaLockTimeout = SchemaLockTimeout;
        });
    }
}
