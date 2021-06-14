using System.Threading.Tasks;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services.Dispatch;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowDefinitionJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        public RunHangfireWorkflowDefinitionJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher) => _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
        public async Task ExecuteAsync(RunHangfireWorkflowDefinitionJobModel data) => await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(data.WorkflowDefinitionId, data.ActivityId));
    }
}