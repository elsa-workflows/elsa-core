using System;
using Elsa.Activities.Rebus;
using Elsa.Activities.Rebus.Bookmarks;
using Elsa.Activities.Rebus.Consumers;
using Elsa.Activities.Rebus.StartupTasks;
using Elsa.Options;
using Elsa.Runtime;
using Rebus.Handlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptionsBuilder AddRebusActivities(this ElsaOptionsBuilder options, params Type[] messageTypes)
        {
            var services = options.Services;

            foreach (var messageType in messageTypes)
            {
                var handlerServiceType = typeof(IHandleMessages<>).MakeGenericType(messageType);
                var handlerImplementationType = typeof(MessageConsumer<>).MakeGenericType(messageType);
                services.AddTransient(handlerServiceType, handlerImplementationType);
            }

            services
                .AddBookmarkProvider<MessageReceivedTriggerProvider>()
                .AddStartupTask(sp => ActivatorUtilities.CreateInstance<CreateSubscriptions>(sp, (object) messageTypes));

            options
                .AddActivity<PublishRebusMessage>()
                .AddActivity<SendRebusMessage>()
                .AddActivity<RebusMessageReceived>();

            return options;
        }

        public static ElsaOptionsBuilder AddRebusActivities<T>(this ElsaOptionsBuilder options) => options.AddRebusActivities(typeof(T));
        public static ElsaOptionsBuilder AddRebusActivities<T1, T2>(this ElsaOptionsBuilder options) => options.AddRebusActivities(typeof(T1), typeof(T2));
        public static ElsaOptionsBuilder AddRebusActivities<T1, T2, T3>(this ElsaOptionsBuilder options) => options.AddRebusActivities(typeof(T1), typeof(T2), typeof(T3));
    }
}