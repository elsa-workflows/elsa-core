using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Services;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Jobs
{
    public class RunQuartzWorkflowDefinitionJob : IJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly ITenantProvider _tenantProvider;
        public RunQuartzWorkflowDefinitionJob(IWorkflowDefinitionDispatcher workflowDefinitionDispatcher, ITenantProvider tenantProvider)
        {
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _tenantProvider = tenantProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowDefinitionId = dataMap.GetString("WorkflowDefinitionId")!;
            var activityId = dataMap.GetString("ActivityId")!;
            var tenant = _tenantProvider.GetCurrentTenant();

            await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(tenant, workflowDefinitionId, activityId), cancellationToken);
        }
    }
}