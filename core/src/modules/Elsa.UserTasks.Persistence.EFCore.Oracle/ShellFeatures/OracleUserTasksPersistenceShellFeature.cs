using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.UserTasks.Persistence.EFCore.ShellFeatures;
using Elsa.UserTasks.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.UserTasks.Persistence.EFCore.Oracle.ShellFeatures;

[ManifestFeatureCategory("User Tasks")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(DisplayName = "Oracle User Tasks Persistence", Description = "Provides Oracle persistence for User Tasks", DependsOn = [typeof(UserTasksFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores User Tasks data in Oracle.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public sealed class OracleUserTasksPersistenceShellFeature : EFCoreUserTasksPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options) => builder.UseElsaOracle(migrationsAssembly, connectionString, options.ConfigureOracle());
}
