using System;
using Elsa.Activities.AzureServiceBus.Bookmarks;
using Elsa.Activities.AzureServiceBus.Consumers;
using Elsa.Activities.AzureServiceBus.Options;
using Elsa.Activities.AzureServiceBus.Services;
using Elsa.Activities.AzureServiceBus.StartupTasks;
using Elsa.Events;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
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
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IQueueMessageSenderFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IQueueMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<ITopicMessageSenderFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<ITopicMessageReceiverFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IServiceBusQueuesStarter, ServiceBusQueuesStarter>()
                .AddSingleton<IServiceBusTopicsStarter, ServiceBusTopicsStarter>()
                .AddSingleton<Scoped<IWorkflowLaunchpad>>()
                .AddStartupTask<StartServiceBusQueues>()
                .AddStartupTask<StartServiceBusTopics>()
                .AddBookmarkProvider<QueueMessageReceivedBookmarkProvider>()
                .AddBookmarkProvider<TopicMessageReceivedBookmarkProvider>()
                ;

            options.AddCompetingConsumer<RestartServiceBusQueuesConsumer, WorkflowDefinitionPublished>();
            options.AddCompetingConsumer<RestartServiceBusQueuesConsumer, WorkflowDefinitionRetracted>();
            options.AddCompetingConsumer<RestartServiceBusTopicsConsumer, WorkflowDefinitionPublished>();
            options.AddCompetingConsumer<RestartServiceBusTopicsConsumer, WorkflowDefinitionRetracted>();

            options
                .AddActivity<AzureServiceBusQueueMessageReceived>()
                .AddActivity<SendAzureServiceBusQueueMessage>()
                .AddActivity<SendAzureServiceBusTopicMessage>()
                .AddActivity<AzureServiceBusTopicMessageReceived>()
                ;

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