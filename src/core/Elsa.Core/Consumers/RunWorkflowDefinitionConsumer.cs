using System.Threading.Tasks;
using Elsa.DistributedLock;
using Elsa.Messages;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Consumers
{
    public class RunWorkflowDefinitionConsumer : IHandleMessages<RunWorkflowDefinition>
    {
        private readonly IWorkflowRunner _workflowRunner;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ILogger _logger;
        private IWorkflowFactory _workflowFactory;

        public RunWorkflowDefinitionConsumer(
            IWorkflowRunner workflowRunner,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IDistributedLockProvider distributedLockProvider,
            IWorkflowFactory workflowFactory,
            ILogger<RunWorkflowDefinitionConsumer> logger)
        {
            _workflowRunner = workflowRunner;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _logger = logger;
            _workflowFactory = workflowFactory;
        }

        public async Task Handle(RunWorkflowDefinition message)
        {
            var workflowDefinitionId = message.WorkflowDefinitionId;
            var tenantId = message.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published);

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return;

            var correlationId = message.CorrelationId;

            if (!string.IsNullOrWhiteSpace(message.CorrelationId))
            {
                var lockKey = $"{nameof(RunWorkflowDefinitionConsumer)}:workflow-definition-{message.WorkflowDefinitionId}:correlation-{message.CorrelationId}";

                if (!await _distributedLockProvider.AcquireLockAsync(lockKey))
                {
                    _logger.LogDebug("Lock {LockKey} already taken", lockKey);
                    return;
                }

                try
                {
                    var correlatedWorkflowInstanceCount = await _workflowInstanceStore.CountAsync(new WorkflowDefinitionIdSpecification(workflowDefinitionId).And(new CorrelationIdSpecification<WorkflowInstance>(correlationId)));

                    if (correlatedWorkflowInstanceCount > 0)
                    {
                        // Do not create a new workflow instance.
                        _logger.LogWarning("There's already a workflow with correlation ID '{CorrelationId}'", correlationId);
                        return;
                    }

                    _logger.LogDebug("No existing workflows found with correlation ID '{CorrelationId}'. Starting new workflow", correlationId);

                    // Persist workflow immediately before leaving the lock to prevent scenarios where the workflow takes a long time before the first time it gets persisted, while a new message comes in with the same workflow and correlation.
                    var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint!, correlationId, message.ContextId);
                    await _workflowInstanceStore.SaveAsync(workflowInstance);
                    
                    // Run the workflow instance.
                    await _workflowRunner.RunWorkflowAsync(workflowBlueprint!, workflowInstance, message.ActivityId, message.Input);
                    
                    return;
                }
                finally
                {
                    await _distributedLockProvider.ReleaseLockAsync(lockKey);
                }
            }

            await _workflowRunner.RunWorkflowAsync(workflowBlueprint!, message.ActivityId, message.Input, message.CorrelationId, message.ContextId);
        }

        private bool ValidatePreconditions(string? workflowDefinitionId, IWorkflowBlueprint? workflowBlueprint)
        {
            if (workflowBlueprint == null)
            {
                _logger.LogError("Could not run workflow with ID {WorkflowDefinitionId} because it does not exist", workflowDefinitionId);
                return false;
            }

            return true;
        }
    }
}