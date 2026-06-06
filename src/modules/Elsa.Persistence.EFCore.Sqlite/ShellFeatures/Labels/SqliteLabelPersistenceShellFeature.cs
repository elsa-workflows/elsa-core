using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Label Persistence",
    Description = "Provides Sqlite persistence for label management",
    DependsOn = [typeof(global::Elsa.Labels.ShellFeatures.LabelsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores workflow label data in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteLabelPersistenceShellFeature
    : EFCoreLabelPersistenceShellFeatureBase
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
