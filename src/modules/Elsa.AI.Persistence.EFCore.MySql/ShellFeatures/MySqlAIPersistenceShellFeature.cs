using System.Reflection;
using CShells.Features;
using Elsa.AI.Host.ShellFeatures;
using Elsa.AI.Persistence.EFCore.ShellFeatures;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.MySql.ShellFeatures;

[ManifestFeatureCategory("AI")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "MySQL AI Persistence",
    Description = "Provides MySQL persistence for Weaver AI conversations, proposals and audit records",
    DependsOn = [typeof(AIFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("mysql-database", "database", Reason = "Stores Weaver AI persistence data in MySQL.", Providers = new[] { "MySQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class MySqlAIPersistenceShellFeature : EFCoreAIPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }

    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddMySqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
