using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Options;
using MassTransit;

namespace Elsa.MassTransit.AzureServiceBus.Features;

/// <summary>
/// Configures MassTransit to use the Azure Service Bus transport.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
public class AzureServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public AzureServiceBusFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// An Azure Service Bus connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransitFeature =>
        {
            massTransitFeature.BusConfigurator = configure =>
            {
                configure.AddServiceBusMessageScheduler();
                
                configure.UsingAzureServiceBus((context, configurator) =>
                {
                    if (ConnectionString != null) 
                        configurator.Host(ConnectionString);
                    
                    configurator.UseServiceBusMessageScheduler();
                    configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                });
            };
        });
    }
}