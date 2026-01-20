using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Tenant Persistence",
    Description = "Provides PostgreSql persistence for tenant management")]
[UsedImplicitly]
public class PostgreSqlTenantPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreTenantManagementShellFeature, TenantsElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlTenantPersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlTenantPersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlTenantPersistenceShellFeature).Assembly))
    {
    }
}
