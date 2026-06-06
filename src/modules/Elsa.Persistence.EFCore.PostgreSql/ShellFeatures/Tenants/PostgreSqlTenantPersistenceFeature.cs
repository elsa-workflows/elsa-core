using System.Reflection;
using CShells.Features;
using Elsa.Platform.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Tenants;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Tenants;

/// <summary>
/// Configures the tenants feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Tenant Persistence",
    Description = "Provides PostgreSql persistence for tenant management",
    DependsOn = [typeof(global::Elsa.Tenants.ShellFeatures.TenantManagementFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores tenant data in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlTenantPersistenceFeature
    : EFCoreTenantManagementShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddPostgreSqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
