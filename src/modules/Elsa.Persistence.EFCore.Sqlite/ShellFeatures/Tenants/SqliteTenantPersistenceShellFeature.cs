using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Tenant Persistence",
    Description = "Provides Sqlite persistence for tenant management")]
[UsedImplicitly]
public class SqliteTenantPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreTenantManagementShellFeature, TenantsElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteTenantPersistenceShellFeature"/> class.
    /// </summary>
    public SqliteTenantPersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteTenantPersistenceShellFeature).Assembly))
    {
    }
}
