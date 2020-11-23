using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusMessageReceivedExtensions
    {
        public static ISetupActivity<AzureServiceBusMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.QueueName, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithQueueName(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.QueueName, value!);
        
        public static ISetupActivity<AzureServiceBusMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<Type>> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<ActivityExecutionContext, Type> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Func<Type> value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithMessageType(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived, Type value) => messageReceived.Set(x => x.MessageType, value!);
        public static ISetupActivity<AzureServiceBusMessageReceived> WithMessageType<T>(this ISetupActivity<AzureServiceBusMessageReceived> messageReceived) => messageReceived.WithMessageType(typeof(T));
    }
}