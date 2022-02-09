using System.Threading.Tasks;
using Elsa.Abstractions.MultiTenancy;
using Elsa.Services;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Jobs
{
    public class RunQuartzWorkflowInstanceJob : IJob
    {
        private readonly IWorkflowInstanceDispatcher _workflowInstanceDispatcher;
        private readonly ITenantProvider _tenantProvider;

        public RunQuartzWorkflowInstanceJob(IWorkflowInstanceDispatcher workflowDefinitionDispatcher, ITenantProvider tenantProvider)
        {
            _workflowInstanceDispatcher = workflowDefinitionDispatcher;
            _tenantProvider = tenantProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowInstanceId = dataMap.GetString("WorkflowInstanceId")!;
            var activityId = dataMap.GetString("ActivityId")!;
            var tenant = _tenantProvider.GetCurrentTenant();

            await _workflowInstanceDispatcher.DispatchAsync(new ExecuteWorkflowInstanceRequest(tenant, workflowInstanceId, activityId), cancellationToken);
        }
    }
}