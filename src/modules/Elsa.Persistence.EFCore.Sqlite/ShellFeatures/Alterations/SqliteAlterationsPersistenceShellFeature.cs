using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Alterations Persistence",
    Description = "Provides Sqlite persistence for workflow alterations",
    DependsOn = ["Alterations"])]
[UsedImplicitly]
public class SqliteAlterationsPersistenceShellFeature
    : EFCoreAlterationsPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }
}
