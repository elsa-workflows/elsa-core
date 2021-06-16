using System.Threading.Tasks;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services.Dispatch;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowInstanceJob
    {
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        public RunHangfireWorkflowInstanceJob(IWorkflowInstanceDispatcher workflowInstanceDispatcher) => _workflowInstanceDispatcher = workflowInstanceDispatcher;
        public async Task ExecuteAsync(RunHangfireWorkflowInstanceJobModel data) => await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(data.WorkflowInstanceId, data.ActivityId));
    }
}