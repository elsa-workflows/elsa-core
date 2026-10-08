using System.Reflection;
using CShells.Features;
using Elsa.AI.Host.ShellFeatures;
using Elsa.AI.Persistence.EFCore.ShellFeatures;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.Sqlite.ShellFeatures;

[ManifestFeatureCategory("AI")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "SQLite AI Persistence",
    Description = "Provides SQLite persistence for Weaver AI conversations, proposals and audit records",
    DependsOn = [typeof(AIFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores Weaver AI persistence data in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteAIPersistenceShellFeature : EFCoreAIPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }

    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddSqliteEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
