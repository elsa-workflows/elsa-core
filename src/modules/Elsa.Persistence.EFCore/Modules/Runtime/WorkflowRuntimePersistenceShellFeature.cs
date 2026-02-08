using Elsa.KeyValues.Contracts;
using Elsa.KeyValues.Entities;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// Base class for workflow runtime persistence features.
/// This is not a standalone shell feature - use provider-specific features.
/// </summary>
[UsedImplicitly]
public abstract class EFCoreWorkflowRuntimePersistenceShellFeatureBase : PersistenceShellFeatureBase<RuntimeElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        services.AddScoped<ITriggerStore, EFCoreTriggerStore>();
        services.AddScoped<IBookmarkStore, EFCoreBookmarkStore>();
        services.AddScoped<IBookmarkQueueStore, EFBookmarkQueueStore>();
        services.AddScoped<IWorkflowExecutionLogStore, EFCoreWorkflowExecutionLogStore>();
        services.AddScoped<IActivityExecutionStore, EFCoreActivityExecutionStore>();
        services.AddScoped<IKeyValueStore, EFCoreKeyValueStore>();
        
        AddEntityStore<StoredTrigger, EFCoreTriggerStore>(services);
        AddStore<StoredBookmark, EFCoreBookmarkStore>(services);
        AddStore<BookmarkQueueItem, EFBookmarkQueueStore>(services);
        AddEntityStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>(services);
        AddEntityStore<ActivityExecutionRecord, EFCoreActivityExecutionStore>(services);
        AddStore<SerializedKeyValuePair, EFCoreKeyValueStore>(services);
    }
}
