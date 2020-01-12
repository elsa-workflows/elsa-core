using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class WhileBuilderExtensions
    {
        public static OutcomeBuilder While(this IBuilder builder, IWorkflowExpression<bool> condition, Action<OutcomeBuilder> iteration) => While(builder.Then<While>(x => x.WithCondition(condition)), iteration);
        public static OutcomeBuilder While(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<OutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(condition), iteration);
        public static OutcomeBuilder While(this IBuilder builder, Func<bool> condition, Action<OutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(condition), iteration);
        public static OutcomeBuilder While(this IBuilder builder, bool condition, Action<OutcomeBuilder> iteration) => builder.While(new CodeExpression<bool>(() => condition), iteration);

        private static OutcomeBuilder While(ActivityBuilder @while, Action<OutcomeBuilder> iteration)
        {
            iteration.Invoke(@while.When(OutcomeNames.Iterate));
            return @while.When(OutcomeNames.Done);
        }
    }
}