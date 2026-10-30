using System.Reflection;
using CShells.Features;
using Elsa.Labels.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use MySql persistence.
/// </summary>
[ManifestFeatureCategory("Persistence")]
[ManifestFeatureCategory("Labels")]
[ShellFeature(
    DisplayName = "MySql Label Persistence",
    Description = "Provides MySql persistence for label management",
    DependsOn = [typeof(LabelsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("mysql-database", "database", Reason = "Stores workflow label data in MySQL.", Providers = new[] { "MySQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class MySqlLabelPersistenceShellFeature
    : EFCoreLabelPersistenceShellFeatureBase
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
