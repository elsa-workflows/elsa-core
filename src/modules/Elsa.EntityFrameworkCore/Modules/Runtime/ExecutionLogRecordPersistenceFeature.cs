using Elsa.EntityFrameworkCore.Common;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.EntityFrameworkCore.Modules.Runtime;

[DependsOn(typeof(WorkflowRuntimeFeature))]
public class EFCoreExecutionLogRecordPersistenceFeature : PersistenceFeatureBase<RuntimeElsaDbContext>
{
    public EFCoreExecutionLogRecordPersistenceFeature(IModule module) : base(module)
    {
    }
    
    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(feature =>
        {
            feature.WorkflowExecutionLogStore = sp => sp.GetRequiredService<EFCoreWorkflowExecutionLogStore>();
        });
    }

    public override void Apply()
    {
        base.Apply();
        
        AddStore<WorkflowExecutionLogRecord, EFCoreWorkflowExecutionLogStore>();
    }
}