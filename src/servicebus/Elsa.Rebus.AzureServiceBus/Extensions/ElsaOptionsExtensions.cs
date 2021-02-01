using System;
using Elsa.Services;
using Microsoft.Azure.ServiceBus.Primitives;
using Rebus.Config;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, default));

        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, ITokenProvider tokenProvider) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenProvider, default));

        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, configureTransport));

        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, ITokenProvider tokenProvider, Action<AzureServiceBusTransportSettings> configureTransport) =>
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