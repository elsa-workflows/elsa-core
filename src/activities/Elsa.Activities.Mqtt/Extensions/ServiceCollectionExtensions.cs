using Elsa.Activities.Mqtt.Activities.MqttMessageReceived;
using Elsa.Activities.Mqtt.Activities.SendMqttMessage;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Consumers;
using Elsa.Activities.Mqtt.Services;
using Elsa.Activities.Mqtt.StartupTasks;
using Elsa.Activities.Mqtt.Testing;
using Elsa.Activities.Mqtt.Testing.Handlers;
using Elsa.Events;
using Elsa.Extensions;
using Elsa.Options;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Mqtt.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddMqttActivities(this ElsaOptionsBuilder options)
        {
            options.ContainerBuilder
                .AddMultiton<BusClientFactory>()
                .AddMultiton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddMultiton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddMultiton<IMqttTopicsStarter, MqttTopicsStarter>()
                .AddStartupTask<StartMqttTopics>()
                .AddMultiton<IMqttTestClientManager, MqttTestClientManager>()
                .AddNotificationHandlersFrom<ConfigureMqttActivitiesForTestHandler>()
                .AddBookmarkProvider<MessageReceivedBookmarkProvider>();

            options.AddPubSubConsumer<RestartMqttTopicsConsumer, TriggerIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartMqttTopicsConsumer, TriggersDeleted>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartMqttTopicsConsumer, BookmarkIndexingFinished>("WorkflowManagementEvents");
            options.AddPubSubConsumer<RestartMqttTopicsConsumer, BookmarksDeleted>("WorkflowManagementEvents");

            options
                .AddActivity<MqttMessageReceived>()
                .AddActivity<SendMqttMessage>();

            return options;
        }
    }
}