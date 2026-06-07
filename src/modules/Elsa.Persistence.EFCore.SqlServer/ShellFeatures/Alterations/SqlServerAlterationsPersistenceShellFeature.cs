using System.Reflection;
using CShells.Features;
using Elsa.Alterations.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use SqlServer persistence.
/// </summary>
[ManifestFeatureCategory(ManifestFeatureCategories.Persistence)]
[ManifestFeatureCategory(ManifestFeatureCategories.Alterations)]
[ShellFeature(
    DisplayName = "SqlServer Alterations Persistence",
    Description = "Provides SqlServer persistence for workflow alterations",
    DependsOn = [typeof(AlterationsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("sqlserver-database", "database", Reason = "Stores workflow alteration records in SQL Server.", Providers = new[] { "SQL Server" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class SqlServerAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
