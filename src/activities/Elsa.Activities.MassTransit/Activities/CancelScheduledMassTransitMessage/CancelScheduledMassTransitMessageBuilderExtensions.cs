using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class CancelScheduledMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder CancelScheduledMassTransitMessage(this IBuilder builder, Action<ISetupActivity<CancelScheduledMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}