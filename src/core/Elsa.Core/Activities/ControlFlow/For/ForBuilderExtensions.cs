using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForBuilderExtensions
    {
        public static IOutcomeBuilder For(this IBuilder builder, Action<For> setup, Action<IOutcomeBuilder> iteration) => For(builder.Then(setup), iteration);

        public static IOutcomeBuilder For(
            this IBuilder builder,
            IWorkflowExpression<int> start,
            IWorkflowExpression<int> end,
            IWorkflowExpression<int> step,
            Action<IOutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        public static IOutcomeBuilder For(
            this IBuilder builder,
            Func<ActivityExecutionContext, int> start,
            Func<ActivityExecutionContext, int> end,
            Func<ActivityExecutionContext, int> step,
            Action<IOutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        public static IOutcomeBuilder For(
            this IBuilder builder,
            Func<ActivityExecutionContext, int> start,
            Func<ActivityExecutionContext, int> end,
            Action<IOutcomeBuilder> setupIteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(1), setupIteration);

        public static IOutcomeBuilder For(
            this IBuilder builder,
            Func<int> start,
            Func<int> end,
            Func<int> step,
            Action<IOutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);

        public static IOutcomeBuilder For(
            this IBuilder builder,
            Func<int> start,
            Func<int> end,
            Action<IOutcomeBuilder> setupIteration) =>
            For(builder.Then<For>(a => a.WithStart(start).WithEnd(end).WithStep(1)), setupIteration);

        public static IOutcomeBuilder For(
            this IBuilder builder,
            int start,
            int end,
            int step,
            Action<IOutcomeBuilder> iteration) =>
            builder.For(a => a.WithStart(start).WithEnd(end).WithStep(step), iteration);
        
        public static IOutcomeBuilder For(
            this IBuilder builder,
            int start,
            int end,
            Action<IOutcomeBuilder> iteration) =>
            builder.For(start, end, 1, iteration);

        private static IOutcomeBuilder For(IActivityBuilder @for, Action<IOutcomeBuilder> iteration)
        {
            iteration.Invoke(@for.When(OutcomeNames.Iterate));
            return @for.When(OutcomeNames.Done);
        }
    }
}