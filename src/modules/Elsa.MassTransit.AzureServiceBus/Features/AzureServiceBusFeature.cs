using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using MassTransit;

namespace Elsa.MassTransit.AzureServiceBus.Features;

/// Configures MassTransit to use the Azure Service Bus transport.
/// See https://masstransit.io/documentation/configuration/transports/azure-service-bus
[DependsOn(typeof(MassTransitFeature))]
public class AzureServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public AzureServiceBusFeature(IModule module) : base(module)
    {
    }
    
    /// An Azure Service Bus connection string.
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A delegate that configures the Azure Service Bus transport options.
    /// </summary>
    public Action<IServiceBusBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransitFeature =>
        {
            massTransitFeature.BusConfigurator = configure =>
            {
                configure.AddServiceBusMessageScheduler();
                
                configure.UsingAzureServiceBus((context, serviceBus) =>
                {
                    if (ConnectionString != null) 
                        serviceBus.Host(ConnectionString);
                    
                    serviceBus.UseServiceBusMessageScheduler();
                    ConfigureServiceBus?.Invoke(serviceBus);
                    serviceBus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                });
            };
        });
    }
}