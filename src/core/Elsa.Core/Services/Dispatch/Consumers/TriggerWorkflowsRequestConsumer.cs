using System.Threading.Tasks;
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
            var pendingWorkflows = await _workflowLaunchpad.CollectWorkflowsAsync(new CollectWorkflowsContext(
                message.ActivityType, 
                message.Bookmark, 
                message.Trigger,
                message.CorrelationId, 
                message.WorkflowInstanceId, 
                message.ContextId, 
                message.TenantId));

            await _workflowLaunchpad.ExecutePendingWorkflowsAsync(pendingWorkflows, message.Input);
        }
    }
}