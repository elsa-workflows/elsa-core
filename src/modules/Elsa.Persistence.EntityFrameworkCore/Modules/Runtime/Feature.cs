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
            feature.WorkflowTriggerStore = sp => sp.GetRequiredService<EFCoreWorkflowTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<EFCoreBookmarkStore>();
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddStore<WorkflowState, EFCoreWorkflowStateStore>();
        AddStore<WorkflowTrigger, EFCoreWorkflowTriggerStore>();
        AddStore<StoredBookmark, EFCoreBookmarkStore>();
        AddStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
    }
}