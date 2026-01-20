using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Label Persistence",
    Description = "Provides SqlServer persistence for label management")]
[UsedImplicitly]
public class SqlServerLabelPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreLabelPersistenceShellFeature, LabelsElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerLabelPersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerLabelPersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerLabelPersistenceShellFeature).Assembly))
    {
    }
}
