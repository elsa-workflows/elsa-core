using System;
using System.Threading.Tasks;
using Elsa.Activities.Temporal.Common.Options;
using Elsa.Dispatch;
using Elsa.Services;
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
            var clusterModeString = dataMap.GetString("ClusterMode");
            var clusterMode = !string.IsNullOrEmpty(clusterModeString) ? Enum.Parse<ClusterMode>(clusterModeString) : ClusterMode.SingleNode;

            if (clusterMode == ClusterMode.MultiNode)
            {
                await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(workflowDefinitionId, activityId), cancellationToken);
            }
            else
            {
                var resource = $"{nameof(RunQuartzWorkflowDefinitionJob)}:{workflowDefinitionId}";
                await using var @lock = await _distributedLockProvider.AcquireLockAsync(resource, cancellationToken: cancellationToken);

                if (@lock == null)
                    return;

                await _workflowDefinitionDispatcher.DispatchAsync(new ExecuteWorkflowDefinitionRequest(workflowDefinitionId, activityId), cancellationToken);
            }
        }
    }
}