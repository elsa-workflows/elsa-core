using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Modules.Runtime;

/// <summary>
/// Configures the <see cref="WorkflowRuntimeFeature"/> feature with Elasticsearch persistence. 
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(ElasticsearchFeature))]
public class ElasticExecutionLogRecordFeature : ElasticPersistenceFeatureBase
{
    /// <inheritdoc />
    public ElasticExecutionLogRecordFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<ElasticWorkflowExecutionLogStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowExecutionLogRecord, ElasticWorkflowExecutionLogStore>();
    }
}