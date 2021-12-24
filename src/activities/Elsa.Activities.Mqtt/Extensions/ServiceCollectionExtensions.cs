using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Consumers;
using Elsa.Activities.Mqtt.Services;
using Elsa.Activities.Mqtt.StartupTasks;
using Elsa.Activities.Mqtt.Testing;
using Elsa.Events;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Mqtt
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddMqttActivities(this ElsaOptionsBuilder options)
        {
            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMqttTopicsStarter, MqttTopicsStarter>()
                .AddSingleton<Scoped<IWorkflowLaunchpad>>()
                .AddStartupTask<StartMqttTopics>()
                .AddBookmarkProvider<MessageReceivedBookmarkProvider>()
                .AddSingleton<IMqttTestClientManager, MqttTestClientManager>()
                .AddNotificationHandlersFrom<ConfigureMqttActivitiesForTestHandler>();

            options.AddPubSubConsumer<RestartMqttTopicsConsumer, WorkflowDefinitionPublished>("WorkflowDefinitionEvents");
            options.AddPubSubConsumer<RestartMqttTopicsConsumer, WorkflowDefinitionRetracted>("WorkflowDefinitionEvents");
            options.AddPubSubConsumer<RestartMqttTopicsConsumer, WorkflowDefinitionDeleted>("WorkflowDefinitionEvents");

            options
                .AddActivity<MqttMessageReceived>()
                .AddActivity<SendMqttMessage>();

            return options;
        }
    }
}