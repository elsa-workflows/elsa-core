using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class WorkflowBuilderExtensions
    {
        public static ConnectionBuilder StartWith(this IWorkflowBuilder builder, Action action) => builder.StartWith(new Inline(action));
        public static ConnectionBuilder StartWith(this IWorkflowBuilder builder, Action<WorkflowExecutionContext, ActivityExecutionContext> action) => builder.StartWith(new Inline(action));
        public static ConnectionBuilder StartWith(this IWorkflowBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> action) => builder.StartWith(new Inline(action));
        public static ConnectionBuilder StartWith(this IWorkflowBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> action) => builder.StartWith(new Inline(action));
    }
}