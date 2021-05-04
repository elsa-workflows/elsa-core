using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusTopicMessageReceivedExtensions
    {
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithTopicName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.TopicName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithTopicName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.TopicName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithTopicName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.TopicName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithTopicName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.TopicName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithTopicName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.TopicName, value!);

        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithSubscriptionName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.SubscriptionName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithSubscriptionName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.SubscriptionName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithSubscriptionName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.SubscriptionName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithSubscriptionName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.SubscriptionName, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithSubscriptionName(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.SubscriptionName, value!);

        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<Type>> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<ActivityExecutionContext, Type> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Func<Type> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived, Type value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusTopicMessageReceived> WithMessageType<T>(this ISetupActivity<AzureServiceBusTopicMessageReceived> messageReceived) => messageReceived.WithMessageType(typeof(T));
    }
}