using Elsa.Activities.RabbitMq.Bookmarks;
using Elsa.Activities.RabbitMq.Consumers;
using Elsa.Activities.RabbitMq.Services;
using Elsa.Activities.RabbitMq.StartupTasks;
using Elsa.Activities.RabbitMq.Testing;
using Elsa.Events;
using Elsa.Options;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.RabbitMq.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddRabbitMqActivities(this ElsaOptionsBuilder options)
        {
            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IRabbitMqQueueStarter, RabbitMqQueueStarter>()
                .AddSingleton<IRabbitMqTestQueueManager, RabbitMqTestQueueManager>()
                .AddNotificationHandlersFrom<ConfigureRabbitMqActivitiesForTestHandler>()
                .AddStartupTask<StartRabbitMqQueues>()
                .AddBookmarkProvider<QueueMessageReceivedBookmarkProvider>();

            options.AddPubSubConsumer<RestartRabbitMqBusConsumer, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartRabbitMqBusConsumer, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartRabbitMqBusConsumer, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartRabbitMqBusConsumer, BookmarksDeleted>("WorkflowManagementEvents");

            options
                .AddActivity<RabbitMqMessageReceived>()
                .AddActivity<SendRabbitMqMessage>();

            return options;
        }
    }
}