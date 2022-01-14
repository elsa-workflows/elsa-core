using System;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Consumers;
using Elsa.Activities.Mqtt.Services;
using Elsa.Activities.Mqtt.StartupTasks;
using Elsa.Activities.Mqtt.Testing;
using Elsa.Events;
using Elsa.Multitenancy;
using Elsa.Options;
using Elsa.Runtime;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Mqtt
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddMqttActivities(this ElsaOptionsBuilder options, Action<MultitenancyOptions>? configure)
        {
            if (configure != null)
                options.Services.Configure(configure);
            else
                options.Services.AddOptions<MultitenancyOptions>();

            options.Services
                .AddSingleton<BusClientFactory>()
                .AddSingleton<IMessageReceiverClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton<IMessageSenderClientFactory>(sp => sp.GetRequiredService<BusClientFactory>())
                .AddSingleton(CreateMqttTopicsStarter)
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

        private static IMqttTopicsStarter CreateMqttTopicsStarter(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<MultitenancyOptions>>().Value;
            var multitenancyEnabled = options.MultitenancyEnabled;

            if (multitenancyEnabled)
                return ActivatorUtilities.GetServiceOrCreateInstance<MultitenantMqttTopicsStarter>(serviceProvider);
            else
                return ActivatorUtilities.GetServiceOrCreateInstance<MqttTopicsStarter>(serviceProvider);
        }
    }
}