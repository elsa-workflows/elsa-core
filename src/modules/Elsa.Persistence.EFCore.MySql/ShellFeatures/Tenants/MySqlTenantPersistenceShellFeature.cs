using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Tenant Persistence",
    Description = "Provides MySql persistence for tenant management",
    DependsOn = [typeof(global::Elsa.Tenants.ShellFeatures.TenantManagementFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("mysql-database", "database", Reason = "Stores tenant data in MySQL.", Providers = new[] { "MySQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class MySqlTenantPersistenceShellFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddMySqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
