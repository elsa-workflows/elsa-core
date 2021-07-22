using System.Threading.Tasks;
using Elsa.Services.Models;
using Rebus.Handlers;

namespace Elsa.Services.Dispatch.Consumers
{
    public class TriggerWorkflowsRequestConsumer : IHandleMessages<TriggerWorkflowsRequest>
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        public TriggerWorkflowsRequestConsumer(IWorkflowLaunchpad workflowLaunchpad)
        {
            _workflowLaunchpad = workflowLaunchpad;
        }

        public async Task Handle(TriggerWorkflowsRequest message)
        {
            var pendingWorkflows = await _workflowLaunchpad.FindWorkflowsAsync(new WorkflowsQuery(
                message.ActivityType, 
                message.Bookmark,
                message.CorrelationId, 
                message.WorkflowInstanceId, 
                message.ContextId, 
                message.TenantId));

            await _workflowLaunchpad.ExecutePendingWorkflowsAsync(pendingWorkflows, message.Input);
        }
    }
}