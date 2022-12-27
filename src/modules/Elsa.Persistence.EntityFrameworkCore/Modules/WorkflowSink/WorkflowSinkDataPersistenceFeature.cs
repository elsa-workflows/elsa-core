using Elsa.Extensions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Common;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Features;
using Elsa.Workflows.Sink.Implementations;
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
        base.Configure();
        Module.AddMassTransitServiceBusConsumerType(typeof(ExportWorkflowSinkToSqlDb));
    }

    public override void Apply()
    {
        base.Apply();

        Services
            .AddSingleton<IEFCoreWorkflowSink, EFCoreWorkflowSink>()
            .AddCommandHandler<ExportWorkflowSinkToSqlDb, ExportWorkflowSinkMessage>();

        AddStore<Workflows.Sink.Models.WorkflowSink, EFCoreWorkflowSink>();
    }
}