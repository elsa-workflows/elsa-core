using Elsa.Activities.Rebus.Consumers;
using Elsa.Activities.Rebus.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Rebus.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRebusActivities(this IServiceCollection services) =>
            services
                .AddTriggerProvider<MessageReceivedTriggerProvider>()
                .AddActivity<PublishMessage>()
                .AddActivity<SendMessage>()
                .AddActivity<MessageReceived>();

        public static IServiceCollection AddRebusActivities<T>(this IServiceCollection services) => services.AddRebusActivities().AddMessageType<T>();
        public static IServiceCollection AddRebusActivities<T1, T2>(this IServiceCollection services) => services.AddRebusActivities().AddMessageType<T1>().AddMessageType<T2>();
        public static IServiceCollection AddRebusActivities<T1, T2, T3>(this IServiceCollection services) => services.AddRebusActivities().AddMessageType<T1>().AddMessageType<T2>().AddMessageType<T3>();
        public static IServiceCollection AddRebusActivities<T1, T2, T3, T4>(this IServiceCollection services) => services.AddRebusActivities().AddMessageType<T1>().AddMessageType<T2>().AddMessageType<T3>().AddMessageType<T4>();

        public static IServiceCollection AddRebusActivities<T1, T2, T3, T4, T5>(this IServiceCollection services) =>
            services.AddRebusActivities().AddMessageType<T1>().AddMessageType<T2>().AddMessageType<T3>().AddMessageType<T4>().AddMessageType<T5>();

        public static IServiceCollection AddMessageType<T>(this IServiceCollection services) => services.AddConsumer<T, MessageConsumer<T>>();
    }
}