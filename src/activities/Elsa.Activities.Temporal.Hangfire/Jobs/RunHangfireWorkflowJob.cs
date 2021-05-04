using System.Threading.Tasks;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Dispatch;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;

        public RunHangfireWorkflowJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher, IWorkflowInstanceDispatcher workflowInstanceDispatcher)
        {
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
        }

        public async Task ExecuteAsync(RunHangfireWorkflowJobModel data)
        {
            if (data.WorkflowInstanceId == null)
                await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(data.WorkflowDefinitionId!, data.ActivityId, TenantId: data.TenantId));
            else
                await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(data.WorkflowInstanceId, data.ActivityId));
        }
    }
}
