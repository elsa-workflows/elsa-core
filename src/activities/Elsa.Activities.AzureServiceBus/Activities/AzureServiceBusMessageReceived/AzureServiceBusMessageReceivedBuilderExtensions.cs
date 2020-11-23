using System;
using Elsa.Builders;

namespace Elsa.Activities.AzureServiceBus
{
    public static class AzureServiceBusMessageReceivedBuilderExtensions
    {
        public static IActivityBuilder MessageReceived(this IBuilder builder, Action<ISetupActivity<AzureServiceBusMessageReceived>> setup) => builder.Then(setup);
        public static IActivityBuilder MessageReceived<T>(this IBuilder builder, string queueName) => builder.MessageReceived(setup => setup.WithQueueName(queueName).WithMessageType<T>());
    }
}