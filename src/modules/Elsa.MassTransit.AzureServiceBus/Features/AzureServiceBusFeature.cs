using Azure.Messaging.ServiceBus.Administration;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.MassTransit.Features;
using Elsa.MassTransit.Messages;
using Elsa.MassTransit.Options;
using Elsa.Workflows.Runtime.Activities;
using MassTransit;
using MassTransit.Configuration;

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
                
                configure.UsingAzureServiceBus((context, serviceBus) =>
                {
                    if (ConnectionString != null) 
                        serviceBus.Host(ConnectionString);
                    
                    serviceBus.UseServiceBusMessageScheduler();
                    serviceBus.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                });
            };
        });
    }
}