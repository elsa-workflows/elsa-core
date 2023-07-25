using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// A feature that registers the <see cref="EFCoreWorkflowExecutionLogStore"/> and the <see cref="EFCoreActivityExecutionStore"/>.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreExecutionLogPersistenceFeature : PersistenceFeatureBase<RuntimeElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreExecutionLogPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
            feature.ActivityExecutionLogStore = sp => sp.GetRequiredService<EFCoreActivityExecutionStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddEntityStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
        AddEntityStore<ActivityExecutionRecord, EFCoreActivityExecutionStore>();
    }
}