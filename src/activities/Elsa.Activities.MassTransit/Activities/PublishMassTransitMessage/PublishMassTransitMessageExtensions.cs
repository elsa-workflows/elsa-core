using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class PublishMassTransitMessageExtensions
    {
        public static PublishMassTransitMessage WithMessage(this PublishMassTransitMessage activity, IWorkflowExpression value) => activity.With(x => x.Message, value);
        public static PublishMassTransitMessage WithMessage<T>(this PublishMassTransitMessage activity, Func<ActivityExecutionContext, T> value) => activity.With(x => x.Message, new CodeExpression<T>(value));
        public static PublishMassTransitMessage WithMessage<T>(this PublishMassTransitMessage activity, Func<T> value) => activity.With(x => x.Message, new CodeExpression<T>(value));
        public static PublishMassTransitMessage WithMessage<T>(this PublishMassTransitMessage activity, T value) => activity.With(x => x.Message, new CodeExpression<T>(value));
    }
}