using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Elsa.Elasticsearch.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Modules.Runtime;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class ElasticExecutionLogRecordFeature : ElasticFeatureBase
{
    public ElasticExecutionLogRecordFeature(IModule module) : base(module)
    {
    }
    
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<ElasticWorkflowExecutionLogStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowExecutionLogRecord, ElasticWorkflowExecutionLogStore>();
    }
}