using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowInstanceJob
    {
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly Tenant _tenant;

        public RunHangfireWorkflowInstanceJob(IWorkflowInstanceDispatcher workflowInstanceDispatcher, ITenantProvider tenantProvider)
        {
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _tenant = tenantProvider.GetCurrentTenant();
        }

        public async Task ExecuteAsync(RunHangfireWorkflowInstanceJobModel data) => await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(_tenant, data.WorkflowInstanceId, data.ActivityId));
    }
}