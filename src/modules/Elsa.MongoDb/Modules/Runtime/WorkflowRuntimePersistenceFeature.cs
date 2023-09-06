using Elsa.Features.Attributes;
using Elsa.Features.Services;
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
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.TriggerStore = sp => sp.GetRequiredService<MongoTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<MongoBookmarkStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<MongoWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<MongoActivityExecutionLogStore>();
            feature.WorkflowInboxStore = sp => sp.GetRequiredService<MongoWorkflowInboxMessageStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddCollection<StoredTrigger>("triggers");
        AddCollection<StoredBookmark>("bookmarks");
        AddCollection<WorkflowExecutionLogRecord>("workflow_execution_logs");
        AddCollection<ActivityExecutionRecord>("activity_execution_logs");
        AddCollection<WorkflowInboxMessage>("workflow_inbox_messages");
        
        AddStore<StoredTrigger, MongoTriggerStore>();
        AddStore<StoredBookmark, MongoBookmarkStore>();
        AddStore<WorkflowExecutionLogRecord, MongoWorkflowExecutionLogStore>();
        AddStore<ActivityExecutionRecord, MongoActivityExecutionLogStore>();
        AddStore<WorkflowInboxMessage, MongoWorkflowInboxMessageStore>();
        
        Services.AddHostedService<CreateIndices>();
    }
}