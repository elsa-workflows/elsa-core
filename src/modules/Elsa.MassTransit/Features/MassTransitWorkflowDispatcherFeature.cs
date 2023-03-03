using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Implementations;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Features;

/// <summary>
/// Configures the system to use a MassTransit implementation of <see cref="IWorkflowDispatcher"/>; 
/// </summary>
[DependsOn(typeof(WorkflowRuntimeFeature))]
[DependsOn(typeof(MassTransitFeature))]
public class MassTransitWorkflowDispatcherFeature : FeatureBase
{
    /// <inheritdoc />
    public MassTransitWorkflowDispatcherFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransit => massTransit.AddConsumer<DispatchWorkflowRequestConsumer>());
        Module.Configure<WorkflowRuntimeFeature>(f => f.WorkflowDispatcher = sp => sp.GetRequiredService<MassTransitWorkflowDispatcher>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSingleton<MassTransitWorkflowDispatcher>();
    }
}