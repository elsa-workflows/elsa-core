using System.Threading.Tasks;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services;
using Hangfire;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowDefinitionJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        public RunHangfireWorkflowDefinitionJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher) 
            => _workflowDefinitionDispatcher = workflowDefinitionDispatcher;

        [JobDisplayName("{0}")]
        public async Task ExecuteAsync(string jobName, RunHangfireWorkflowDefinitionJobModel data) 
            => await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(data.WorkflowDefinitionId, data.ActivityId, IgnoreAlreadyRunningAndSingleton: true));
    }
}