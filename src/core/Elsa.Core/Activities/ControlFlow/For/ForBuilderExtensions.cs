using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForBuilderExtensions
    {
        public static OutcomeBuilder For(this IBuilder builder, Action<For> setup, Action<OutcomeBuilder> iteration) => For(builder.Then(setup), iteration);

        public static OutcomeBuilder For(
            this IBuilder builder,
            IWorkflowExpression<int> start,
            IWorkflowExpression<int> end,
            IWorkflowExpression<int> step,
            Action<OutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        public static OutcomeBuilder For(
            this IBuilder builder,
            Func<ActivityExecutionContext, int> start,
            Func<ActivityExecutionContext, int> end,
            Func<ActivityExecutionContext, int> step,
            Action<OutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        public static OutcomeBuilder For(
            this IBuilder builder,
            Func<ActivityExecutionContext, int> start,
            Func<ActivityExecutionContext, int> end,
            Action<OutcomeBuilder> setupIteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(1), setupIteration);

        public static OutcomeBuilder For(
            this IBuilder builder,
            Func<int> start,
            Func<int> end,
            Func<int> step,
            Action<OutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        public static OutcomeBuilder For(
            this IBuilder builder,
            Func<int> start,
            Func<int> end,
            Action<OutcomeBuilder> setupIteration) =>
            For(builder.Then<For>(a => a.WithStart(start).WithEnd(end).WithStep(1)), setupIteration);

        public static OutcomeBuilder For(
            this IBuilder builder,
            int start,
            int end,
            int step,
            Action<OutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        private static OutcomeBuilder For(ActivityBuilder @for, Action<OutcomeBuilder> iteration)
        {
            iteration.Invoke(@for.When(OutcomeNames.Iterate));
            return @for.When(OutcomeNames.Done);
        }
    }
}