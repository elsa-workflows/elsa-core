using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Elsa.Persistence.EFCore.PostgreSql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use PostgreSql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "PostgreSql Workflow Definition Persistence",
    Description = "Provides PostgreSql persistence for workflow definitions")]
[UsedImplicitly]
public class PostgreSqlWorkflowDefinitionPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowDefinitionPersistenceShellFeature, ManagementElsaDbContext, NpgsqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlWorkflowDefinitionPersistenceShellFeature"/> class.
    /// </summary>
    public PostgreSqlWorkflowDefinitionPersistenceShellFeature()
        : base(new PostgreSqlProviderConfigurator(typeof(PostgreSqlWorkflowDefinitionPersistenceShellFeature).Assembly))
    {
    }
}
