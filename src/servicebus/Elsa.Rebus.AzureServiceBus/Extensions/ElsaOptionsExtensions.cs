using System;
using Elsa.Services.Messaging;
using Microsoft.Azure.ServiceBus.Primitives;
using Rebus.Config;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, default));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, ITokenProvider tokenProvider) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenProvider, default));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, configureTransport));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, ITokenProvider tokenProvider, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenProvider, configureTransport));

        private static void ConfigureAzureServiceBusEndpoint(
            ServiceBusEndpointConfigurationContext context,
            string connectionString,
            ITokenProvider? tokenProvider,
            Action<AzureServiceBusTransportSettings>? configureTransport)
        {
            var queueName = context.QueueName;

            context.Configurer
                .Transport(t =>
                {
                    var transport = t.UseAzureServiceBus(connectionString, queueName, tokenProvider);
                    configureTransport?.Invoke(transport);
                });
        }
    }
}