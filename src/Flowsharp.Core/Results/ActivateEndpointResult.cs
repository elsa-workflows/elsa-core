using Flowsharp.Models;
using Flowsharp.Services;

namespace Flowsharp.Results
{
    /// <summary>
    /// A result that carries information about the next activity to execute.
    /// </summary>
    public class ActivateEndpointResult : ActivityExecutionResult
    {
        public ActivateEndpointResult(string endpointName = null)
        {
            EndpointName = endpointName;
        }

        public string EndpointName { get; }

        protected override void Execute(IWorkflowInvoker invoker, WorkflowExecutionContext workflowContext)
        {
            var currentActivity = workflowContext.CurrentActivity;
            workflowContext.ScheduleNextActivities(workflowContext, new SourceEndpoint(currentActivity, EndpointName));
        }
    }
}