using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.MassTransit.Consumers;
using Elsa.MassTransit.Implementations;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.MassTransit.Features;

/// <summary>
/// Enables the <see cref="MassTransitFeature"/> feature.
/// </summary>
public class MassTransitFeature : FeatureBase
{
    /// <inheritdoc />
    public MassTransitFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate that can be set to configure MassTransit's <see cref="IBusRegistrationConfigurator"/>. 
    /// </summary>
    public Action<IBusRegistrationConfigurator>? BusConfigurator { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        BusConfigurator ??= configure =>
        {
            configure.UsingInMemory((context, configurator) => { configurator.ConfigureEndpoints(context); });
        };
        
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddActivityProvider<MassTransitActivityTypeProvider>();
        AddMassTransit(BusConfigurator);

        // Add collected message types to options.
        Services.Configure<MassTransitActivityOptions>(options => options.MessageTypes = new HashSet<Type>(this.GetMessages()));
        
        // Configure message serializer.
        SystemTextJsonMessageSerializer.Options.Converters.Add(new TypeJsonConverter(new WellKnownTypeRegistry()));
    }
    
    /// <summary>
    /// Adds MassTransit to the service container and registers all collected assemblies for discovery of consumers.
    /// </summary>
    private void AddMassTransit(Action<IBusRegistrationConfigurator>? config)
    {
        // For each message type, create a concrete WorkflowMessageConsumer<T>.
        var workflowMessageConsumerType = typeof(WorkflowMessageConsumer<>);
        var workflowMessageConsumers = this.GetMessages().Select(x => workflowMessageConsumerType.MakeGenericType(x));

        // Concatenate the manually registered consumers with the workflow message consumers.
        var consumerTypes = this.GetConsumers().Concat(workflowMessageConsumers).ToArray();

        Services.AddMassTransit(bus =>
        {
            bus.SetKebabCaseEndpointNameFormatter();
            bus.AddConsumers(consumerTypes);

            config?.Invoke(bus);
        });
        
        Services.AddOptions<MassTransitHostOptions>().Configure(options =>
        {
            // Wait until the bus is started before returning from IHostedService.StartAsync.
            options.WaitUntilStarted = true;
        });
    }
}