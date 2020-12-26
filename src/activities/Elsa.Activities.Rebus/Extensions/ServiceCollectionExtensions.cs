using System;
using Elsa;
using Elsa.Activities.Rebus;
using Elsa.Activities.Rebus.Consumers;
using Elsa.Activities.Rebus.StartupTasks;
using Elsa.Activities.Rebus.Triggers;
using Elsa.Runtime;
using Rebus.Handlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions AddRebusActivities(this ElsaOptions options, params Type[] messageTypes)
        {
            var services = options.Services;

            foreach (var messageType in messageTypes)
            {
                var handlerServiceType = typeof(IHandleMessages<>).MakeGenericType(messageType);
                var handlerImplementationType = typeof(MessageConsumer<>).MakeGenericType(messageType);
                services.AddTransient(handlerServiceType, handlerImplementationType);
            }

            services
                .AddTriggerProvider<MessageReceivedTriggerProvider>()
                .AddStartupTask(sp => ActivatorUtilities.CreateInstance<CreateSubscriptions>(sp, (object) messageTypes));

            options
                .AddActivity<PublishRebusMessage>()
                .AddActivity<SendRebusMessage>()
                .AddActivity<RebusMessageReceived>();

            return options;
        }

        public static ElsaOptions AddRebusActivities<T>(this ElsaOptions options) => options.AddRebusActivities(typeof(T));
        public static ElsaOptions AddRebusActivities<T1, T2>(this ElsaOptions options) => options.AddRebusActivities(typeof(T1), typeof(T2));
        public static ElsaOptions AddRebusActivities<T1, T2, T3>(this ElsaOptions options) => options.AddRebusActivities(typeof(T1), typeof(T2), typeof(T3));
    }
}