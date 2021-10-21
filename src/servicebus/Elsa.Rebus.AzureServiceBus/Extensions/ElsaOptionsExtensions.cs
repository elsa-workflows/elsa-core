using System;
using Azure.Core;
using Elsa.Options;
using Elsa.Services.Messaging;
using Rebus.Config;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, default));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, TokenCredential tokenCredential) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenCredential, default));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, configureTransport));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, TokenCredential tokenCredential, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenCredential, configureTransport));

        private static void ConfigureAzureServiceBusEndpoint(
            ServiceBusEndpointConfigurationContext context,
            string connectionString,
            TokenCredential? tokenProvider,
            Action<AzureServiceBusTransportSettings>? configureTransport)
        {
            var queueName = context.QueueName;

            context.Configurer
                .Transport(t =>
                {
                    if (queueName.Length > 50)
                        queueName = queueName.Substring(queueName.Length - 50);
                    
                    var transport = t.UseAzureServiceBus(connectionString, queueName, tokenProvider);
                    configureTransport?.Invoke(transport);
                });
        }
    }
}