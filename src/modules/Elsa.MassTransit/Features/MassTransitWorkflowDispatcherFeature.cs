using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.ConsumerDefinitions;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Options;
using Elsa.MassTransit.Services;
using Elsa.Workflows.Runtime;
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
    
    /// <summary>
    /// Configures the MassTransit workflow dispatcher.
    /// </summary>
    public Action<MassTransitWorkflowDispatcherOptions>? ConfigureDispatcherOptions { get; set; }
    

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddMassTransitConsumer<DispatchWorkflowRequestConsumer, DispatchWorkflowRequestConsumerDefinition>();
        Module.AddMassTransitConsumer<DispatchCancelWorkflowsRequestConsumer>("elsa-dispatch-cancel-workflow", true);
        Module.Configure<WorkflowRuntimeFeature>(f =>
        {
            f.WorkflowDispatcher = sp =>
            {
                var decoratedService = ActivatorUtilities.CreateInstance<MassTransitWorkflowDispatcher>(sp);
                return ActivatorUtilities.CreateInstance<ValidatingWorkflowDispatcher>(sp, decoratedService);
            };

            f.WorkflowCancellationDispatcher = sp => sp.GetRequiredService<MassTransitWorkflowCancellationDispatcher>();
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        var options = Services.AddOptions<MassTransitWorkflowDispatcherOptions>();
        
        if (ConfigureDispatcherOptions != null)
            options.Configure(ConfigureDispatcherOptions);
        
        Services.AddScoped<MassTransitWorkflowCancellationDispatcher>();
    }
}