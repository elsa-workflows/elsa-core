using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.Features.Services;
using Elsa.ServiceBus.MassTransit.AzureServiceBus.Features;
using Elsa.ServiceBus.MassTransit.Features;
using JetBrains.Annotations;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit and the Azure Service Bus transport.
/// </summary>
[PublicAPI]
public static class ModuleExtensions
{
    extension(MassTransitFeature feature)
    {
        /// <summary>
        /// Enable and configure the Azure Service Bus transport for MassTransit.
        /// </summary>
        /// <param name="connectionString">The Azure Service Bus connection string or name.</param>
        /// <param name="configure">An optional action to further configure the Azure Service Bus feature.</param>
        /// <returns>The <see cref="MassTransitFeature"/> for method chaining.</returns>
        public MassTransitFeature UseAzureServiceBus(string? connectionString, Action<AzureServiceBusFeature>? configure = null)
        {
            feature.Module.Configure((Action<AzureServiceBusFeature>)Configure);
            return feature;

            void Configure(AzureServiceBusFeature bus)
            {
                bus.AzureServiceBusOptions += options => options.ConnectionStringOrName = connectionString;
                bus.ConnectionString = connectionString;
                configure?.Invoke(bus);
            }
        }
        
        /// <summary>
        /// Enable and configure the Azure Service Bus transport for MassTransit.
        /// </summary>
        /// <param name="hostAddress">The host address, in MassTransit format (sb://namespace.servicebus.windows.net/scope).</param>
        /// <param name="serviceBusClient">The Azure Service Bus client instance.</param>
        /// <param name="serviceBusAdministrationClient">The Azure Service Bus administration client instance.</param>
        /// <param name="configure">An optional action to further configure the Azure Service Bus feature.</param>
        /// <returns>The <see cref="MassTransitFeature"/> for method chaining.</returns>
        public MassTransitFeature UseAzureServiceBus(
            Uri hostAddress, 
            ServiceBusClient serviceBusClient, 
            ServiceBusAdministrationClient serviceBusAdministrationClient, 
            Action<AzureServiceBusFeature>? configure = null)
        {
            feature.Module.Configure((Action<AzureServiceBusFeature>)Configure);
            return feature;

            void Configure(AzureServiceBusFeature bus)
            {
                // Don't concatenate factory delegates, because that would cause multiple creations of the client.
                bus.ServiceBusAdministrationClientFactory = _ => serviceBusAdministrationClient;
                
                // But it's OK to concatenate multiple configuration delegates.
                bus.ConfigureTransportBus +=  (_, configurator) => configurator.Host(hostAddress, serviceBusClient, serviceBusAdministrationClient);
                configure?.Invoke(bus);
            }
        }

        /// <summary>
        /// Enable and configure the Azure Service Bus transport for MassTransit.
        /// </summary>
        /// <param name="configure">An action to configure the Azure Service Bus feature.</param>
        /// <returns>The <see cref="MassTransitFeature"/> for method chaining.</returns>
        public MassTransitFeature UseAzureServiceBus(Action<AzureServiceBusFeature>? configure)
        {
            feature.Module.Configure(configure);
            return feature;
        }
    }
}