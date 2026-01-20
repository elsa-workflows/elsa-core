using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Management;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Management;

/// <summary>
/// Configures the management feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Workflow Instance Persistence",
    Description = "Provides MySql persistence for workflow instances")]
[UsedImplicitly]
public class MySqlWorkflowInstancePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowInstancePersistenceShellFeature, ManagementElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlWorkflowInstancePersistenceShellFeature"/> class.
    /// </summary>
    public MySqlWorkflowInstancePersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlWorkflowInstancePersistenceShellFeature).Assembly))
    {
    }
}
