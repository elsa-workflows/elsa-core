using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using Elsa.Tenants.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use Oracle persistence.
/// </summary>
[ManifestFeatureCategory("Persistence")]
[ManifestFeatureCategory("Tenancy")]
[ShellFeature(
    DisplayName = "Oracle Tenant Persistence",
    Description = "Provides Oracle persistence for tenant management",
    DependsOn = [typeof(TenantManagementFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("oracle-database", "database", Reason = "Stores tenant data in Oracle Database.", Providers = new[] { "Oracle" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class OracleTenantPersistenceShellFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        options = options.ConfigureOracle();
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
