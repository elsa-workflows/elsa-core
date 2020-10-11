using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class PublishMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder PublishMassTransitMessage(this IBuilder builder, Action<ISetupActivity<PublishMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}