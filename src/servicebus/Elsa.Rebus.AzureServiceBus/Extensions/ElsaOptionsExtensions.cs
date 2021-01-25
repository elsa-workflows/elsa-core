using System;
using Elsa.Services;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Routing.TypeBased;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(elsaOptions, context, connectionString, default, default));
        
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, ITokenProvider tokenProvider) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(elsaOptions, context, connectionString, tokenProvider, default));
        
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(elsaOptions, context, connectionString, default, configureTransport));
        
        public static ElsaOptions UseAzureServiceBus(this ElsaOptions elsaOptions, string connectionString, ITokenProvider tokenProvider, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(elsaOptions, context, connectionString, tokenProvider, configureTransport));

        private static void ConfigureAzureServiceBusEndpoint(
            ElsaOptions elsaOptions,
            ServiceBusEndpointConfigurationContext context,
            string connectionString,
            ITokenProvider? tokenProvider,
            Action<AzureServiceBusTransportSettings>? configureTransport)
        {
            var queueName = context.QueueName;
            var loggerFactory = context.ServiceProvider.GetRequiredService<ILoggerFactory>();

            context.Configurer
                .Logging(l => l.MicrosoftExtensionsLogging(loggerFactory))
                .Transport(t =>
                {
                    var transport = t.UseAzureServiceBus(connectionString, queueName, tokenProvider);
                    configureTransport?.Invoke(transport);
                })
                .Routing(r => r.TypeBased().Map(context.MessageTypeMap))
                .Options(o => o.Apply(elsaOptions.ServiceBusOptions));
        }
    }
}