using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Tenant Persistence",
    Description = "Provides MySql persistence for tenant management",
    DependsOn = ["TenantManagement"])]
[UsedImplicitly]
public class MySqlTenantPersistenceShellFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }
}
