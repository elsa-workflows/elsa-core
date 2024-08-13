using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Handlers;
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
public class EFCoreWorkflowRuntimePersistenceFeature(IModule module) : PersistenceFeatureBase<RuntimeElsaDbContext>(module)
{
    /// Delegate for determining the exception handler.
    public Func<IServiceProvider, IDbExceptionHandler<RuntimeElsaDbContext>> DbExceptionHandler { get; set; } = _ => new RethrowDbExceptionHandler(); 

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
            feature.WorkflowInboxStore = sp => sp.GetRequiredService<EFCoreWorkflowInboxMessageStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<EFCoreActivityExecutionStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        Services.AddScoped(DbExceptionHandler);

        AddEntityStore<StoredTrigger, EFCoreTriggerStore>();
        AddStore<StoredBookmark, EFCoreBookmarkStore>();
        AddEntityStore<WorkflowInboxMessage, EFCoreWorkflowInboxMessageStore>();
        AddEntityStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
        AddEntityStore<ActivityExecutionRecord, EFCoreActivityExecutionStore>();
        AddStore<SerializedKeyValuePair, EFCoreKeyValueStore>();
    }
}