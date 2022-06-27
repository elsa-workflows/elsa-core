using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elsa.Builders;

namespace Elsa.Activities.Kafka.Activities.KafkaMessageReceived
{
    public static class KafkaMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder MessageReceived(this IBuilder builder, Action<ISetupActivity<KafkaMessageReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, string topic, string group, Dictionary<string, string> headers, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithGroup(group).WithHeaders(headers), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, string group, Dictionary<string, string> headers, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithGroup(group).WithHeaders(headers), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, string topic, string group, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithTopic(topic).WithGroup(group), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, string group, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithGroup(group), lineNumber, sourceFile);
    }
}
