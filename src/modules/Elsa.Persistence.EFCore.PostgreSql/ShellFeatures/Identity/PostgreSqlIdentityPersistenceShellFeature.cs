using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Identity Persistence",
    Description = "Provides PostgreSql persistence for identity management",
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class PostgreSqlIdentityPersistenceShellFeature
    : EFCoreIdentityPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaPostgreSql(migrationsAssembly, connectionString, options);
    }
}
