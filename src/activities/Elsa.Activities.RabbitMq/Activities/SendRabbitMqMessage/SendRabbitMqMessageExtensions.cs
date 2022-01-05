using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq
{
    public static class SendRabbitMqMessageExtensions
    {
        public static ISetupActivity<SendRabbitMqMessage> WithConnectionString(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithConnectionString(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithConnectionString(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithConnectionString(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithConnectionString(this ISetupActivity<SendRabbitMqMessage> sendMessage, string value) => sendMessage.Set(x => x.ConnectionString, value!);

        public static ISetupActivity<SendRabbitMqMessage> WithExchangeName(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithExchangeName(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithExchangeName(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithExchangeName(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithExchangeName(this ISetupActivity<SendRabbitMqMessage> sendMessage, string value) => sendMessage.Set(x => x.ExchangeName, value!);

        public static ISetupActivity<SendRabbitMqMessage> WithTopic(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithTopic(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithTopic(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithTopic(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithTopic(this ISetupActivity<SendRabbitMqMessage> sendMessage, string value) => sendMessage.Set(x => x.RoutingKey, value!);

        public static ISetupActivity<SendRabbitMqMessage> WithHeaders(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<Dictionary<string, string>>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithHeaders(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ValueTask<Dictionary<string, string>>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithHeaders(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<Dictionary<string, string>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithHeaders(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, Dictionary<string, string>> value) => sendMessage.Set(x => x.Headers, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithHeaders(this ISetupActivity<SendRabbitMqMessage> sendMessage, Dictionary<string, string> value) => sendMessage.Set(x => x.Headers, value!);

        public static ISetupActivity<SendRabbitMqMessage> WithMessage(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, ValueTask<string>> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithMessage(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ValueTask<string>> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithMessage(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<string> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithMessage(this ISetupActivity<SendRabbitMqMessage> sendMessage, Func<ActivityExecutionContext, string> value) => sendMessage.Set(x => x.Message, value!);
        public static ISetupActivity<SendRabbitMqMessage> WithMessage(this ISetupActivity<SendRabbitMqMessage> sendMessage, string value) => sendMessage.Set(x => x.Message, value!);
    }
}
