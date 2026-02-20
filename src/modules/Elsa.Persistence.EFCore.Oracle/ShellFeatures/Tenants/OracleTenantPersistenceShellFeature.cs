using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Tenant Persistence",
    Description = "Provides Oracle persistence for tenant management",
    DependsOn = ["TenantManagement"])]
[UsedImplicitly]
public class OracleTenantPersistenceShellFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaOracle(migrationsAssembly, connectionString, options);
    }
}
