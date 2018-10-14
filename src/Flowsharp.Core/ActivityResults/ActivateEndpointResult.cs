using Flowsharp.Models;

namespace Flowsharp.ActivityResults
{
    /// <summary>
    /// A result that carries information about the next activity to execute.
    /// </summary>
    public class ActivateEndpointResult : ActivityExecutionResult
    {
        public ActivateEndpointResult(SourceEndpoint endpoint)
        {
            Endpoint = endpoint;
        }

        public SourceEndpoint Endpoint { get; }

        protected override void Execute(WorkflowExecutionContext workflowContext)
        {
            workflowContext.ScheduleNextActivities(workflowContext, Endpoint);
        }
    }
}