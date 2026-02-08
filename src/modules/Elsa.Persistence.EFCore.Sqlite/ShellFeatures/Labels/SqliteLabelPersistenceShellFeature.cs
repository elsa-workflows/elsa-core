using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Label Persistence",
    Description = "Provides Sqlite persistence for label management",
    DependsOn = ["Labels"])]
[UsedImplicitly]
public class SqliteLabelPersistenceShellFeature
    : EFCoreLabelPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlite(migrationsAssembly, connectionString, options);
    }
}
