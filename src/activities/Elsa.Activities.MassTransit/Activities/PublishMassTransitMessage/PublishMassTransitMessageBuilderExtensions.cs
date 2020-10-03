using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class PublishMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder PublishMassTransitMessage(this IBuilder builder, Action<ISetupActivity<PublishMassTransitMessage>>? setup = default) => builder.Then(setup);
    }
}