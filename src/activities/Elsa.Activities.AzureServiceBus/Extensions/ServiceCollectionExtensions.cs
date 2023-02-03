using System;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Consumers;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Activities.AzureServiceBus.StartupTasks;
using Elsa.Events;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.AzureServiceBus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddAzureServiceBusActivities(this ElsaOptionsBuilder options, Action<AzureServiceBusOptions>? configure)
        {
            if (configure != null)
                options.Services.Configure(configure);
            else
                options.Services.AddOptions<AzureServiceBusOptions>();

            options.Services
                .AddSingleton(CreateServiceBusConnection)
                .AddSingleton(CreateServiceBusManagementClient)
                .AddSingleton<IMessageSenderFactory, MessageSenderFactory>()
                .AddSingleton<IWorkerManager, WorkerManager>()
                .AddSingleton<IAzureServiceBusTenantIdResolver, DefaultAzureServiceBusTenantIdResolver>()
                .AddHostedService<StartWorkers>()
                .AddBookmarkProvider<MessageReceivedBookmarkProvider>()
                ;

            options.AddPubSubConsumer<UpdateWorkers, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, BookmarksDeleted>("WorkflowManagementEvents");

            options
                .AddActivity<AzureServiceBusQueueMessageReceived>()
                .AddActivity<SendAzureServiceBusQueueMessage>()
                .AddActivity<SendAzureServiceBusTopicMessage>()
                .AddActivity<AzureServiceBusTopicMessageReceived>();

            return options;
        }

        private static ServiceBusClient CreateServiceBusConnection(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            var connectionString = options.ConnectionString;

            return new ServiceBusClient(connectionString, new ServiceBusClientOptions
            {
                RetryOptions = new ServiceBusRetryOptions
                {
                    Mode = ServiceBusRetryMode.Fixed,
                    Delay = TimeSpan.FromSeconds(10),
                    MaxRetries = 100
                }
            });
        }

        private static ServiceBusAdministrationClient CreateServiceBusManagementClient(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<AzureServiceBusOptions>>().Value;
            var connectionString = options.ConnectionString;

            return new ServiceBusAdministrationClient(connectionString, new ServiceBusAdministrationClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Fixed,
                    Delay = TimeSpan.FromSeconds(5),
                    MaxRetries = 5
                }
            });
        }
    }
}