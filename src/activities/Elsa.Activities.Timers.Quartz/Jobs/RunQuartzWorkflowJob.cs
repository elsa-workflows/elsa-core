using System.Threading.Tasks;
using Elsa.Activities.Timers.Quartz.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Quartz;

namespace Elsa.Activities.Timers.Quartz.Jobs
{
    public class RunQuartzWorkflowJob : IJob
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly WorkflowRunnerQueue _workflowRunnerQueue;

        public RunQuartzWorkflowJob(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, WorkflowRunnerQueue workflowRunnerQueue)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowRunnerQueue = workflowRunnerQueue;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var tenantId = dataMap.GetString("TenantId");
            var workflowDefinitionId = dataMap.GetString("WorkflowDefinitionId")!;
            var workflowInstanceId = dataMap.GetString("WorkflowInstanceId");
            var activityId = dataMap.GetString("ActivityId")!;
            var cancellationToken = context.CancellationToken;
            var workflowBlueprint = (await _workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken))!;

            if (workflowInstanceId == null)
            {
                if (workflowBlueprint.IsSingleton)
                {
                    var hasExecutingWorkflow = (await _workflowInstanceStore.FindAsync(new WorkflowIsAlreadyExecutingSpecification().WithTenant(tenantId).WithWorkflowDefinition(workflowDefinitionId), cancellationToken)) != null;

                    if (hasExecutingWorkflow)
                        return;
                }

                await _workflowRunner.RunWorkflowAsync(workflowBlueprint, activityId, cancellationToken: cancellationToken);
            }
            else
            {
                await _workflowRunnerQueue.Enqueue(workflowInstanceId, activityId, cancellationToken);
            }
        }
    }
}