using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowDefinitionJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly Tenant _tenant;

        public RunHangfireWorkflowDefinitionJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher, ITenantProvider tenantProvider)
        {
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _tenant = tenantProvider.GetCurrentTenant();
        }

        public async Task ExecuteAsync(RunHangfireWorkflowDefinitionJobModel data) => await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(_tenant, data.WorkflowDefinitionId, data.ActivityId));
    }
}