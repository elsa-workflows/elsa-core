using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Tenant Persistence",
    Description = "Provides MySql persistence for tenant management")]
[UsedImplicitly]
public class MySqlTenantPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreTenantManagementShellFeature, TenantsElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlTenantPersistenceShellFeature"/> class.
    /// </summary>
    public MySqlTenantPersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlTenantPersistenceShellFeature).Assembly))
    {
    }
}
