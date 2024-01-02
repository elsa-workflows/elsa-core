using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.ConsumerDefinitions;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Implementations;
using Elsa.MassTransit.Messages;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using MassTransit;
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
    
    /// <summary>
    /// Configures the MassTransit workflow dispatcher.
    /// </summary>
    public Action<MassTransitWorkflowDispatcherOptions>? ConfigureDispatcherOptions { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddMassTransitConsumer<DispatchWorkflowRequestConsumer, DispatchWorkflowRequestConsumerDefinition>();
        Module.Configure<WorkflowRuntimeFeature>(f => f.WorkflowDispatcher = sp => sp.GetRequiredService<MassTransitWorkflowDispatcher>());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var options = Services.AddOptions<MassTransitWorkflowDispatcherOptions>();
        
        if (ConfigureDispatcherOptions != null)
            options.Configure(ConfigureDispatcherOptions);
        
        var queueName = KebabCaseEndpointNameFormatter.Instance.Consumer<DispatchWorkflowRequestConsumer>();
        var queueAddress = new Uri($"queue:elsa-{queueName}");
        EndpointConvention.Map<DispatchWorkflowDefinition>(queueAddress);
        EndpointConvention.Map<DispatchWorkflowInstance>(queueAddress);
        EndpointConvention.Map<DispatchTriggerWorkflows>(queueAddress);
        EndpointConvention.Map<DispatchResumeWorkflows>(queueAddress);
        
        Services.AddScoped<MassTransitWorkflowDispatcher>();
    }
}