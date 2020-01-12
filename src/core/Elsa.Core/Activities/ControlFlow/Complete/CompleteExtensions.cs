using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CompleteExtensions
    {
        public static Complete WithOutput(this Complete activity, IWorkflowExpression value) => activity.With(x => x.OutputValue, value);
        public static Complete WithOutput<T>(this Complete activity, Func<ActivityExecutionContext, T> value) => activity.WithOutput(new CodeExpression<T>(value));
        public static Complete WithOutput<T>(this Complete activity, Func<T> value) => activity.WithOutput((IWorkflowExpression)new CodeExpression<T>(value));
        public static Complete WithOutput<T>(this Complete activity, T value) => activity.WithOutput((IWorkflowExpression)new CodeExpression<T>(value));
    }
}