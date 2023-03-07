using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

/// <summary>
/// A feature that registers the <see cref="EFCoreWorkflowExecutionLogStore"/> as the default <see cref="IWorkflowExecutionLogStore"/>.
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreExecutionLogRecordPersistenceFeature : PersistenceFeatureBase<RuntimeElsaDbContext>
{
    /// <inheritdoc />
    public EFCoreExecutionLogRecordPersistenceFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        base.Apply();
        
        AddEntityStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
    }
}