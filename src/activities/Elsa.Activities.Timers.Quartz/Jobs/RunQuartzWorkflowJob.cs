using System.Diagnostics;
using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
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
        private readonly Stopwatch _stopwatch = new();

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

            _logger.LogDebug("Acquiring lock on {WorkflowInstanceId} / {WorkflowDefinitionId} / {ActivityId}", workflowInstanceId, workflowDefinitionId, activityId);
            
            if (!await _distributedLockProvider.AcquireLockAsync(lockKey, cancellationToken))
            {
                _logger.LogDebug("Failed to acquire lock on {WorkflowInstanceId} / {WorkflowDefinitionId} / {ActivityId}", workflowInstanceId, workflowDefinitionId, activityId);
                return;
            }

            _stopwatch.Restart();
            
            try
            {
                if (workflowInstanceId == null)
                {
                    var workflowBlueprint = (await _workflowRegistry.GetWorkflowAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken))!;
                    
                    if (!workflowBlueprint.IsSingleton || await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId) == false)
                        await _workflowQueue.EnqueueWorkflowDefinition(workflowDefinitionId, tenantId, activityId, null, null, null, cancellationToken);
                }
                else
                {
                    await _workflowQueue.EnqueueWorkflowInstance(workflowInstanceId, activityId, null, cancellationToken);
                }
            }
            finally
            {
                _stopwatch.Stop();
                await _distributedLockProvider.ReleaseLockAsync(lockKey, cancellationToken);
                _logger.LogDebug("Held lock on {WorkflowInstanceId} / {WorkflowDefinitionId} / {ActivityId} for {LockTime}", workflowInstanceId, workflowDefinitionId, activityId, _stopwatch.Elapsed);
            }
        }

        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId)
        {
            var specification = new TenantSpecification<WorkflowInstance>(tenantId).WithWorkflowDefinition(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification());
            return await _workflowInstanceStore.FindAsync(specification) != null;
        }
    }
}