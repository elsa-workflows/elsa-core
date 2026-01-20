using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.SqlServer.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use SqlServer persistence.
/// </summary>
[ShellFeature(
    DisplayName = "SqlServer Workflow Definition Persistence",
    Description = "Provides SqlServer persistence for workflow definitions")]
[UsedImplicitly]
public class SqlServerWorkflowDefinitionPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowDefinitionPersistenceShellFeature, ManagementElsaDbContext, SqlServerDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerWorkflowDefinitionPersistenceShellFeature"/> class.
    /// </summary>
    public SqlServerWorkflowDefinitionPersistenceShellFeature()
        : base(new SqlServerProviderConfigurator(typeof(SqlServerWorkflowDefinitionPersistenceShellFeature).Assembly))
    {
    }
}
