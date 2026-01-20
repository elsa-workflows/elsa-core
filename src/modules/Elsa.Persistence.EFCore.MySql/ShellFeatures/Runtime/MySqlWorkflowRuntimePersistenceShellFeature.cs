using CShells.Features;
using Elsa.Persistence.EFCore.Modules.Runtime;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Elsa.Persistence.EFCore.MySql.ShellFeatures.Runtime;

/// <summary>
/// Configures the runtime feature to use MySql persistence.
/// </summary>
[ShellFeature(
    DisplayName = "MySql Workflow Runtime Persistence",
    Description = "Provides MySql persistence for workflow runtime")]
[UsedImplicitly]
public class MySqlWorkflowRuntimePersistenceShellFeature
    : DatabaseProviderShellFeature<EFCoreWorkflowRuntimePersistenceShellFeature, RuntimeElsaDbContext, MySqlDbContextOptionsBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlWorkflowRuntimePersistenceShellFeature"/> class.
    /// </summary>
    public MySqlWorkflowRuntimePersistenceShellFeature()
        : base(new MySqlProviderConfigurator(typeof(MySqlWorkflowRuntimePersistenceShellFeature).Assembly))
    {
    }
}
