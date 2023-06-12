using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDB.Common;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDB.Modules.Runtime;

/// <summary>
/// Configures the default workflow runtime to use MongoDb persistence providers.
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

        AddStore<WorkflowState, MongoWorkflowStateStore>();
        AddStore<StoredTrigger, MongoTriggerStore>();
        AddStore<StoredBookmark, MongoBookmarkStore>();
    }
}