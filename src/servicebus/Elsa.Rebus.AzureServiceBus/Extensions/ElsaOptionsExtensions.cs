using System;
using Azure.Core;
using Elsa.Options;
using Elsa.Rebus.AzureServiceBus.StartupTasks;
using Elsa.Runtime;
using Elsa.Services;
using Elsa.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Config;

namespace Elsa.Rebus.AzureServiceBus
{
    public static class ElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, default));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, TokenCredential tokenCredential) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenCredential, default));

        [Obsolete("This method will be removed in the next version. Please use the overload that accepts Action<ConfigureTransportContext> instead")]
        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, x => configureTransport(x.TransportSettings)));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, Action<ConfigureTransportContext> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, default, configureTransport));

        [Obsolete("This method will be removed in the next version. Please use the overload that accepts Action<ConfigureTransportContext> instead")]
        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, TokenCredential tokenCredential, Action<AzureServiceBusTransportSettings> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenCredential, x => configureTransport(x.TransportSettings)));

        public static ElsaOptionsBuilder UseAzureServiceBus(this ElsaOptionsBuilder elsaOptions, string connectionString, TokenCredential tokenCredential, Action<ConfigureTransportContext> configureTransport) =>
            elsaOptions.UseServiceBus(context => ConfigureAzureServiceBusEndpoint(context, connectionString, tokenCredential, configureTransport));

        public static ElsaOptionsBuilder PurgeAzureSubscriptionOnStartup(
            this ElsaOptionsBuilder elsaOptions,
            string connectionString)
        {

            elsaOptions.Services.AddStartupTask<PurgeSubscriptions>(sp =>
            {
                return new PurgeSubscriptions(sp.GetService<IServiceBusFactory>()!,
                    sp.GetService<ElsaOptions>()!,
                    sp.GetService<ILogger<PurgeSubscriptions>>()!,
                    sp.GetService<IDistributedLockProvider>()!,
                    connectionString);
            });

            return elsaOptions;
        }

        private static void ConfigureAzureServiceBusEndpoint(
            ServiceBusEndpointConfigurationContext context,
            string connectionString,
            TokenCredential? tokenProvider,
            Action<ConfigureTransportContext>? configureTransport)
        {
            var queueName = context.QueueName;

            context.Configurer
                .Transport(t =>
                {
                    if (queueName.Length > 50)
                        queueName = queueName.Substring(queueName.Length - 50);

                    t.UseNativeDeadlettering();
                    var transport = t.UseAzureServiceBus(connectionString, queueName, tokenProvider);

                    if (context.AutoDeleteOnIdle)
                        transport.SetAutoDeleteOnIdle(TimeSpan.FromMinutes(5));

                    configureTransport?.Invoke(new ConfigureTransportContext(context, transport));
                });
        }
    }

    public record ConfigureTransportContext(ServiceBusEndpointConfigurationContext ConfigurationContext, AzureServiceBusTransportSettings TransportSettings);
}