using Elsa.Elasticsearch.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Modules.Management;

[DependsOn(typeof(WorkflowManagementFeature))]
public class ElasticWorkflowInstanceFeature : ElasticFeatureBase
{
    public ElasticWorkflowInstanceFeature(IModule module) : base(module)
    {
    }
    
    public override void Configure()
    {
        Module.Configure<WorkflowManagementFeature>(feature =>
        {
            feature.WorkflowInstanceStore = sp => sp.GetRequiredService<ElasticWorkflowInstanceStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowInstance, ElasticWorkflowInstanceStore>();
    }
}