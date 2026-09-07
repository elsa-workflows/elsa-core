using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.UserTasks.Persistence.EFCore.ShellFeatures;
using Elsa.UserTasks.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore.SqlServer.ShellFeatures;

[ManifestFeatureCategory("User Tasks")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(DisplayName = "SQL Server User Tasks Persistence", Description = "Provides SQL Server persistence for User Tasks", DependsOn = [typeof(UserTasksFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores User Tasks data in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public sealed class SqlServerUserTasksPersistenceShellFeature : EFCoreUserTasksPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options) => builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
}
