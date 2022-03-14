using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elsa.Activities.OpcUa
{
    public static class OpcUaMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder MessageReceived(this IBuilder builder, Action<ISetupActivity<OpcUaMessageReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, Dictionary<string, string> tags, int publishingInterval, int sessionTimeout, int operationTimeout, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithTags(tags).WithPublishingInterval(publishingInterval).WithSessionTimeout(sessionTimeout).WithOperationTimeout(operationTimeout), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, Dictionary<string, string> tags, int publishingInterval, int sessionTimeout, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithTags(tags).WithPublishingInterval(publishingInterval).WithSessionTimeout(sessionTimeout), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, Dictionary<string, string> tags, int publishingInterval, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithTags(tags).WithPublishingInterval(publishingInterval), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString, Dictionary<string, string> tags, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString).WithTags(tags), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived(this IBuilder builder, string connectionString,  [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithConnectionString(connectionString), lineNumber, sourceFile);
    }
}
