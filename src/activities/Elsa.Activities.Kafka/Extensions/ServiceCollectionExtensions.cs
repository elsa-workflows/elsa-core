using Elsa.Activities.Kafka.Activities.KafkaMessageReceived;
using Elsa.Activities.Kafka.Activities.SendKafkaMessage;
using Elsa.Activities.Kafka.Bookmarks;
using Elsa.Activities.Kafka.Consumers;
using Elsa.Activities.Kafka.Services;
using Elsa.Activities.Kafka.StartupTasks;
using Elsa.Events;
using Elsa.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Kafka.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddKafkaActivities(this ElsaOptionsBuilder options)
        {
            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IWorkerManager, WorkerManager>()
                .AddHostedService<StartKafkaQueues>()
                .AddSingleton<IKafkaTenantIdResolver, DefaultKafkaTenantIdResolver>()
                .AddBookmarkProvider<QueueMessageReceivedBookmarkProvider>();

            options.AddPubSubConsumer<UpdateWorkers, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<UpdateWorkers, BookmarksDeleted>("WorkflowManagementEvents");
            
            
            options
                .AddActivity<KafkaMessageReceived>()
                .AddActivity<SendKafkaMessage>();

            return options;
        }
    }
}