using CShells.Features;
using Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.Extensions;
using Elsa.ExternalAuthentication.Persistence.EFCore.ShellFeatures;
using Elsa.ExternalAuthentication.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.Oracle.ShellFeatures;

[ManifestFeatureCategory("Security")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "Oracle External Authentication Persistence",
    Description = "Provides Oracle persistence for external authentication",
    DependsOn = [typeof(ExternalAuthenticationShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores external authentication connections, links, sessions and grants in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleExternalAuthenticationPersistenceShellFeature : EFCoreExternalAuthenticationPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureExternalAuthenticationOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
