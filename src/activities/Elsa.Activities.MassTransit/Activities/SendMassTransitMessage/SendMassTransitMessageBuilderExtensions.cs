using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class SendMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder SendMassTransitMessage(this IBuilder builder, Action<ISetupActivity<SendMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}