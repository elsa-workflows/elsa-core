using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusQueueMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder MessageQueueReceived(this IBuilder builder, Action<ISetupActivity<AzureServiceBusQueueMessageReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder MessageQueueReceived<T>(this IBuilder builder, string queueName, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageQueueReceived(setup => setup.WithQueueName(queueName).WithMessageType<T>(), lineNumber, sourceFile);
    }
}