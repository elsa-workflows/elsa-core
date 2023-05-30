using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Features;
using Elsa.MongoDB.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDB.Modules.Labels;

[DependsOn(typeof(LabelsFeature))]
public class MongoLabelPersistenceFeature : PersistenceFeatureBase
{
    public MongoLabelPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.UseLabels(labels =>
        {
            labels.LabelStore = sp => sp.GetRequiredService<MongoLabelStore>();
            labels.WorkflowDefinitionLabelStore = sp => sp.GetRequiredService<MongoWorkflowDefinitionLabelStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<Label, MongoLabelStore>();
        AddStore<WorkflowDefinitionLabel, MongoWorkflowDefinitionLabelStore>();
    }
}