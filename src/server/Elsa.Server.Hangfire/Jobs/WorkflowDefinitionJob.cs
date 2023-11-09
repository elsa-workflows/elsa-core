using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Services;
using Hangfire;

namespace Elsa.Server.Hangfire.Jobs
{
    public class WorkflowDefinitionJob
    {
        private readonly IWorkflowLaunchpad _launchpad;
        public WorkflowDefinitionJob(IWorkflowLaunchpad launchpad) => _launchpad = launchpad;

        [JobDisplayName("{0}")]
        public async Task ExecuteAsync(string jobName, ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                await _launchpad.FindAndExecuteStartableWorkflowAsync(request.WorkflowDefinitionId, request.ActivityId, request.CorrelationId, request.ContextId, request.Input, request.TenantId, request.IgnoreAlreadyRunningAndSingleton, cancellationToken);
            }
            catch (WorkflowAlreadyRunningException) when (request.IgnoreAlreadyRunningAndSingleton == true) 
            {
                // Ignore when IgnoreAlreadyRunningAndSingleton is set, allow other exceptions to bubble up
            }
        }
    }
}