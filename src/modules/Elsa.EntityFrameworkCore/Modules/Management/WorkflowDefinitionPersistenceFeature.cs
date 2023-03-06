using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowDefinitionsFeature"/> feature with an Entity Framework Core persistence provider.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
public class EFCoreWorkflowDefinitionPersistenceFeature : PersistenceFeatureBase<ManagementElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreWorkflowDefinitionPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowDefinitionsFeature>(feature =>
        {
            feature.WorkflowDefinitionStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddEntityStore<WorkflowDefinition, EFCoreWorkflowDefinitionStore>();
    }
}