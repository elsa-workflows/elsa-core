using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder MessageReceived(this IBuilder builder, Action<ISetupActivity<AzureServiceBusMessageReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);
        
        public static IActivityBuilder MessageReceived(this IBuilder builder, string queueOrTopic, string? subscription = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithQueueOrTopic(queueOrTopic).WithSubscription(subscription), lineNumber, sourceFile);

        public static IActivityBuilder MessageReceived<T>(this IBuilder builder, string queueOrTopic, string? subscription = default, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.MessageReceived(setup => setup.WithQueueOrTopic(queueOrTopic).WithSubscription(subscription).WithMessageType<T>(), lineNumber, sourceFile);
    }
}