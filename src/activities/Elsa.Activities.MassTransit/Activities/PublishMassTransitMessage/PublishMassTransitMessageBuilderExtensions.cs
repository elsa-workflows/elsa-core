using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class PublishMassTransitMessageBuilderExtensions
    {
        public static ActivityBuilder PublishMassTransitMessage(this IBuilder builder, Action<PublishMassTransitMessage>? setup = default) => builder.Then(setup);
        public static ActivityBuilder PublishMassTransitMessage(this IBuilder builder, IWorkflowExpression message) => builder.PublishMassTransitMessage(x => x.WithMessage(message));
        public static ActivityBuilder PublishMassTransitMessage<T>(this IBuilder builder, Func<ActivityExecutionContext, T> message) => builder.PublishMassTransitMessage(x => x.WithMessage(message));
        public static ActivityBuilder PublishMassTransitMessage<T>(this IBuilder builder, Func<T> message) => builder.PublishMassTransitMessage(x => x.WithMessage(message));
        public static ActivityBuilder PublishMassTransitMessage<T>(this IBuilder builder, T message) => builder.PublishMassTransitMessage(x => x.WithMessage(message));
    }
}