using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseBuilderExtensions
    {
        public static IOutcomeBuilder IfElse(this IBuilder builder, IWorkflowExpression<bool> condition, Action<IOutcomeBuilder> trueBranch, Action<IOutcomeBuilder>? falseBranch = default) => IfElse(builder.Then<IfElse>(x => x.Condition = condition), trueBranch, falseBranch);
        public static IOutcomeBuilder IfElse(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<IOutcomeBuilder> trueBranch, Action<IOutcomeBuilder>? falseBranch = default) => builder.IfElse(new CodeExpression<bool>(condition), trueBranch, falseBranch);
        public static IOutcomeBuilder IfElse(this IBuilder builder, Func<bool> condition, Action<IOutcomeBuilder> trueBranch, Action<IOutcomeBuilder>? falseBranch = default) => builder.IfElse(new CodeExpression<bool>(condition), trueBranch, falseBranch);
        public static IOutcomeBuilder IfElse(this IBuilder builder, bool condition, Action<IOutcomeBuilder> trueBranch, Action<IOutcomeBuilder>? falseBranch = default) => builder.IfElse(new CodeExpression<bool>(() => condition), trueBranch, falseBranch);

        private static IOutcomeBuilder IfElse(IActivityBuilder ifElse, Action<IOutcomeBuilder> trueBranch, Action<IOutcomeBuilder>? falseBranch = default)
        {
            trueBranch.Invoke(ifElse.When(OutcomeNames.True));
            falseBranch?.Invoke(ifElse.When(OutcomeNames.False));
            return ifElse.When(OutcomeNames.Done);
        }
    }
}