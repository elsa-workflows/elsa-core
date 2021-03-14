using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusQueueMessageExtensions
    {
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<ValueTask<string>> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<string> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, string value) => activity.Set(x => x.QueueName, value!);
        
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithMessage(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<ActivityExecutionContext, ValueTask<object>> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithMessage(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<ActivityExecutionContext, object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithMessage(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, Func<object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusQueueMessage> WithMessage(this ISetupActivity<SendAzureServiceBusQueueMessage> activity, object value) => activity.Set(x => x.Message, value!);
    }
}