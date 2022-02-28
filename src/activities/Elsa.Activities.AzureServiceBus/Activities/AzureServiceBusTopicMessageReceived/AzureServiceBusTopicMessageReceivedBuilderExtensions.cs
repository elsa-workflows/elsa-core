using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusTopicMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder TopicMessageReceived(this IBuilder builder, Action<ISetupActivity<AzureServiceBusTopicMessageReceived>> setup, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.Then(setup, null, lineNumber, sourceFile);

        public static IActivityBuilder TopicMessageReceived(this IBuilder builder, string topicName,string subscriptionName, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.TopicMessageReceived(setup => setup.WithTopicName(topicName).WithSubscriptionName(subscriptionName), lineNumber, sourceFile);
        
        public static IActivityBuilder TopicMessageReceived<T>(this IBuilder builder, string topicName,string subscriptionName, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) =>
            builder.TopicMessageReceived(setup => setup.WithTopicName(topicName).WithSubscriptionName(subscriptionName).WithMessageType<T>(), lineNumber, sourceFile);
    }
}