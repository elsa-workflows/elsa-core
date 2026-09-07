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

namespace Elsa.UserTasks.Persistence.EFCore.PostgreSql.ShellFeatures;

[ManifestFeatureCategory("User Tasks")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(DisplayName = "PostgreSQL User Tasks Persistence", Description = "Provides PostgreSQL persistence for User Tasks", DependsOn = [typeof(UserTasksFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores User Tasks data in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public sealed class PostgreSqlUserTasksPersistenceShellFeature : EFCoreUserTasksPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options) => builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddPostgreSqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
