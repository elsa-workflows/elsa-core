using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

namespace Elsa.Activities.Kafka.Activities.SendKafkaMessage
{
    public static class SendKafkaMessageBuilderExtensions
    {
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, Action<ISetupActivity<SendKafkaMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Dictionary<string, string> headers, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Dictionary<string, string> headers, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Dictionary<string, string> headers, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Dictionary<string, string> headers, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Dictionary<string, string> headers, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Dictionary<string, string> headers, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Dictionary<string, string> headers, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Dictionary<string, string> headers, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithHeaders(headers).WithMessage(message), lineNumber, sourceFile);



        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string topic, string group, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Func<ActivityExecutionContext, ValueTask<string>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Func<ActivityExecutionContext, string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, Func<string> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string connectionString, string group, string message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithConnectionString(connectionString).WithMessage(message), lineNumber, sourceFile);
    }
}
