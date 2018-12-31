using Elsa.Models;

namespace Elsa.Results
{
    /// <summary>
    /// A result that carries information about the next activity to execute.
    /// </summary>
    public class TriggerEndpointResult : ActivityExecutionResult
    {
        public TriggerEndpointResult(string endpointName = null)
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