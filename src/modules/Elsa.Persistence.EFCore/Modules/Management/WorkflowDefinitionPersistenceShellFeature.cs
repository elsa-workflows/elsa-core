using CShells.Features;
using Elsa.Workflows.Management.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>
/// Configures the workflow definitions feature with an Entity Framework Core persistence provider.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Workflow Definition Persistence",
    Description = "Provides Entity Framework Core persistence for workflow definitions",
    DependsOn = ["WorkflowManagement", "WorkflowDefinitions"])]
[UsedImplicitly]
public class EFCoreWorkflowDefinitionPersistenceShellFeature : PersistenceShellFeatureBase<EFCoreWorkflowDefinitionPersistenceShellFeature, ManagementElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>(services);
    }
}
