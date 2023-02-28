using Elsa.EntityFrameworkCore.Common;
using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Labels;

[DependsOn(typeof(LabelsFeature))]
public class EFCoreLabelPersistenceFeature : PersistenceFeatureBase<LabelsElsaDbContext>
{
    public EFCoreLabelPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.UseLabels(labels =>
        {
            labels.LabelStore = sp => sp.GetRequiredService<EFCoreLabelStore>();
            labels.WorkflowDefinitionLabelStore = sp => sp.GetRequiredService<EFCoreWorkflowDefinitionLabelStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddEntityStore<Label, EFCoreLabelStore>();
        AddEntityStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>();
    }
}