using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Features;
using Elsa.Workflows.Sink.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;

[DependsOn(typeof(WorkflowSinkFeature))]
public class EFCoreWorkflowSinkPersistenceFeature : PersistenceFeatureBase<WorkflowSinkElsaDbContext>
{
    public EFCoreWorkflowSinkPersistenceFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowSinkFeature>().WorkflowSinkClient = default;
    }

    public override void Apply()
    {
        base.Apply();

        Services.AddSingleton<IWorkflowSinkClient, EFCoreWorkflowSinkClient>();
        
        AddStore<WorkflowSinkEntity, EFCoreWorkflowSinkClient>();
    }
}