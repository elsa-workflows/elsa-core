using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.ActivityResults;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class WorkflowBuilderExtensions
    {
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Action action) => builder.StartWith(new Inline(action));
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Action<ActivityExecutionContext> action) => builder.StartWith(new Inline(action));
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Func<ActivityExecutionContext, Task> action) => builder.StartWith(new Inline(action));
        public static IActivityBuilder StartWith(this IWorkflowBuilder builder, Func<ActivityExecutionContext, Task<IActivityExecutionResult>> action) => builder.StartWith(new Inline(action));
        
        public static IActivityBuilder SetVariable(this IWorkflowBuilder builder, string variableName, object? value) => builder.StartWith<SetVariable>(activity =>
        {
            activity.Set(x => x.VariableName, _ => variableName);
            activity.Set(x => x.Value, _ => value);
        });
        
        public static IActivityBuilder SetVariable(this IWorkflowBuilder builder, string variableName, Func<object?> value) => builder.SetVariable(variableName, value());
    }
}