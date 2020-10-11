using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ScheduleSendMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder ScheduleSendMassTransitMessage(this IBuilder builder, Action<ISetupActivity<ScheduleSendMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}