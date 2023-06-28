using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Labels.Entities;
using Elsa.Labels.Features;
using Elsa.MongoDb.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Labels;

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
        
        AddCollection<Label>("labels");
        AddCollection<WorkflowDefinitionLabel>("workflow_definition_labels");

        AddStore<Label, MongoLabelStore>();
        AddStore<WorkflowDefinitionLabel, MongoWorkflowDefinitionLabelStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}