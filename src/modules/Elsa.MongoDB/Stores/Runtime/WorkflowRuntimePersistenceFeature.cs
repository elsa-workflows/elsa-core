using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MongoDB.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDB.Stores.Runtime;

/// <summary>
/// Configures the default workflow runtime to use MongoDb persistence providers.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class MongoWorkflowRuntimePersistenceFeature : PersistenceFeatureBase
{
    /// <inheritdoc />
    public MongoWorkflowRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowTriggerStore = sp => sp.GetRequiredService<MongoTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<MongoBookmarkStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddStore<StoredTrigger, MongoTriggerStore>();
        AddStore<StoredBookmark, MongoBookmarkStore>();
    }
}