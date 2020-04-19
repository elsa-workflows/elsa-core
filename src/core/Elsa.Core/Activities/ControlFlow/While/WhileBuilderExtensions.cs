using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class WhileBuilderExtensions
    {
        public static IOutcomeBuilder While(this IBuilder builder, IWorkflowExpression<bool> condition, Action<IOutcomeBuilder> iteration) => While(builder.Then<While>(x => x.WithCondition(condition)), iteration);
        public static IOutcomeBuilder While(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<IOutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(condition), iteration);
        public static IOutcomeBuilder While(this IBuilder builder, Func<bool> condition, Action<IOutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(condition), iteration);
        public static IOutcomeBuilder While(this IBuilder builder, bool condition, Action<IOutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(() => condition), iteration);

        private static IOutcomeBuilder While(IActivityBuilder @while, Action<IOutcomeBuilder> iteration)
        {
            iteration.Invoke(@while.When(OutcomeNames.Iterate));
            return @while.When(OutcomeNames.Done);
        }
    }
}