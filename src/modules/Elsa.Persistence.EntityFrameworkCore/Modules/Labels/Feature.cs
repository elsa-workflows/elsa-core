using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Extensions;
using Elsa.Labels.Features;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Labels;

[DependsOn(typeof(LabelsFeature))]
public class EFCoreLabelPersistenceFeature : PersistenceFeatureBase<LabelsDbContext>
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

        AddStore<Label, EFCoreLabelStore>();
        AddStore<WorkflowDefinitionLabel, EFCoreWorkflowDefinitionLabelStore>();
    }
}