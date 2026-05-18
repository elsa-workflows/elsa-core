using System.Reflection;
using CShells.Features;
using Elsa.PackageManifest.Generator.Hints;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Alterations Persistence",
    Description = "Provides PostgreSql persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
[ManifestInfrastructure("postgresql-database", "database", Reason = "Stores workflow alteration records in PostgreSQL.", Providers = new[] { "PostgreSQL" }, ConfigurationKeys = new[] { "ConnectionString" })]
public class PostgreSqlAlterationsPersistenceFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
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
