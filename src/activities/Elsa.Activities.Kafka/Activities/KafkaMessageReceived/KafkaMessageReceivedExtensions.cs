using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.Kafka.Activities.KafkaMessageReceived
{
    public static class KafkaMessageReceivedExtensions
    {
        public static ISetupActivity<KafkaMessageReceived> WithConnectionString(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<KafkaMessageReceived> WithConnectionString(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<KafkaMessageReceived> WithConnectionString(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<KafkaMessageReceived> WithConnectionString(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<KafkaMessageReceived> WithConnectionString(this ISetupActivity<KafkaMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.ConnectionString, value!);

        public static ISetupActivity<KafkaMessageReceived> WithTopic(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<KafkaMessageReceived> WithTopic(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<KafkaMessageReceived> WithTopic(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<KafkaMessageReceived> WithTopic(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Topic, value!);
        public static ISetupActivity<KafkaMessageReceived> WithTopic(this ISetupActivity<KafkaMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.Topic, value!);

        public static ISetupActivity<KafkaMessageReceived> WithGroup(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.Group, value!);
        public static ISetupActivity<KafkaMessageReceived> WithGroup(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.Group, value!);
        public static ISetupActivity<KafkaMessageReceived> WithGroup(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.Group, value!);
        public static ISetupActivity<KafkaMessageReceived> WithGroup(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.Group, value!);
        public static ISetupActivity<KafkaMessageReceived> WithGroup(this ISetupActivity<KafkaMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.Group, value!);

        public static ISetupActivity<KafkaMessageReceived> WithHeaders(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<Dictionary<string, string>>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<KafkaMessageReceived> WithHeaders(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ValueTask<Dictionary<string, string>>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<KafkaMessageReceived> WithHeaders(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<Dictionary<string, string>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<KafkaMessageReceived> WithHeaders(this ISetupActivity<KafkaMessageReceived> messageReceived, Func<ActivityExecutionContext, Dictionary<string, string>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<KafkaMessageReceived> WithHeaders(this ISetupActivity<KafkaMessageReceived> messageReceived, Dictionary<string, string> value) => messageReceived.Set(x => x.Headers, value!);
    }
}
