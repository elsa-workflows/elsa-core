using System;
using Elsa.Activities.RabbitMq.Bookmarks;
using Elsa.Activities.RabbitMq.Consumers;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Activities.RabbitMq.StartupTasks;
using Elsa.Activities.RabbitMq.Testing;
using Elsa.Events;
using Elsa.Multitenancy;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.RabbitMq
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddRabbitMqActivities(this ElsaOptionsBuilder options, Action<MultitenancyOptions>? configure)
        {
            if (configure != null)
                options.Services.Configure(configure);
            else
                options.Services.AddOptions<MultitenancyOptions>();

            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton(CreateRabbitMqQueueStarter)
                .AddSingleton<Scoped<IWorkflowLaunchpad>>()
                .AddSingleton<IRabbitMqTestQueueManager, RabbitMqTestQueueManager>()
                .AddNotificationHandlersFrom<ConfigureRabbitMqActivitiesForTestHandler>()
                .AddStartupTask<StartRabbitMqQueues>()
                .AddBookmarkProvider<QueueMessageReceivedBookmarkProvider>();

            options.AddPubSubConsumer<RestartRabbitMqBusConsumer, WorkflowDefinitionPublished>("WorkflowDefinitionEvents");
            options.AddPubSubConsumer<RestartRabbitMqBusConsumer, WorkflowDefinitionRetracted>("WorkflowDefinitionEvents");

            options
                .AddActivity<RabbitMqMessageReceived>()
                .AddActivity<SendRabbitMqMessage>();

            return options;
        }

        private static IRabbitMqQueueStarter CreateRabbitMqQueueStarter(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<MultitenancyOptions>>().Value;
            var multitenancyEnabled = options.MultitenancyEnabled;

            if (multitenancyEnabled)
                return ActivatorUtilities.GetServiceOrCreateInstance<MultitenantRabbitMqQueueStarter>(serviceProvider);
            else
                return ActivatorUtilities.GetServiceOrCreateInstance<RabbitMqQueueStarter>(serviceProvider);
        }
    }
}