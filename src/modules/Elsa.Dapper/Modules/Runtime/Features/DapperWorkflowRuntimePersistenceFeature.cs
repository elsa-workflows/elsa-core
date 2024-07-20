using Elsa.Dapper.Extensions;
using Elsa.Dapper.Features;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Modules.Runtime.Stores;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.KeyValues.Features;
using Elsa.Workflows.Runtime.Features;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dapper.Modules.Runtime.Features;

/// <summary>
/// Configures the default workflow runtime to use Dapper persistence providers.
/// </summary>
/// <inheritdoc />
[DependsOn(typeof(DapperFeature))]
[PublicAPI]
public class DapperWorkflowRuntimePersistenceFeature(IModule module) : FeatureBase(module)
{

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<KeyValueFeature>(feature =>
        {
            feature.KeyValueStore = sp => sp.GetRequiredService<DapperKeyValueStore>();
        });
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.TriggerStore = sp => sp.GetRequiredService<DapperTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<DapperBookmarkStore>();
            feature.BookmarkQueueStore = sp => sp.GetRequiredService<DapperBookmarkQueueStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<DapperWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<DapperActivityExecutionRecordStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();

        Services.AddDapperStore<DapperTriggerStore, StoredTriggerRecord>("Triggers");
        Services.AddDapperStore<DapperBookmarkStore, StoredBookmarkRecord>("Bookmarks");
        Services.AddDapperStore<DapperBookmarkQueueStore, BookmarkQueueItemRecord>("BookmarkQueueItems");
        Services.AddDapperStore<DapperWorkflowExecutionLogStore, WorkflowExecutionLogRecordRecord>("WorkflowExecutionLogRecords");
        Services.AddDapperStore<DapperActivityExecutionRecordStore, ActivityExecutionRecordRecord>("ActivityExecutionRecords");
        Services.AddDapperStore<DapperKeyValueStore, KeyValuePairRecord>("KeyValues");
    }
}