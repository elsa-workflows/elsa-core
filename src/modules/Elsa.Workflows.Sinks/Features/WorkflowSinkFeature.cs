using Elsa.Common.Features;
using Elsa.Common.Services;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Extensions;
using Elsa.Mediator.Extensions;
using Elsa.Mediator.Implementations;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Sinks.Contracts;
using Elsa.Workflows.Sinks.Implementations;
using Elsa.Workflows.Sinks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sinks.Features;

[DependsOn(typeof(SystemClockFeature))]
public class WorkflowSinkFeature : FeatureBase
{
    public WorkflowSinkFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A factory that instantiates a concrete <see cref="ISinkTransport"/>.
    /// </summary>
    public Func<IServiceProvider, ITransport<ExportWorkflowSinkMessage>> SinkTransport { get; set; } = sp => ActivatorUtilities.CreateInstance<InProcessTransport<ExportWorkflowSinkMessage>>(sp);

    public override void Configure()
    {
        Module.AddMassTransitServiceBusConsumerType(typeof(ExportWorkflowSink));
    }

    public override void Apply()
    {
        Services
            // Core.
            .AddSingleton(SinkTransport)
            .AddSingleton<IPrepareWorkflowInstance, PrepareWorkflowInstance>()

            //Handlers.
            .AddNotificationHandler<WorkflowExecutedNotificationHandler, WorkflowExecuted>()
            .AddNotificationHandler<ExportWorkflowSink, ExportWorkflowSinkMessage>();
    }
}