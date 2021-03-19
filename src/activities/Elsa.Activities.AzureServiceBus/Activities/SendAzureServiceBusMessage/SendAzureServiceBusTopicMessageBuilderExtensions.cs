using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusTopicMessageBuilderExtensions
    {
        public static IActivityBuilder SendTopicMessage(this IBuilder builder, Action<ISetupActivity<SendAzureServiceBusTopicMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topicName, Func<ActivityExecutionContext, ValueTask<object>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendTopicMessage(setup => setup.WithTopicName(topicName).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topicName, Func<ActivityExecutionContext, object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithTopicName(topicName).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topicName, Func<object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithTopicName(topicName).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendTopicMessage(this IBuilder builder, string topicName, object message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendTopicMessage(setup => setup.WithTopicName(topicName).WithMessage(message), lineNumber, sourceFile);
    }
}