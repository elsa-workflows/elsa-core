using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// Configures the default workflow runtime to use EF Core persistence providers.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreWorkflowRuntimePersistenceFeature : PersistenceFeatureBase<RuntimeElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreWorkflowRuntimePersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowTriggerStore = sp => sp.GetRequiredService<EFCoreTriggerStore>();
            feature.BookmarkStore = sp => sp.GetRequiredService<EFCoreBookmarkStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddEntityStore<StoredTrigger, EFCoreTriggerStore>();
        AddStore<StoredBookmark, EFCoreBookmarkStore>();
    }
}