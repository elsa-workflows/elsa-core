using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusMessageExtensions
    {
        public static ISetupActivity<SendAzureServiceBusMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<ValueTask<string>> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<string> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithQueueName(this ISetupActivity<SendAzureServiceBusMessage> activity, string value) => activity.Set(x => x.QueueName, value!);
        
        public static ISetupActivity<SendAzureServiceBusMessage> WithMessage(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<ActivityExecutionContext, ValueTask<object>> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithMessage(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<ActivityExecutionContext, object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithMessage(this ISetupActivity<SendAzureServiceBusMessage> activity, Func<object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusMessage> WithMessage(this ISetupActivity<SendAzureServiceBusMessage> activity, object value) => activity.Set(x => x.Message, value!);
    }
}