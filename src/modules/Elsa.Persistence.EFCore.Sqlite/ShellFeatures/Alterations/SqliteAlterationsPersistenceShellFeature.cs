using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.Sqlite.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use Sqlite persistence.
/// </summary>
[ShellFeature(
    DisplayName = "Sqlite Alterations Persistence",
    Description = "Provides Sqlite persistence for workflow alterations")]
[UsedImplicitly]
public class SqliteAlterationsPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreAlterationsPersistenceShellFeature, AlterationsElsaDbContext, SqliteDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteAlterationsPersistenceShellFeature"/> class.
    /// </summary>
    public SqliteAlterationsPersistenceShellFeature()
        : base(new SqliteProviderConfigurator(typeof(SqliteAlterationsPersistenceShellFeature).Assembly))
    {
    }
}
