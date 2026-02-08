using System.Reflection;
using CShells.Features;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Label Persistence",
    Description = "Provides MySql persistence for label management",
    DependsOn = ["Labels"])]
[UsedImplicitly]
public class MySqlLabelPersistenceShellFeature
    : EFCoreLabelPersistenceShellFeatureBase
{
    /// <inheritdoc />
    protected override void ConfigureProvider(DbContextOptionsBuilder builder, Assembly migrationsAssembly, string connectionString, ElsaDbContextOptions? options)
    {
        builder.UseElsaMySql(migrationsAssembly, connectionString, options);
    }
}
