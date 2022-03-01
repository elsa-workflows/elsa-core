using System.Threading.Tasks;
using Elsa.Abstractions.Multitenancy;
using Elsa.Activities.Temporal.Hangfire.Models;
using Elsa.Services;

namespace Elsa.Activities.Temporal.Hangfire.Jobs
{
    public class RunHangfireWorkflowDefinitionJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly ITenantProvider _tenantProvider;

        public RunHangfireWorkflowDefinitionJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher, ITenantProvider tenantProvider)
        {
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _tenantProvider = tenantProvider;
        }

        public async Task ExecuteAsync(RunHangfireWorkflowDefinitionJobModel data) => await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(await _tenantProvider.GetCurrentTenantAsync(), data.WorkflowDefinitionId, data.ActivityId));
    }
}