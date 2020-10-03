using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ReceiveMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder ReceiveMassTransitMessage(this IBuilder builder, Action<ISetupActivity<ReceiveMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}