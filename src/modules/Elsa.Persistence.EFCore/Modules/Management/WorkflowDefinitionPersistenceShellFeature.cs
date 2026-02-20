using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Base class for workflow definition persistence features.
/// This is not a standalone shell feature - use provider-specific features like SqliteWorkflowDefinitionPersistenceShellFeature.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreWorkflowDefinitionPersistenceShellFeatureBase : PersistenceShellFeatureBase<ManagementElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<IWorkflowDefinitionStore, EFCoreWorkflowDefinitionStore>();
        AddEntityStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>(services);
    }
}
