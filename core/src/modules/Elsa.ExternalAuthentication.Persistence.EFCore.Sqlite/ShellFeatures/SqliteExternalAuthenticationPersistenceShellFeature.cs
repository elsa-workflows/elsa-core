using CShells.Features;
using Elsa.ExternalAuthentication.Persistence.EFCore.ShellFeatures;
using Elsa.ExternalAuthentication.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Sqlite.ShellFeatures;

[ManifestFeatureCategory("Security")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "Sqlite External Authentication Persistence",
    Description = "Provides Sqlite persistence for external authentication",
    DependsOn = [typeof(ExternalAuthenticationShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores external authentication connections, links, sessions and grants in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqliteExternalAuthenticationPersistenceShellFeature : EFCoreExternalAuthenticationPersistenceShellFeatureBase
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
