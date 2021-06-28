using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowInstanceJob
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public WorkflowInstanceJob(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;
        public async Task ExecuteAsync(ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default) => 
            await _workflowLaunchpad.ExecutePendingWorkflowAsync(request.WorkflowInstanceId, request.ActivityId, request.Input, cancellationToken);
    }
}