using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseBuilderExtensions
    {
        public static OutcomeBuilder IfElse(this IBuilder builder, IWorkflowExpression<bool> condition, Action<OutcomeBuilder> trueBranch, Action<OutcomeBuilder>? falseBranch = default) => IfElse(builder.Then<IfElse>(x => x.Condition = condition), trueBranch, falseBranch);
        public static OutcomeBuilder IfElse(this IBuilder builder, Func<ActivityExecutionContext, bool> condition, Action<OutcomeBuilder> trueBranch, Action<OutcomeBuilder>? falseBranch = default) => builder.IfElse(new CodeExpression<bool>(condition), trueBranch, falseBranch);
        public static OutcomeBuilder IfElse(this IBuilder builder, Func<bool> condition, Action<OutcomeBuilder> trueBranch, Action<OutcomeBuilder>? falseBranch = default) => builder.IfElse(new CodeExpression<bool>(condition), trueBranch, falseBranch);
        public static OutcomeBuilder IfElse(this IBuilder builder, bool condition, Action<OutcomeBuilder> trueBranch, Action<OutcomeBuilder>? falseBranch = default) => builder.IfElse(new CodeExpression<bool>(() => condition), trueBranch, falseBranch);

        private static OutcomeBuilder IfElse(ActivityBuilder ifElse, Action<OutcomeBuilder> trueBranch, Action<OutcomeBuilder>? falseBranch = default)
        {
            trueBranch.Invoke(ifElse.When(OutcomeNames.True));
            falseBranch?.Invoke(ifElse.When(OutcomeNames.False));
            return ifElse.When(OutcomeNames.Done);
        }
    }
}