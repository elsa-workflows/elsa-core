using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Base class for workflow instance persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreWorkflowInstancePersistenceShellFeatureBase : PersistenceShellFeatureBase<ManagementElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<IWorkflowInstanceStore, EFCoreWorkflowInstanceStore>();
        AddEntityStore<WorkflowInstance, EFCoreWorkflowInstanceStore>(services);
    }
}
