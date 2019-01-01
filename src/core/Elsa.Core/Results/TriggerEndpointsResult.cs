using System.Collections.Generic;
using Elsa.Models;

namespace Elsa.Results
{
    /// <summary>
    /// A result that carries information about the next activity to execute.
    /// </summary>
    public class TriggerEndpointsResult : ActivityExecutionResult
    {
        public TriggerEndpointsResult(IEnumerable<string> endpointNames)
        {
            EndpointNames = endpointNames;
        }

        public IEnumerable<string> EndpointNames { get; }

        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            var currentActivity = workflowContext.CurrentActivity;

            foreach (var endpointName in EndpointNames)
            {
                workflowContext.ScheduleNextActivities(workflowContext, new SourceEndpoint(currentActivity, endpointName));
            }
        }
    }
}