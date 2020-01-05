using System;
using System.Threading.Tasks;
using Elsa.Activities.Primitives;
using Elsa.Results;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public static class FlowchartBuilderExtensions
    {
        public static ConnectionBuilder StartWith(this FlowchartBuilder builder, Action activity) => builder.StartWith(new Inline(activity));
        public static ConnectionBuilder StartWith(this FlowchartBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => builder.StartWith(new Inline(activity));
        public static ConnectionBuilder StartWith(this FlowchartBuilder builder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity) => builder.StartWith(new Inline(activity));
        public static ConnectionBuilder StartWith(this FlowchartBuilder builder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity) => builder.StartWith(new Inline(activity));

        public static FlowchartBuilder BuildFlowchart(this IWorkflowBuilder workflowBuilder)
        {
            var flowchartBuilder = ActivatorUtilities.GetServiceOrCreateInstance<FlowchartBuilder>(workflowBuilder.ServiceProvider);
            workflowBuilder.StartWith(flowchartBuilder);
            return flowchartBuilder;
        }
        
        public static FlowchartBuilder BuildFlowchart(this IActivityBuilder activityBuilder) => ActivatorUtilities.GetServiceOrCreateInstance<FlowchartBuilder>(activityBuilder.ServiceProvider);
    }
}