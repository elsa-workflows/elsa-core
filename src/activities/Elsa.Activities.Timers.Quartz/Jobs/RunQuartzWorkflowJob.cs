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
        private readonly IWorkflowQueue _workflowQueue;

        public RunQuartzWorkflowJob(IWorkflowRunner workflowRunner, IWorkflowRegistry workflowRegistry, IWorkflowInstanceStore workflowInstanceStore, IWorkflowQueue workflowQueue)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowQueue = workflowQueue;
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
                if (workflowBlueprint.IsSingleton || await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId) == false)
                    await _workflowQueue.EnqueueWorkflowDefinition(workflowDefinitionId, tenantId, activityId, null, null, null, cancellationToken);
            }
            else
            {
                await _workflowQueue.EnqueueWorkflowInstance(workflowInstanceId, activityId, null, cancellationToken);
            }
        }

        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId)
        {
            var specification = new TenantSpecification<WorkflowInstance>(tenantId).WithWorkflowDefinition(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification());
            return await _workflowInstanceStore.FindAsync(specification) != null;
        }
    }
}