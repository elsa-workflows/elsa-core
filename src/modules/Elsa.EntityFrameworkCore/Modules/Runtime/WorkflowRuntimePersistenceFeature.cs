using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.KeyValues.Entities;
using Elsa.KeyValues.Features;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreWorkflowRuntimePersistenceFeature(IModule module) : PersistenceFeatureBase<EFCoreWorkflowRuntimePersistenceFeature, RuntimeElsaDbContext>(module)
{
    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<KeyValueFeature>(feature =>
        {
            feature.KeyValueStore = sp => sp.GetRequiredService<EFCoreKeyValueStore>();
        });
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.TriggerStore = sp => sp.GetRequiredService<EFCoreTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<EFCoreBookmarkStore>();
            feature.BookmarkQueueStore = sp => sp.GetRequiredService<EFBookmarkQueueStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<EFCoreActivityExecutionStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        AddEntityStore<StoredTrigger, EFCoreTriggerStore>();
        AddStore<StoredBookmark, EFCoreBookmarkStore>();
        AddStore<BookmarkQueueItem, EFBookmarkQueueStore>();
        AddEntityStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
        AddEntityStore<ActivityExecutionRecord, EFCoreActivityExecutionStore>();
        AddStore<SerializedKeyValuePair, EFCoreKeyValueStore>();
        Services.AddScoped<IEntityModelCreatingHandler, SetupForOracle>();
    }
}