using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreDefaultRuntimePersistenceFeature : PersistenceFeatureBase<RuntimeElsaDbContext>
{
    public EFCoreDefaultRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowStateStore = sp => sp.GetRequiredService<EFCoreWorkflowStateStore>();
            feature.WorkflowTriggerStore = sp => sp.GetRequiredService<EFCoreTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<EFCoreBookmarkStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();

        AddEntityStore<WorkflowState, EFCoreWorkflowStateStore>();
        AddEntityStore<StoredTrigger, EFCoreTriggerStore>();
        AddStore<StoredBookmark, EFCoreBookmarkStore>();
    }
}