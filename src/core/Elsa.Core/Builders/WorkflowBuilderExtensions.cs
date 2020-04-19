using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class WorkflowBuilderExtensions
    {
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Action action) => builder.StartWith(new Inline(action));
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Action<ActivityExecutionContext> action) => builder.StartWith(new Inline(action));
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Func<ActivityExecutionContext, Task> action) => builder.StartWith(new Inline(action));
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> action) => builder.StartWith(new Inline(action));
        
        public static IActivityBuilder SetVariable(this IWorkflowBuilder builder, string variableName, IWorkflowExpression<object> value) => builder.StartWith<SetVariable>(x =>
        {
            x.VariableName = variableName;
            x.Value = value;
        });
        
        public static IActivityBuilder SetVariable(this IWorkflowBuilder builder, string variableName, Func<object> value) => builder.SetVariable(variableName, (IWorkflowExpression<object>)new CodeExpression<object>(value));
        public static IActivityBuilder SetVariable<T>(this IWorkflowBuilder builder, string variableName, T value) => builder.SetVariable(variableName, () => value);
    }
}