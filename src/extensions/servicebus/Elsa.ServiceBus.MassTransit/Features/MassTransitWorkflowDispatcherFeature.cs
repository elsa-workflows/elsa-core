using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.ServiceBus.MassTransit.ConsumerDefinitions;
using Elsa.ServiceBus.MassTransit.Consumers;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.Services;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ServiceBus.MassTransit.Features;

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
    [Obsolete("Use ConfigureWorkflowDispatcherOptions instead.")]
    public Action<MassTransitWorkflowDispatcherOptions> ConfigureDispatcherOptions
    {
        get => ConfigureWorkflowDispatcherOptions; 
        set => ConfigureWorkflowDispatcherOptions = value;
    }
    
    /// <summary>
    /// Configures the MassTransit workflow dispatcher.
    /// </summary>
    public Action<MassTransitWorkflowDispatcherOptions> ConfigureWorkflowDispatcherOptions { get; set; } = _ => { };
    
    /// <summary>
    /// Configures the MassTransit stimulus dispatcher.
    /// </summary>
    public Action<MassTransitStimulusDispatcherOptions> ConfigureStimulusDispatcherOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddMassTransitConsumer<DispatchWorkflowRequestConsumer, DispatchWorkflowRequestConsumerDefinition>();
        Module.AddMassTransitConsumer<DispatchCancelWorkflowsRequestConsumer>("elsa-wf-cancel", true);
        Module.AddMassTransitConsumer<DispatchStimulusRequestConsumer, DispatchStimulusRequestConsumerDefinition>("elsa-dispatch-stimulus");
        Module.Configure<WorkflowRuntimeFeature>(f =>
        {
            // Same decorator shape as core: Validating → Transactional → MassTransit.
            f.WorkflowDispatcher = sp =>
            {
                var decoratedService = ActivatorUtilities.CreateInstance<MassTransitWorkflowDispatcher>(sp);
                var transactionalService = ActivatorUtilities.CreateInstance<TransactionalWorkflowDispatcher>(sp, decoratedService);
                return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, transactionalService);
            };

            f.WorkflowCancellationDispatcher = sp => sp.GetRequiredService<MassTransitWorkflowCancellationDispatcher>();
            f.StimulusDispatcher = sp => sp.GetRequiredService<MassTransitStimulusDispatcher>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(ConfigureWorkflowDispatcherOptions);
        Services.Configure(ConfigureStimulusDispatcherOptions);
        
        Services.AddScoped<MassTransitWorkflowCancellationDispatcher>();
        Services.AddScoped<MassTransitStimulusDispatcher>();
    }
}