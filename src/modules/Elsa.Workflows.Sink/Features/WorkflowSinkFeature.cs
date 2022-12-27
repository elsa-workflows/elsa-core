using System;
using Elsa.Common.Extensions;
using Elsa.Common.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Mediator.Extensions;
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

    public override void Apply()
    {
        Services
            // Core.
            .AddSingleton<IPrepareWorkflowSinkModel, PrepareWorkflowSinkModel>()
            .AddSingleton(SinkTransport)

            //Handlers.
            .AddNotificationHandlersFrom<WorkflowSinkFeature>();
    }
}