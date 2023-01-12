using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstanceFeature"/> feature with Elasticsearch.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(ElasticsearchFeature))]
public class ElasticWorkflowInstanceFeature : ElasticPersistenceFeatureBase
{
    /// <inheritdoc />
    public ElasticWorkflowInstanceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstanceFeature>(feature =>
        {
            feature.WorkflowInstanceStore = sp => sp.GetRequiredService<ElasticWorkflowInstanceStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowInstance, ElasticWorkflowInstanceStore>();
    }
}