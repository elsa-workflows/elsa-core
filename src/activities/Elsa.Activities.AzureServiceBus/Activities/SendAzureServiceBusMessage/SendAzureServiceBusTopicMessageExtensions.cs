using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusTopicMessageExtensions
    {
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithTopicName(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.TopicName, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithTopicName(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<ValueTask<string>> value) => activity.Set(x => x.TopicName, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithTopicName(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<string> value) => activity.Set(x => x.TopicName, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithTopicName(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.TopicName, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithTopicName(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, string value) => activity.Set(x => x.TopicName, value!);

        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithMessage(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<ActivityExecutionContext, ValueTask<object>> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithMessage(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<ActivityExecutionContext, object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithMessage(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, Func<object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusTopicMessage> WithMessage(this ISetupActivity<SendAzureServiceBusTopicMessage> activity, object value) => activity.Set(x => x.Message, value!);
    }
}