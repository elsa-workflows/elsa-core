using Elsa.Activities.OpcUa.Bookmarks;
using Elsa.Activities.OpcUa.Consumers;
using Elsa.Activities.OpcUa.Services;
using Elsa.Activities.OpcUa.StartupTasks;
using Elsa.Activities.OpcUa.Testing;
using Elsa.Events;
using Elsa.Options;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.OpcUa.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddOpcUaActivities(this ElsaOptionsBuilder options)
        {
            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IOpcUaQueueStarter, OpcUaQueueStarter>()
                .AddSingleton<IOpcUaTestQueueManager, OpcUaTestQueueManager>()
                .AddNotificationHandlersFrom<ConfigureOpcUaActivitiesForTestHandler>()
                .AddStartupTask<StartOpcUaQueues>()
                .AddBookmarkProvider<QueueMessageReceivedBookmarkProvider>();

            options.AddPubSubConsumer<RestartOpcUaBusConsumer, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartOpcUaBusConsumer, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartOpcUaBusConsumer, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartOpcUaBusConsumer, BookmarksDeleted>("WorkflowManagementEvents");

            options
                .AddActivity<OpcUaMessageReceived>()
                .AddActivity<SendOpcUaMessage>();

            return options;

        }
    }
}