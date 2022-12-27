using Elsa.Common.Extensions;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Implementations;
using Elsa.Workflows.Sink.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sink.Features;

public class InMemoryWorkflowSinkPersistenceFeature : FeatureBase
{
    public InMemoryWorkflowSinkPersistenceFeature(IModule module) : base(module)
    {
    }
    
    public override void Configure()
    {
        base.Configure();
        Module.AddMassTransitServiceBusConsumerType(typeof(ExportWorkflowSinkToMemory));
    }

    public override void Apply()
    {
        base.Apply();
        
        Services
            .AddSingleton<IMemoryWorkflowSink, MemoryWorkflowSink>()
            .AddMemoryStore<WorkflowSinkDto, MemoryWorkflowSink>()
            .AddCommandHandler<ExportWorkflowSinkToMemory, ExportWorkflowSinkMessage>();
    }
}