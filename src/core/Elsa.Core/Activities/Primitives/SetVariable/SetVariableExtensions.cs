using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Primitives
{
    public static class SetVariableExtensions
    {
        public static SetVariable WithVariableName(this SetVariable activity, string value) => activity.With(x => x.VariableName, value);
        public static SetVariable WithValue(this SetVariable activity, IWorkflowExpression value) => activity.With(x => x.Value, value);
        public static SetVariable WithValue<T>(this SetVariable activity, Func<ActivityExecutionContext, T> value) => activity.With(x => x.Value, new CodeExpression<T>(value));
        public static SetVariable WithValue<T>(this SetVariable activity, Func<T> value) => activity.With(x => x.Value, new CodeExpression<T>(value));
        public static SetVariable WithValue<T>(this SetVariable activity, T value) => activity.With(x => x.Value, new CodeExpression<T>(value));
    }
}