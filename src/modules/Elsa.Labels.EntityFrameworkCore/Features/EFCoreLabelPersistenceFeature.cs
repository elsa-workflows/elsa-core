using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.EntityFrameworkCore.Implementations;
using Elsa.Labels.Extensions;
using Elsa.Labels.Features;
using Elsa.Persistence.EntityFrameworkCore.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.EntityFrameworkCore.Features;

[DependsOn(typeof(LabelsFeature))]
public class EFCoreLabelPersistenceFeature : EFCorePersistenceFeature<LabelsDbContext>
{
    public EFCoreLabelPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.UseLabels(labels => labels
            .WithLabelStore(sp => sp.GetRequiredService<EFCoreLabelStore>())
            .WithWorkflowDefinitionLabelStore(sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>())
        );
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<Label, EFCoreLabelStore>(Services);
        AddStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>(Services);
    }
}