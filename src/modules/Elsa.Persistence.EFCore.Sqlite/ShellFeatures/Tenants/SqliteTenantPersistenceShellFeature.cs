using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Tenant Persistence",
    Description = "Provides Sqlite persistence for tenant management",
    DependsOn = ["TenantManagement"])]
[UsedImplicitly]
public class SqliteTenantPersistenceShellFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }
}
