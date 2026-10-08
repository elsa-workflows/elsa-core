using CShells.Features;
using Elsa.ExternalAuthentication.Persistence.EFCore.ShellFeatures;
using Elsa.ExternalAuthentication.ShellFeatures;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore;
using Elsa.Platform.PackageManifest.Generator.Hints;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Elsa.ExternalAuthentication.Persistence.EFCore.PostgreSql.ShellFeatures;

[ManifestFeatureCategory("Security")]
[ManifestFeatureCategory("Persistence")]
[ShellFeature(
    DisplayName = "PostgreSql External Authentication Persistence",
    Description = "Provides PostgreSql persistence for external authentication",
    DependsOn = [typeof(ExternalAuthenticationShellFeature)])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores external authentication connections, links, sessions and grants in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlExternalAuthenticationPersistenceShellFeature : EFCoreExternalAuthenticationPersistenceShellFeatureBase
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
