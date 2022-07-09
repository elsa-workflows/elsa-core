using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rebus
{
    public static class SendRebusMessageExtensions
    {
        // With Message
        public static ISetupActivity<SendRebusMessage> WithMessage(this ISetupActivity<SendRebusMessage> activity, Func<ActivityExecutionContext, ValueTask<object>> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendRebusMessage> WithMessage(this ISetupActivity<SendRebusMessage> activity, Func<ValueTask<object>> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendRebusMessage> WithMessage(this ISetupActivity<SendRebusMessage> activity, Func<object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendRebusMessage> WithMessage(this ISetupActivity<SendRebusMessage> activity, Func<ActivityExecutionContext, object> value) => activity.Set(x => x.Message, value!);
        public static ISetupActivity<SendRebusMessage> WithMessage(this ISetupActivity<SendRebusMessage> activity, object value) => activity.Set(x => x.Message, value!);
        
        // With Queue Name
        public static ISetupActivity<SendRebusMessage> WithQueueName(this ISetupActivity<SendRebusMessage> activity, Func<ActivityExecutionContext, ValueTask<string>> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendRebusMessage> WithQueueName(this ISetupActivity<SendRebusMessage> activity, Func<ValueTask<string>> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendRebusMessage> WithQueueName(this ISetupActivity<SendRebusMessage> activity, Func<string> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendRebusMessage> WithQueueName(this ISetupActivity<SendRebusMessage> activity, Func<ActivityExecutionContext, string> value) => activity.Set(x => x.QueueName, value!);
        public static ISetupActivity<SendRebusMessage> WithQueueName(this ISetupActivity<SendRebusMessage> activity, string value) => activity.Set(x => x.QueueName, value!);

        // With Headers
        public static ISetupActivity<SendRebusMessage> WithHeaders(this ISetupActivity<SendRebusMessage> activity, Func<ActivityExecutionContext, ValueTask<IDictionary<string, string>>> value) => activity.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRebusMessage> WithHeaders(this ISetupActivity<SendRebusMessage> activity, Func<ValueTask<IDictionary<string, string>>> value) => activity.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRebusMessage> WithHeaders(this ISetupActivity<SendRebusMessage> activity, Func<IDictionary<string, string>> value) => activity.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRebusMessage> WithHeaders(this ISetupActivity<SendRebusMessage> activity, Func<ActivityExecutionContext, IDictionary<string, string>> value) => activity.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRebusMessage> WithHeaders(this ISetupActivity<SendRebusMessage> activity, IDictionary<string, string> value) => activity.Set(x => x.Headers, value!);
    }
}