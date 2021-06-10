using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Dispatch;
using Quartz;

namespace Elsa.Activities.Temporal.Quartz.Jobs
{
    public class RunQuartzWorkflowDefinitionJob : IJob
    {
        private readonly IWorkflowDefinitionDispatcher _workflowDefinitionDispatcher;
        private readonly IDistributedLockProvider _distributedLockProvider;

        public RunQuartzWorkflowDefinitionJob(
            IWorkflowDefinitionDispatcher workflowDefinitionDispatcher,
            IDistributedLockProvider distributedLockProvider)
        {
            _workflowDefinitionDispatcher = workflowDefinitionDispatcher;
            _distributedLockProvider = distributedLockProvider;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowDefinitionId = dataMap.GetString("WorkflowDefinitionId")!;
            var activityId = dataMap.GetString("ActivityId")!;

            await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(workflowDefinitionId, activityId), cancellationToken);
        }
    }
}