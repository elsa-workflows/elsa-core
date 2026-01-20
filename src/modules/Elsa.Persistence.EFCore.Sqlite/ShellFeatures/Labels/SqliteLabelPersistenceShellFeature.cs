using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Label Persistence",
    Description = "Provides Sqlite persistence for label management")]
[UsedImplicitly]
public class SqliteLabelPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreLabelPersistenceShellFeature, LabelsElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteLabelPersistenceShellFeature"/> class.
    /// </summary>
    public SqliteLabelPersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteLabelPersistenceShellFeature).Assembly))
    {
    }
}
