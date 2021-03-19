using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusQueueMessageReceivedExtensions
    {
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.QueueName, value!);
        
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<Type>> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<ActivityExecutionContext, Type> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Func<Type> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived, Type value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusQueueMessageReceived> WithMessageType<T>(this ISetupActivity<AzureServiceBusQueueMessageReceived> messageReceived) => messageReceived.WithMessageType(typeof(T));
    }
}