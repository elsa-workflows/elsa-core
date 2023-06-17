using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// Configures the default workflow runtime to use MongoDB persistence providers.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(DefaultWorkflowRuntimeFeature))]
public class MongoDefaultWorkflowRuntimePersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoDefaultWorkflowRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.TriggerStore = sp => sp.GetRequiredService<MongoTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<MongoBookmarkStore>();
        });
        
        Module.Configure<DefaultWorkflowRuntimeFeature>(feature => { feature.WorkflowStateStore = sp => sp.GetRequiredService<MongoWorkflowStateStore>(); });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<WorkflowState>("workflow_states");

        AddStore<WorkflowState, MongoWorkflowStateStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}