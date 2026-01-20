using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Workflow Definition Persistence",
    Description = "Provides MySql persistence for workflow definitions")]
[UsedImplicitly]
public class MySqlWorkflowDefinitionPersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowDefinitionPersistenceShellFeature, ManagementElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlWorkflowDefinitionPersistenceShellFeature"/> class.
    /// </summary>
    public MySqlWorkflowDefinitionPersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlWorkflowDefinitionPersistenceShellFeature).Assembly))
    {
    }
}
