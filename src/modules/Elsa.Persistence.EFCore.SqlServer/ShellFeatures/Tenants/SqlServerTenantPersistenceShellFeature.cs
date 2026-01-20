using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Tenant Persistence",
    Description = "Provides SqlServer persistence for tenant management")]
[UsedImplicitly]
public class SqlServerTenantPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreTenantManagementShellFeature, TenantsElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerTenantPersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerTenantPersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerTenantPersistenceShellFeature).Assembly))
    {
    }
}
