using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Server.Hangfire.Jobs
{
    public class CorrelatedWorkflowDefinitionJob
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public CorrelatedWorkflowDefinitionJob(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        public async Task ExecuteAsync(TriggerWorkflowsRequest request, CancellationToken cancellationToken = default) => await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(new WorkflowsQuery(
                request.ActivityType,
                request.Bookmark,
                request.CorrelationId,
                request.WorkflowInstanceId,
                request.ContextId,
                request.TenantId),
            request.Input,
            cancellationToken);
    }
}