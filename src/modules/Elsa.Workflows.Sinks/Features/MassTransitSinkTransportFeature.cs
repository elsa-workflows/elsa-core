using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Implementations;
using Elsa.Workflows.Sinks.Implementations;
using Elsa.Workflows.Sinks.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sinks.Features;

[DependsOn(typeof(WorkflowSinkFeature))]
public class MassTransitSinkTransportFeature : FeatureBase
{
    public MassTransitSinkTransportFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowSinkFeature>().SinkTransport = sp => ActivatorUtilities.CreateInstance<MassTransitTransport<ExportWorkflowSinkMessage>>(sp);
    }
}