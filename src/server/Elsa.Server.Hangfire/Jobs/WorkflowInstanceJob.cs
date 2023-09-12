using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Hangfire;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowInstanceJob
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        public WorkflowInstanceJob(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;

        [JobDisplayName("{0}")]
        public async Task ExecuteAsync(string jobName, ExecuteWorkflowInstanceRequest request, CancellationToken cancellationToken = default) => 
            await _workflowLaunchpad.ExecutePendingWorkflowAsync(request.WorkflowInstanceId, request.ActivityId, request.Input, cancellationToken);
    }
}