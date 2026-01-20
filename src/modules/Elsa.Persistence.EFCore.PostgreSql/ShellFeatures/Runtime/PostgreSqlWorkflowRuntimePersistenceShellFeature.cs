using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Runtime Persistence",
    Description = "Provides PostgreSql persistence for workflow runtime")]
[UsedImplicitly]
public class PostgreSqlWorkflowRuntimePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowRuntimePersistenceShellFeature, RuntimeElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlWorkflowRuntimePersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlWorkflowRuntimePersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlWorkflowRuntimePersistenceShellFeature).Assembly))
    {
    }
}
