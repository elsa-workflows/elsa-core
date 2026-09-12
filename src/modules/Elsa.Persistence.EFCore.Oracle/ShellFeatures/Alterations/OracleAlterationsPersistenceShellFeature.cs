using System.Reflection;
using CShells.Features;
using Elsa.Alterations.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use Oracle persistence.
/// </summary>
[ManifestFeatureCategory("Persistence")]
[ManifestFeatureCategory("Alterations")]
[ShellFeature(
    DisplayName = "Oracle Alterations Persistence",
    Description = "Provides Oracle persistence for workflow alterations",
    DependsOn = [typeof(AlterationsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores workflow alteration records in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
