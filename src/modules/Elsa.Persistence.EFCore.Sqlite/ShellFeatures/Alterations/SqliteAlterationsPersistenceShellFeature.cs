using System.Reflection;
using CShells.Features;
using Elsa.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Alterations Persistence",
    Description = "Provides Sqlite persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores workflow alteration records in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddSqliteEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
