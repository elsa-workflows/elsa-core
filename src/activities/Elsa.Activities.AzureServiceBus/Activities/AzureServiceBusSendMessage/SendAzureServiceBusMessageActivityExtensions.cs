using System;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusMessageActivityExtensions
    {
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithQueue(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.Queue, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithQueue(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ValueTask<string>> value) => activity.Set(x => x.Queue, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithQueue(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<string> value) => activity.Set(x => x.Queue, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithQueue(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Queue, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithQueue(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, string value) => activity.Set(x => x.Queue, value!);
        
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithTopic(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.Topic, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithTopic(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ValueTask<string>> value) => activity.Set(x => x.Topic, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithTopic(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<string> value) => activity.Set(x => x.Topic, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithTopic(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.Topic, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithTopic(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, string value) => activity.Set(x => x.Topic, value!);
        
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithMessage(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ActivityExecutionContext, ValueTask<object>> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithMessage(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<ActivityExecutionContext, object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithMessage(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, Func<object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendAzureServiceBusMessageActivity> WithMessage(this ISetupActivity<SendAzureServiceBusMessageActivity> activity, object value) => activity.Set(x => x.Message, value!);
    }
}