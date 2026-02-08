using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Identity Persistence",
    Description = "Provides MySql persistence for identity management",
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class MySqlIdentityPersistenceShellFeature
    : EFCoreIdentityPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }
}
