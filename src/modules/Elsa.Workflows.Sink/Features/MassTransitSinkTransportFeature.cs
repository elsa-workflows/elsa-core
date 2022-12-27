using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows.Sink.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Sink.Features;

[DependsOn(typeof(WorkflowSinkFeature))]
public class MassTransitSinkTransportFeature : FeatureBase
{
    public MassTransitSinkTransportFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowSinkFeature>().SinkTransport = sp => ActivatorUtilities.CreateInstance<MassTransitSinkTransport>(sp);
    }
}