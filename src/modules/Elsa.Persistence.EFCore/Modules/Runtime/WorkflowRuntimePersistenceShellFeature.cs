using CShells.Features;
using Elsa.KeyValues.Entities;
using Elsa.Workflows.Runtime.Entities;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EFCore.Modules.Runtime;

/// <summary>
/// Configures the workflow runtime feature with Entity Framework Core persistence providers.
/// </summary>
[ShellFeature(
    DisplayName = "EF Core Workflow Runtime Persistence",
    Description = "Provides Entity Framework Core persistence for workflow runtime",
    DependsOn = ["WorkflowRuntime"])]
[UsedImplicitly]
public class EFCoreWorkflowRuntimePersistenceShellFeature : PersistenceShellFeatureBase<EFCoreWorkflowRuntimePersistenceShellFeature, RuntimeElsaDbContext>
{
    protected override void OnConfiguring(IServiceCollection services)
    {
        AddEntityStore<StoredTrigger, EFCoreTriggerStore>(services);
        AddStore<StoredBookmark, EFCoreBookmarkStore>(services);
        AddStore<BookmarkQueueItem, EFBookmarkQueueStore>(services);
        AddEntityStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>(services);
        AddEntityStore<ActivityExecutionRecord, EFCoreActivityExecutionStore>(services);
        AddStore<SerializedKeyValuePair, EFCoreKeyValueStore>(services);
    }
}
