using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Label Persistence",
    Description = "Provides SqlServer persistence for label management",
    DependsOn = ["Labels"])]
[UsedImplicitly]
public class SqlServerLabelPersistenceShellFeature
    : EFCoreLabelPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaSqlServer(migrationsAssembly, connectionString, options);
    }
}
