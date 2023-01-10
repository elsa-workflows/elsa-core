using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.Kafka.Activities.SendKafkaMessage
{
    public static class SendKafkaMessageExtensions
    {
        public static ISetupActivity<SendKafkaMessage> WithConnectionString(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendKafkaMessage> WithConnectionString(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendKafkaMessage> WithConnectionString(this ISetupActivity<SendKafkaMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendKafkaMessage> WithConnectionString(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendKafkaMessage> WithConnectionString(this ISetupActivity<SendKafkaMessage> sendMessage, string value) => sendMessage.Set(x => x.ConnectionString, value!);

        public static ISetupActivity<SendKafkaMessage> WithTopic(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.Topic, value!);
        public static ISetupActivity<SendKafkaMessage> WithTopic(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.Topic, value!);
        public static ISetupActivity<SendKafkaMessage> WithTopic(this ISetupActivity<SendKafkaMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.Topic, value!);
        public static ISetupActivity<SendKafkaMessage> WithTopic(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.Topic, value!);
        public static ISetupActivity<SendKafkaMessage> WithTopic(this ISetupActivity<SendKafkaMessage> sendMessage, string value) => sendMessage.Set(x => x.Topic, value!);
        
        public static ISetupActivity<SendKafkaMessage> WithHeaders(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<Dictionary<string, string>>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendKafkaMessage> WithHeaders(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ValueTask<Dictionary<string, string>>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendKafkaMessage> WithHeaders(this ISetupActivity<SendKafkaMessage> sendMessage, Func<Dictionary<string, string>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendKafkaMessage> WithHeaders(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, Dictionary<string, string>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendKafkaMessage> WithHeaders(this ISetupActivity<SendKafkaMessage> sendMessage, Dictionary<string, string> value) => sendMessage.Set(x => x.Headers, value!);

        public static ISetupActivity<SendKafkaMessage> WithMessage(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendKafkaMessage> WithMessage(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendKafkaMessage> WithMessage(this ISetupActivity<SendKafkaMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendKafkaMessage> WithMessage(this ISetupActivity<SendKafkaMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendKafkaMessage> WithMessage(this ISetupActivity<SendKafkaMessage> sendMessage, string value) => sendMessage.Set(x => x.Message, value!);
    }
}
