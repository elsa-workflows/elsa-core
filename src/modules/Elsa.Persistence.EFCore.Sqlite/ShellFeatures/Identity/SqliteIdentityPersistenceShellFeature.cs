using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Identity Persistence",
    Description = "Provides Sqlite persistence for identity management",
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class SqliteIdentityPersistenceShellFeature
    : EFCoreIdentityPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }

    /// <inheritdoc />
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<IEntityModelCreatingHandler, SetupForSqlite>();
        base.OnConfiguring(services);
    }
}
