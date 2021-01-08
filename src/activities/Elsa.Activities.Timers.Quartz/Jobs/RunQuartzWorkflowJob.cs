using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Elsa.Activities.Timers.Quartz.Jobs
{
    public class RunQuartzWorkflowJob : IJob
    {
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IWorkflowQueue _workflowQueue;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger _logger;

        public RunQuartzWorkflowJob(
            IWorkflowRegistry workflowRegistry, 
            IWorkflowInstanceStore workflowInstanceStore, 
            IWorkflowQueue workflowQueue, 
            IDistributedLockProvider distributedLockProvider,
            ILogger<RunQuartzWorkflowJob> logger)
        {
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _workflowQueue = workflowQueue;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var dataMap = context.MergedJobDataMap;
            var cancellationToken = context.CancellationToken;
            var workflowInstanceId = dataMap.GetString("WorkflowInstanceId");
            var tenantId = dataMap.GetString("TenantId");
            var workflowDefinitionId = dataMap.GetString("WorkflowDefinitionId")!;
            var activityId = dataMap.GetString("ActivityId")!;
            var lockKey = (workflowInstanceId, workflowDefinitionId, activityId).GetHashCode().ToString();

            if (!await _distributedLockProvider.AcquireLockAsync(lockKey, cancellationToken))
            {
                _logger.LogDebug("Failed to acquire lock on {WorkflowInstanceId} / {WorkflowDefinitionId} / {ActivityId}", workflowInstanceId, workflowDefinitionId, activityId);
                return;
            }

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