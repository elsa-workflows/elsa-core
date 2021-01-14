using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Activities.AzureServiceBus
{
    public static class SendAzureServiceBusMessageBuilderExtensions
    {
        public static IActivityBuilder SendMessage(this IBuilder builder, Action<ISetupActivity<SendAzureServiceBusMessage>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, Func<ActivityExecutionContext, ValueTask<object>> message, [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, Func<ActivityExecutionContext, object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, Func<object> message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message), lineNumber, sourceFile);

        public static IActivityBuilder SendMessage(this IBuilder builder, string queueName, object message, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.SendMessage(setup => setup.WithQueueName(queueName).WithMessage(message), lineNumber, sourceFile);
    }
}