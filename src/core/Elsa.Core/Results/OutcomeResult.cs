using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Results
{
    /// <summary>
    /// A result that carries information about the next activity to execute.
    /// </summary>
    public class OutcomeResult : ActivityExecutionResult
    {
        public OutcomeResult(IEnumerable<string> endpointNames)
        {
            EndpointNames = endpointNames.ToList();
        }

        public IReadOnlyList<string> EndpointNames { get; }

        public override async Task ExecuteAsync(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            var currentActivity = workflowContext.CurrentActivity;

            foreach (var endpointName in EndpointNames)
            {
                ScheduleNextActivities(workflowContext, new SourceEndpoint(currentActivity, endpointName));
            }

            var eventHandlers = workflowContext.ServiceProvider.GetServices<IWorkflowEventHandler>();
            var logger = workflowContext.ServiceProvider.GetRequiredService<ILogger<OutcomeResult>>();
            await eventHandlers.InvokeAsync(x => x.ActivityExecutedAsync(workflowContext, currentActivity, cancellationToken), logger);
        }
        
        private void ScheduleNextActivities(WorkflowExecutionContext workflowContext, SourceEndpoint endpoint)
        {
            var completedActivity = workflowContext.CurrentActivity;
            var connections = workflowContext.Workflow.Connections.Where(x => x.Source.Activity == completedActivity &&
                                                                              (x.Source.Outcome ?? OutcomeNames.Done).Equals(endpoint.Outcome, StringComparison.OrdinalIgnoreCase));
            var activities = connections.Select(x => x.Target.Activity);
            
            workflowContext.ScheduleActivities(activities);
        }
    }
}