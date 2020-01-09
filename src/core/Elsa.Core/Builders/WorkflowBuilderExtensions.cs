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
        public static ActivityBuilder StartWith(this IWorkflowBuilder builder, Action action) => builder.StartWith(new Inline(action));
        public static ActivityBuilder StartWith(this IWorkflowBuilder builder, Action<WorkflowExecutionContext, ActivityExecutionContext> action) => builder.StartWith(new Inline(action));
        public static ActivityBuilder StartWith(this IWorkflowBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> action) => builder.StartWith(new Inline(action));
        public static ActivityBuilder StartWith(this IWorkflowBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> action) => builder.StartWith(new Inline(action));
        
        public static ActivityBuilder SetVariable(this IWorkflowBuilder activityBuilder, string variableName, IWorkflowExpression<object> value) => activityBuilder.StartWith<SetVariable>(x =>
        {
            x.VariableName = variableName;
            x.Value = value;
        });
        
        public static ActivityBuilder SetVariable(this IWorkflowBuilder activityBuilder, string variableName, Func<object> value) => activityBuilder.SetVariable(variableName, (IWorkflowExpression<object>)new CodeExpression<object>(value));
        public static ActivityBuilder SetVariable<T>(this IWorkflowBuilder activityBuilder, string variableName, T value) => activityBuilder.SetVariable(variableName, () => value);
    }
}