using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> and <see cref="WorkflowDefinitionsFeature"/> features with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[PublicAPI]
public class EFCoreWorkflowManagementPersistenceFeature : PersistenceFeatureBase<ManagementElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreWorkflowManagementPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature => feature.WorkflowInstanceStore = sp => sp.GetRequiredService<EFCoreWorkflowInstanceStore>());
        Module.Configure<WorkflowDefinitionsFeature>(feature => feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowInstance, EFCoreWorkflowInstanceStore>();
        AddStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>();
    }
}