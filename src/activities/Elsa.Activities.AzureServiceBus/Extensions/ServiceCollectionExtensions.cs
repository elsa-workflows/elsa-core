using System;
using Elsa.Activities.AzureServiceBus.HostedServices;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Activities.AzureServiceBus.Triggers;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddAzureServiceBusActivities(this ElsaOptions options, Action<AzureServiceBusOptions>? configure)
        {
            if (configure != null)
                options.Services.Configure(configure);
            else
                options.Services.AddOptions<AzureServiceBusOptions>();

            options.Services
                .AddSingleton(CreateServiceBusConnection)
                .AddSingleton(CreateServiceBusManagementClient)
                .AddSingleton<IMessageSenderFactory, MessageSenderFactory>()
                .AddSingleton<IMessageReceiverFactory, MessageReceiverFactory>()
                .AddHostedService<StartServiceBusQueues>()
                .AddTriggerProvider<MessageReceivedTriggerProvider>();

            options
                .AddActivity<AzureServiceBusMessageReceived>()
                .AddActivity<SendAzureServiceBusMessage>();
            
            return options;
        }

        private static ServiceBusConnection CreateServiceBusConnection(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            var connectionString = options.ConnectionString;
            return new ServiceBusConnection(connectionString, RetryPolicy.Default);
        }
        
        private static ManagementClient CreateServiceBusManagementClient(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            var connectionString = options.ConnectionString;
            return new ManagementClient(connectionString);
        }
    }
}