using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Alterations Persistence",
    Description = "Provides PostgreSql persistence for workflow alterations")]
[UsedImplicitly]
public class PostgreSqlAlterationsPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreAlterationsPersistenceShellFeature, AlterationsElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlAlterationsPersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlAlterationsPersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlAlterationsPersistenceShellFeature).Assembly))
    {
    }
}
