using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Features;
using Elsa.MongoDb.Common;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MongoDb.Modules.Runtime;

/// <summary>
/// Configures the default workflow runtime to use MongoDB persistence providers.
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
        Module.Configure<KeyValueFeature>(feature =>
        {
            feature.KeyValueStore = sp => sp.GetRequiredService<MongoKeyValueStore>();
        });
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.TriggerStore = sp => sp.GetRequiredService<MongoTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<MongoBookmarkStore>();
            feature.BookmarkQueueStore = sp => sp.GetRequiredService<MongoBookmarkQueueStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<MongoWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<MongoActivityExecutionLogStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<StoredTrigger>("triggers");
        AddCollection<StoredBookmark>("bookmarks");
        AddCollection<BookmarkQueueItem>("bookmark_queue_items");
        AddCollection<WorkflowExecutionLogRecord>("workflow_execution_logs");
        AddCollection<ActivityExecutionRecord>("activity_execution_logs");
        AddCollection<SerializedKeyValuePair>("key_value_pairs");
        
        AddStore<StoredTrigger, MongoTriggerStore>();
        AddStore<StoredBookmark, MongoBookmarkStore>();
        AddStore<BookmarkQueueItem, MongoBookmarkQueueStore>();
        AddStore<WorkflowExecutionLogRecord, MongoWorkflowExecutionLogStore>();
        AddStore<ActivityExecutionRecord, MongoActivityExecutionLogStore>();
        AddStore<SerializedKeyValuePair, MongoKeyValueStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}