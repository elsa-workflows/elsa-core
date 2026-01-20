using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Oracle.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use Oracle persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Oracle Tenant Persistence",
    Description = "Provides Oracle persistence for tenant management")]
[UsedImplicitly]
public class OracleTenantPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreTenantManagementShellFeature, TenantsElsaDbContext, OracleDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OracleTenantPersistenceShellFeature"/> class.
    /// </summary>
    public OracleTenantPersistenceShellFeature()
        : base(new OracleProviderConfigurator(typeof(OracleTenantPersistenceShellFeature).Assembly))
    {
    }
}
