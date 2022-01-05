using Elsa.Builders;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Elsa.Activities.RabbitMq
{
    public static class SendRabbitMqMessageBuilderExtensions
    {
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, Action<ISetupActivity<SendRabbitMqMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Dictionary<string, string> headers, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Dictionary<string, string> headers, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Dictionary<string, string> headers, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Dictionary<string, string> headers, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Dictionary<string, string> headers, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Dictionary<string, string> headers, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Dictionary<string, string> headers, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Dictionary<string, string> headers, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);



        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string exchangeName, string topic, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithExchangeName(exchangeName).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);
    }
}
