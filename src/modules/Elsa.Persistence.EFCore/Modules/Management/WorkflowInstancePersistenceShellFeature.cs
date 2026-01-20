using CShells.Features;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Configures the workflow instances feature with an Entity Framework Core persistence provider.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Workflow Instance Persistence",
    Description = "Provides Entity Framework Core persistence for workflow instances",
    DependsOn = ["WorkflowManagement", "WorkflowInstances"])]
[UsedImplicitly]
public class EFCoreWorkflowInstancePersistenceShellFeature : PersistenceShellFeatureBase<EFCoreWorkflowInstancePersistenceShellFeature, ManagementElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<WorkflowInstance, EFCoreWorkflowInstanceStore>(services);
    }
}
