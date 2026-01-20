using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Labels;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Labels;

/// <summary>
/// Configures the labels feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Label Persistence",
    Description = "Provides PostgreSql persistence for label management")]
[UsedImplicitly]
public class PostgreSqlLabelPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreLabelPersistenceShellFeature, LabelsElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlLabelPersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlLabelPersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlLabelPersistenceShellFeature).Assembly))
    {
    }
}
