using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreRuntimePersistenceFeature : PersistenceFeatureBase<RuntimeDbContext>
{
    public EFCoreRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowStateStore = sp => sp.GetRequiredService<EFCoreWorkflowStateStore>();
            feature.WorkflowTriggerStore = sp => sp.GetRequiredService<EFCoreTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<EFCoreBookmarkStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowState, EFCoreWorkflowStateStore>();
        AddStore<StoredTrigger, EFCoreTriggerStore>();
        AddStore<StoredBookmark, EFCoreBookmarkStore>();
        AddStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
    }
}