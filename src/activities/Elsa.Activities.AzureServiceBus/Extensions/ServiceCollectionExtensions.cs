using System;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
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
using Microsoft.Rest.TransientFaultHandling;

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
                .AddSingleton<IWorkersStarter, WorkersStarter>()
                .AddHostedService<StartWorkers>()
                .AddBookmarkProvider<MessageReceivedBookmarkProvider>()
                ;

            options.AddPubSubConsumer<RestartServiceBusTopicsConsumer, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartServiceBusTopicsConsumer, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartServiceBusTopicsConsumer, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartServiceBusTopicsConsumer, BookmarksDeleted>("WorkflowManagementEvents");

            options
                .AddActivity<AzureServiceBusMessageReceived>()
                .AddActivity<SendAzureServiceBusMessageActivity>();

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