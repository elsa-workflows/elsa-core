using Elsa.Services.Models;

namespace Elsa.Messages
{
    /// <summary>
    /// Published when a workflow transitioned into the Faulted state.
    /// </summary>
    public class WorkflowFaulted : WorkflowNotification
    {
        public WorkflowFaulted(Workflow workflow) : base(workflow)
        {
        }
    }
}