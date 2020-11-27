using System;
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
        public static IServiceCollection AddRebusActivities(this IServiceCollection services, params Type[] messageTypes)
        {
            foreach (var messageType in messageTypes)
            {
                var handlerServiceType = typeof(IHandleMessages<>).MakeGenericType(messageType);
                var handlerImplementationType = typeof(MessageConsumer<>).MakeGenericType(messageType);
                services.AddTransient(handlerServiceType, handlerImplementationType);
            }
            
            return services
                .AddTriggerProvider<MessageReceivedTriggerProvider>()
                .AddStartupTask(sp => ActivatorUtilities.CreateInstance<CreateSubscriptions>(sp, (object)messageTypes))
                .AddActivity<PublishRebusMessage>()
                .AddActivity<SendRebusMessage>()
                .AddActivity<RebusMessageReceived>();
        }

        public static IServiceCollection AddRebusActivities<T>(this IServiceCollection services) => services.AddRebusActivities(typeof(T));
        public static IServiceCollection AddRebusActivities<T1, T2>(this IServiceCollection services) => services.AddRebusActivities(typeof(T1), typeof(T2));
        public static IServiceCollection AddRebusActivities<T1, T2, T3>(this IServiceCollection services) => services.AddRebusActivities(typeof(T1), typeof(T2), typeof(T3));
    }
}