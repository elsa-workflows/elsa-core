using System;
using System.Threading.Tasks;
using Elsa.Activities.Containers;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public static class SequenceBuilderExtensions
    {
        public static SequenceBuilder Add(this SequenceBuilder builder, Action activity) => builder.Add(new Inline(activity));
        public static SequenceBuilder Add(this SequenceBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => builder.Add(new Inline(activity));
        public static SequenceBuilder Add(this SequenceBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity) => builder.Add(new Inline(activity));
        public static SequenceBuilder Add(this SequenceBuilder builder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity) => builder.Add(new Inline(activity));
        
        public static SequenceBuilder BuildSequence(this IWorkflowBuilder workflowBuilder)
        {
            var sequenceBuilder = ActivatorUtilities.GetServiceOrCreateInstance<SequenceBuilder>(workflowBuilder.ServiceProvider);
            workflowBuilder.StartWith(sequenceBuilder);
            return sequenceBuilder;
        }
        
        public static SequenceBuilder BuildSequence(this IActivityBuilder activityBuilder) => ActivatorUtilities.GetServiceOrCreateInstance<SequenceBuilder>(activityBuilder.ServiceProvider);
    }
}