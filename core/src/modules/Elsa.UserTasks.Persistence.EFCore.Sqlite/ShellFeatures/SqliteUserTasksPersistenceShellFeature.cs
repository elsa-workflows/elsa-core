using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.UserTasks.Persistence.EFCore.ShellFeatures;
using Elsa.UserTasks.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.UserTasks.Persistence.EFCore.Sqlite.ShellFeatures;

[ManifestFeatureCategory("User Tasks")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "SQLite User Tasks Persistence",
    Description = "Provides SQLite persistence for User Tasks",
    DependsOn = [typeof(UserTasksFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlite-database", "database", Reason = "Stores User Tasks data in SQLite.", Providers = new[] { "SQLite" }, ConfigurationKeys = new[] { "ConnectionString" })]
public sealed class SqliteUserTasksPersistenceShellFeature : EFCoreUserTasksPersistenceShellFeatureBase
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
