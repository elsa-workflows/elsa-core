using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowDefinitionJob
    {
        private readonly IWorkflowLaunchpad _launchpad;
        public WorkflowDefinitionJob(IWorkflowLaunchpad launchpad) => _launchpad = launchpad;

        public async Task ExecuteAsync(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default) =>
            await _launchpad.FindAndExecuteStartableWorkflowAsync(request.WorkflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, request.Input, request.TenantId, cancellationToken);
    }
}