using Elsa.Elasticsearch.Common;
using Elsa.Elasticsearch.Contracts;
using Elsa.Elasticsearch.Features;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Elasticsearch.Modules.Management;

/// <summary>
/// Configures the <see cref="WorkflowInstancesFeature"/> feature with Elasticsearch.
/// </summary>
[DependsOn(typeof(WorkflowManagementFeature))]
[DependsOn(typeof(ElasticsearchFeature))]
public class ElasticWorkflowInstanceFeature : ElasticPersistenceFeatureBase
{
    /// <inheritdoc />
    public ElasticWorkflowInstanceFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// A delegate that creates an instance of a concrete implementation if <see cref="IIndexConfiguration"/> for <see cref="WorkflowInstance"/>.
    /// </summary>
    public Func<IServiceProvider, IIndexConfiguration<WorkflowInstance>> IndexConfiguration { get; set; } = sp => ActivatorUtilities.CreateInstance<WorkflowInstanceConfiguration>(sp);

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowInstancesFeature>(feature =>
        {
            feature.WorkflowInstanceStore = sp => sp.GetRequiredService<ElasticWorkflowInstanceStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddStore<WorkflowInstance, ElasticWorkflowInstanceStore>();
        AddIndexConfiguration(IndexConfiguration);
    }
}