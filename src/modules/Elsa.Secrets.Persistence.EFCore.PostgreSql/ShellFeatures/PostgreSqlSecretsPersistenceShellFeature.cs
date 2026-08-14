using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Secrets.Persistence.EFCore.ShellFeatures;
using Elsa.Secrets.ShellFeatures;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Persistence.EFCore.PostgreSql.ShellFeatures;

[ManifestFeatureCategory("Secrets")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "PostgreSql Secrets Persistence",
    Description = "Provides PostgreSql persistence for secrets",
    DependsOn = [typeof(SecretsFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores secrets data in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlSecretsPersistenceShellFeature : EFCoreSecretsPersistenceShellFeatureBase
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
