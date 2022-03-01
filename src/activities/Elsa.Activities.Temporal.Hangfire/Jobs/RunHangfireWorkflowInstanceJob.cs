using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowInstanceJob
    {
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly ITenantProvider _tenantProvider;

        public RunHangfireWorkflowInstanceJob(IWorkflowInstanceDispatcher workflowInstanceDispatcher, ITenantProvider tenantProvider)
        {
            _workflowInstanceDispatcher = workflowInstanceDispatcher;
            _tenantProvider = tenantProvider;
        }

        public async Task ExecuteAsync(RunHangfireWorkflowInstanceJobModel data) => await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(await _tenantProvider.GetCurrentTenantAsync(), data.WorkflowInstanceId, data.ActivityId));
    }
}