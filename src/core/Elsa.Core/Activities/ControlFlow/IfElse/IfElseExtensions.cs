using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class IfElseExtensions
    {
        public static IfElse WithCondition(this IfElse activity, IWorkflowExpression<bool> value) => activity.With(x => x.Condition, value);
        public static IfElse WithCondition(this IfElse activity, Func<ActivityExecutionContext, bool> value) => activity.With(x => x.Condition, new CodeExpression<bool>(value));
        public static IfElse WithCondition(this IfElse activity, Func<bool> value) => activity.With(x => x.Condition, new CodeExpression<bool>(value));
        public static IfElse WithCondition(this IfElse activity, bool value) => activity.With(x => x.Condition, new CodeExpression<bool>(value));
    }
}