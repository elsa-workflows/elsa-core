using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq
{
    public static class RabbitMqMessageReceivedExtensions
    {
        public static ISetupActivity<RabbitMqMessageReceived> WithConnectionString(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithConnectionString(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithConnectionString(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithConnectionString(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.ConnectionString, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithConnectionString(this ISetupActivity<RabbitMqMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.ConnectionString, value!);

        public static ISetupActivity<RabbitMqMessageReceived> WithExchangeName(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithExchangeName(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithExchangeName(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithExchangeName(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.ExchangeName, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithExchangeName(this ISetupActivity<RabbitMqMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.ExchangeName, value!);

        public static ISetupActivity<RabbitMqMessageReceived> WithRoutingKey(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<string>> value) => messageReceived.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithRoutingKey(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ValueTask<string>> value) => messageReceived.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithRoutingKey(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<string> value) => messageReceived.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithRoutingKey(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, string> value) => messageReceived.Set(x => x.RoutingKey, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithRoutingKey(this ISetupActivity<RabbitMqMessageReceived> messageReceived, string value) => messageReceived.Set(x => x.RoutingKey, value!);

        public static ISetupActivity<RabbitMqMessageReceived> WithHeaders(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, ValueTask<Dictionary<string, string>>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithHeaders(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ValueTask<Dictionary<string, string>>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithHeaders(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<Dictionary<string, string>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithHeaders(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Func<ActivityExecutionContext, Dictionary<string, string>> value) => messageReceived.Set(x => x.Headers, value!);
        public static ISetupActivity<RabbitMqMessageReceived> WithHeaders(this ISetupActivity<RabbitMqMessageReceived> messageReceived, Dictionary<string, string> value) => messageReceived.Set(x => x.Headers, value!);
    }
}
