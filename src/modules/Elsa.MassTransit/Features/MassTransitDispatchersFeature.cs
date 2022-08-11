using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Implementations;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Features;

[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(MassTransitServiceBusFeature))]
public class MassTransitDispatchersFeature : FeatureBase
{
    public MassTransitDispatchersFeature(IModule module) : base(module)
    {
    }

    public override void Configure()
    {
        Module.Configure<WorkflowRuntimeFeature>(f => f.WorkflowDispatcherFactory = ActivatorUtilities.GetServiceOrCreateInstance<MassTransitWorkflowDispatcher>);
    }
}