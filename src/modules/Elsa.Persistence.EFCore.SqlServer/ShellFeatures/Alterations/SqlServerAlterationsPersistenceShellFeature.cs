using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Alterations;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Alterations;

/// <summary>
/// Configures the alterations feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Alterations Persistence",
    Description = "Provides SqlServer persistence for workflow alterations")]
[UsedImplicitly]
public class SqlServerAlterationsPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreAlterationsPersistenceShellFeature, AlterationsElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerAlterationsPersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerAlterationsPersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerAlterationsPersistenceShellFeature).Assembly))
    {
    }
}
