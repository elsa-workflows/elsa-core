using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class WhileExtensions
    {
        public static While WithCondition(this While activity, IWorkflowExpression<bool> value) => activity.With(x => x.Condition, value);
        public static While WithCondition(this While activity, Func<ActivityExecutionContext, bool> value) => activity.With(x => x.Condition, new CodeExpression<bool>(value));
        public static While WithCondition(this While activity, Func<bool> value) => activity.With(x => x.Condition, new CodeExpression<bool>(value));
        public static While WithCondition(this While activity, bool value) => activity.With(x => x.Condition, new CodeExpression<bool>(value));
    }
}