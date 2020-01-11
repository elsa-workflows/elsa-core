using System;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class SetVariableBuilderExtensions
    {
        public static ActivityBuilder SetVariable(this IBuilder builder, string variableName, IWorkflowExpression<object> value) => builder.Then<SetVariable>(x =>
        {
            x.VariableName = variableName;
            x.Value = value;
        });

        public static ActivityBuilder SetVariable(this IBuilder builder, string variableName, CodeExpression<object> value) => builder.SetVariable(variableName, (IWorkflowExpression<object>)value);
        public static ActivityBuilder SetVariable(this IBuilder builder, string variableName, Func<ActivityExecutionContext, object> value) => builder.SetVariable(variableName, new CodeExpression<object>(value));
        
        public static ActivityBuilder SetVariable(this IBuilder builder, string variableName, Func<object, object> value) =>
            builder.SetVariable(variableName, new CodeExpression<object>(context =>
            {
                var currentValue = context.GetVariable(variableName);
                return value(currentValue);
            }));
        
        public static ActivityBuilder SetVariable<T>(this IBuilder builder, string variableName, Func<T, T> value) =>
            builder.SetVariable(variableName, new CodeExpression<object>(context =>
            {
                var currentValue = context.GetVariable<T>(variableName);
                return value(currentValue);
            }));

        public static ActivityBuilder SetVariable(this IBuilder builder, string variableName, Func<object> value) => builder.SetVariable(variableName, (IWorkflowExpression<object>)new CodeExpression<object>(value));
        public static ActivityBuilder SetVariable<T>(this IBuilder builder, string variableName, T value) => builder.SetVariable(variableName, () => value);
    }
}