using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Secrets.Persistence.EFCore.Oracle.Extensions;
using Elsa.Secrets.Persistence.EFCore.ShellFeatures;
using Elsa.Secrets.ShellFeatures;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore.Oracle.ShellFeatures;

[ShellFeature(
    DisplayName = "Oracle Secrets Persistence",
    Description = "Provides Oracle persistence for secrets",
    DependsOn = [typeof(SecretsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores secrets data in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleSecretsPersistenceShellFeature : EFCoreSecretsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureSecretsOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
