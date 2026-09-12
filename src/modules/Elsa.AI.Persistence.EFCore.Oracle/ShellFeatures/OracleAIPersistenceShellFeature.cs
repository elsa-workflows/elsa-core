using System.Reflection;
using CShells.Features;
using Elsa.AI.Host.ShellFeatures;
using Elsa.AI.Persistence.EFCore.ShellFeatures;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.AI.Persistence.EFCore.Oracle.ShellFeatures;

[ManifestFeatureCategory("AI")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "Oracle AI Persistence",
    Description = "Provides Oracle persistence for Weaver AI conversations, proposals and audit records",
    DependsOn = [typeof(AIFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores Weaver AI persistence data in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleAIPersistenceShellFeature : EFCoreAIPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
