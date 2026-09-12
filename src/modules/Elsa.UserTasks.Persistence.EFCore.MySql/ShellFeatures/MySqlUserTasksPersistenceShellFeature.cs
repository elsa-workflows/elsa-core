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

namespace Elsa.UserTasks.Persistence.EFCore.MySql.ShellFeatures;

[ManifestFeatureCategory("User Tasks")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(DisplayName = "MySQL User Tasks Persistence", Description = "Provides MySQL persistence for User Tasks", DependsOn = [typeof(UserTasksFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("mysql-database", "database", Reason = "Stores User Tasks data in MySQL.", Providers = new[] { "MySQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public sealed class MySqlUserTasksPersistenceShellFeature : EFCoreUserTasksPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options) => builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddMySqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
