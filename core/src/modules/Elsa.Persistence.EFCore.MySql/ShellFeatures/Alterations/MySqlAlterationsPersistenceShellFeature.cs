using System.Reflection;
using CShells.Features;
using Elsa.Alterations.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use MySql persistence.
/// </summary>
[ManifestFeatureCategory("Persistence")]
[ManifestFeatureCategory("Alterations")]
[ShellFeature(
    DisplayName = "MySql Alterations Persistence",
    Description = "Provides MySql persistence for workflow alterations",
    DependsOn = [typeof(AlterationsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("mysql-database", "database", Reason = "Stores workflow alteration records in MySQL.", Providers = new[] { "MySQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class MySqlAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
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
