using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.ConsumerDefinitions;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Contracts;
using Elsa.MassTransit.Formatters;
using Elsa.MassTransit.Options;
using Elsa.MassTransit.Services;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Features;
using Elsa.Workflows.Runtime.Services;
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

    /// <summary>
    /// A factory that creates a <see cref="IEndpointChannelFormatter"/>.
    /// </summary>
    public Func<IServiceProvider, IEndpointChannelFormatter> ChannelQueueFormatterFactory { get; set; } = _ => new DefaultEndpointChannelFormatter();

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddMassTransitConsumer<DispatchWorkflowRequestConsumer, DispatchWorkflowRequestConsumerDefinition>();
        Module.Configure<WorkflowRuntimeFeature>(f => f.WorkflowDispatcher = sp =>
        {
            var decoratedService = ActivatorUtilities.CreateInstance<MassTransitWorkflowDispatcher>(sp);
            return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, decoratedService);
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var options = Services.AddOptions<MassTransitWorkflowDispatcherOptions>();
        
        if (ConfigureDispatcherOptions != null)
            options.Configure(ConfigureDispatcherOptions);
        
        Services.AddSingleton(ChannelQueueFormatterFactory);
    }
}