using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Features;
using Elsa.Workflows.Sinks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

[DependsOn(typeof(WorkflowSinkFeature))]
public class EFCoreWorkflowSinkPersistenceFeature : PersistenceFeatureBase<WorkflowSinkElsaDbContext>
{
    public EFCoreWorkflowSinkPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Apply()
    {
        base.Apply();

        Services.AddSingleton<IWorkflowSinkClient, EFCoreWorkflowSinkClient>();
        
        AddStore<WorkflowInstance, EFCoreWorkflowSinkClient>();
    }
}