using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Instance Persistence",
    Description = "Provides PostgreSql persistence for workflow instances")]
[UsedImplicitly]
public class PostgreSqlWorkflowInstancePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowInstancePersistenceShellFeature, ManagementElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlWorkflowInstancePersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlWorkflowInstancePersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlWorkflowInstancePersistenceShellFeature).Assembly))
    {
    }
}
