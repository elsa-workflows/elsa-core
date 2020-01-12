using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ReceiveMassTransitMessageBuilderExtensions
    {
        public static ActivityBuilder ReceiveMassTransitMessage(this IBuilder builder, Action<ReceiveMassTransitMessage>? setup = default) => builder.Then(setup);
        public static ActivityBuilder ReceiveMassTransitMessage(this IBuilder builder, Type messageType) => builder.ReceiveMassTransitMessage(x => x.WithMessageType(messageType));
        public static ActivityBuilder ReceiveMassTransitMessage<T>(this IBuilder builder, Type messageType) => builder.ReceiveMassTransitMessage(x => x.WithMessageType<T>());
    }
}