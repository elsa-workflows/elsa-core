using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Identity Persistence",
    Description = "Provides Oracle persistence for identity management",
    DependsOn = [typeof(global::Elsa.Identity.ShellFeatures.IdentityFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores identity data in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleIdentityPersistenceShellFeature
    : EFCoreIdentityPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
