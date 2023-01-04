using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Workflows.Core.Serialization.Converters;
using MassTransit;
using MassTransit.Serialization;
using MassTransit.Serialization.JsonConverters;
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
        //ConfigureMessageSerializer();
        AddMassTransit(BusConfigurator);
    }
    
    /// <summary>
    /// Adds MassTransit to the service container and registers all collected assemblies for discovery of consumers.
    /// </summary>
    private void AddMassTransit(Action<IBusRegistrationConfigurator>? config)
    {
        var consumerTypes = this.GetConsumers().ToArray();

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
    
    private static void ConfigureMessageSerializer()
    {
        SystemTextJsonMessageSerializer.Options.Converters.Add(new PolymorphicConverter());
    }
}