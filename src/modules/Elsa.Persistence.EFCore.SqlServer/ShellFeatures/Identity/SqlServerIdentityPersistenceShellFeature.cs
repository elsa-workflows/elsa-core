using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Identity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Identity;

/// <summary>
/// Configures the identity feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Identity Persistence",
    Description = "Provides SqlServer persistence for identity management",
    DependsOn = ["Identity"])]
[UsedImplicitly]
public class SqlServerIdentityPersistenceShellFeature
    : EFCoreIdentityPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
