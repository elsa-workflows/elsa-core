using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ReceiveMassTransitMessageExtensions
    {
        public static ReceiveMassTransitMessage WithMessageType(this ReceiveMassTransitMessage activity, Type value) => activity.With(x => x.MessageType, value);
        public static ReceiveMassTransitMessage WithMessageType<T>(this ReceiveMassTransitMessage activity) => activity.WithMessageType(typeof(T));
    }
}