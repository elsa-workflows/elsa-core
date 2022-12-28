using System;
using Elsa.Common.Extensions;
using Elsa.Common.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Extensions;
using Elsa.Mediator.Extensions;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Sink.Contracts;
using Elsa.Workflows.Sink.Implementations;
using Elsa.Workflows.Sink.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sink.Features;

[DependsOn(typeof(SystemClockFeature))]
public class WorkflowSinkFeature : FeatureBase
{
    public WorkflowSinkFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates a concrete <see cref="ISinkTransport"/>.
    /// </summary>
    public Func<IServiceProvider, ISinkTransport> SinkTransport { get; set; } = sp => ActivatorUtilities.CreateInstance<InProcessSinkTransport>(sp);
    
    /// <summary>
    /// A factory that instantiates a concrete <see cref="IWorkflowSinkManager"/>.
    /// </summary>
    public Func<IServiceProvider, IWorkflowSinkManager>? WorkflowSinkManager { get; set; } = sp => ActivatorUtilities.CreateInstance<MemoryWorkflowSinkManager>(sp);

    public override void Configure()
    {
        Module.AddMassTransitServiceBusConsumerType(typeof(ExportWorkflowSink));
    }

    public override void Apply()
    {
        if (WorkflowSinkManager != default)
        {
            Services.AddSingleton(WorkflowSinkManager);
        }
        
        Services
            // Core.
            .AddSingleton(SinkTransport)
            .AddSingleton<IPrepareWorkflowSinkModel, PrepareWorkflowSinkModel>()
            .AddMemoryStore<WorkflowSinkDto, MemoryWorkflowSinkManager>()

            //Handlers.
            .AddNotificationHandler<WorkflowExecutedNotificationHandler, WorkflowExecuted>()
            .AddNotificationHandler<ExportWorkflowSink, ExportWorkflowSinkMessage>();
    }
}