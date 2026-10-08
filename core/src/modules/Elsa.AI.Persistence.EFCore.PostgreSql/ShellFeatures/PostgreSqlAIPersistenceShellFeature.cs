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

namespace Elsa.AI.Persistence.EFCore.PostgreSql.ShellFeatures;

[ManifestFeatureCategory("AI")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "PostgreSQL AI Persistence",
    Description = "Provides PostgreSQL persistence for Weaver AI conversations, proposals and audit records",
    DependsOn = [typeof(AIFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores Weaver AI persistence data in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlAIPersistenceShellFeature : EFCoreAIPersistenceShellFeatureBase
{
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    }

    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddPostgreSqlEntityModelCreatingHandlers();
        base.OnConfiguring(services);
    }
}
