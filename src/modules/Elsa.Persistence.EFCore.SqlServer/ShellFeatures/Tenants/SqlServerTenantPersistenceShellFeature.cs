using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Tenant Persistence",
    Description = "Provides SqlServer persistence for tenant management",
    DependsOn = ["TenantManagement"])]
[UsedImplicitly]
public class SqlServerTenantPersistenceShellFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
