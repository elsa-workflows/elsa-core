using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Rebus
{
    public static class PublishRebusMessageExtensions
    {
        // With Message
        public static ISetupActivity<PublishRebusMessage> WithMessage(this ISetupActivity<PublishRebusMessage> activity, Func<ActivityExecutionContext, ValueTask<object>> value) => activity.Set(x => x.Message, value!);

        public static ISetupActivity<PublishRebusMessage> WithMessage(this ISetupActivity<PublishRebusMessage> activity, Func<ValueTask<object>> value) => activity.Set(x => x.Message, value!);

        public static ISetupActivity<PublishRebusMessage> WithMessage(this ISetupActivity<PublishRebusMessage> activity, Func<object> value) => activity.Set(x => x.Message, value!);

        public static ISetupActivity<PublishRebusMessage> WithMessage(this ISetupActivity<PublishRebusMessage> activity, Func<ActivityExecutionContext, object> value) => activity.Set(x => x.Message, value!);

        public static ISetupActivity<PublishRebusMessage> WithMessage(this ISetupActivity<PublishRebusMessage> activity, object value) => activity.Set(x => x.Message, value!);

        // With Headers
        public static ISetupActivity<PublishRebusMessage> WithHeaders(this ISetupActivity<PublishRebusMessage> activity, Func<ActivityExecutionContext, ValueTask<IDictionary<string, string>>> value) => activity.Set(x => x.Headers, value!);

        public static ISetupActivity<PublishRebusMessage> WithHeaders(this ISetupActivity<PublishRebusMessage> activity, Func<ValueTask<IDictionary<string, string>>> value) => activity.Set(x => x.Headers, value!);

        public static ISetupActivity<PublishRebusMessage> WithHeaders(this ISetupActivity<PublishRebusMessage> activity, Func<IDictionary<string, string>> value) => activity.Set(x => x.Headers, value!);

        public static ISetupActivity<PublishRebusMessage> WithHeaders(this ISetupActivity<PublishRebusMessage> activity, Func<ActivityExecutionContext, IDictionary<string, string>> value) => activity.Set(x => x.Headers, value!);

        public static ISetupActivity<PublishRebusMessage> WithHeaders(this ISetupActivity<PublishRebusMessage> activity, IDictionary<string, string> value) => activity.Set(x => x.Headers, value!);
    }
}