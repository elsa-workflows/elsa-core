using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForExtensions
    {
        public static For WithStart(this For activity, IWorkflowExpression<int> value) => activity.With(x => x.Start, value);
        public static For WithStart(this For activity, Func<ActivityExecutionContext, int> value) => activity.WithStart(new CodeExpression<int>(value));
        public static For WithStart(this For activity, Func<int> value) => activity.WithStart(new CodeExpression<int>(value));
        public static For WithStart(this For activity, int value) => activity.WithStart(new CodeExpression<int>(value));
        public static For WithEnd(this For activity, IWorkflowExpression<int> value) => activity.With(x => x.End, value);
        public static For WithEnd(this For activity, Func<ActivityExecutionContext, int> value) => activity.WithEnd(new CodeExpression<int>(value));
        public static For WithEnd(this For activity, Func<int> value) => activity.WithEnd(new CodeExpression<int>(value));
        public static For WithEnd(this For activity, int value) => activity.WithEnd(new CodeExpression<int>(value));
        public static For WithStep(this For activity, IWorkflowExpression<int> value) => activity.With(x => x.Step, value);
        public static For WithStep(this For activity, Func<ActivityExecutionContext, int> value) => activity.WithStep(new CodeExpression<int>(value));
        public static For WithStep(this For activity, Func<int> value) => activity.WithStep(new CodeExpression<int>(value));
        public static For WithStep(this For activity, int value) => activity.WithStep(new CodeExpression<int>(value));
    }
}