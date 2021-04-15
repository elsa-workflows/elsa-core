using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Exceptions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Medallion.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using IDistributedLockProvider = Elsa.Services.IDistributedLockProvider;

namespace Elsa.Dispatch.Handlers
{
    public class ExecuteWorkflowDefinition : IRequestHandler<ExecuteWorkflowDefinitionRequest>
    {
        private readonly IStartsWorkflow _startsWorkflow;
        private readonly IWorkflowRegistry _workflowRegistry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly IDistributedLockProvider _distributedLockProvider;
        private readonly ElsaOptions _elsaOptions;
        private readonly ILogger _logger;

        public ExecuteWorkflowDefinition(
            IStartsWorkflow startsWorkflow,
            IWorkflowRegistry workflowRegistry,
            IWorkflowInstanceStore workflowInstanceStore,
            IDistributedLockProvider distributedLockProvider,
            ElsaOptions elsaOptions,
            ILogger<ExecuteWorkflowDefinition> logger)
        {
            _startsWorkflow = startsWorkflow;
            _workflowRegistry = workflowRegistry;
            _workflowInstanceStore = workflowInstanceStore;
            _distributedLockProvider = distributedLockProvider;
            _elsaOptions = elsaOptions;
            _logger = logger;
        }

        public async Task<Unit> Handle(ExecuteWorkflowDefinitionRequest request, CancellationToken cancellationToken)
        {
            var workflowDefinitionId = request.WorkflowDefinitionId;
            var tenantId = request.TenantId;
            var workflowBlueprint = await _workflowRegistry.GetAsync(workflowDefinitionId, tenantId, VersionOptions.Published, cancellationToken);

            if (!ValidatePreconditions(workflowDefinitionId, workflowBlueprint))
                return Unit.Value;

            var lockKey = $"execute-workflow-definition:tenant:{tenantId}:workflow-definition:{workflowDefinitionId}";
            var correlationId = request.CorrelationId;
            var correlationLockHandle = default(IDistributedSynchronizationHandle?);

            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                _logger.LogDebug("Acquiring lock on correlation ID {CorrelationId}", correlationId);
                correlationLockHandle = await _distributedLockProvider.AcquireLockAsync(correlationId, _elsaOptions.DistributedLockTimeout, cancellationToken);

                if (correlationLockHandle == null)
                    throw new LockAcquisitionException($"Failed to acquire a lock on correlation ID {correlationId}");
            }

            try
            {
                await using var handle = await _distributedLockProvider.AcquireLockAsync(lockKey, _elsaOptions.DistributedLockTimeout, cancellationToken);

                if (handle == null)
                    throw new LockAcquisitionException($"Failed to acquire a lock on {lockKey}");

                if (!workflowBlueprint!.IsSingleton || await GetWorkflowIsAlreadyExecutingAsync(tenantId, workflowDefinitionId) == false)
                    await _startsWorkflow.StartWorkflowAsync(workflowBlueprint, request.ActivityId, request.Input, request.CorrelationId, request.ContextId, cancellationToken);
            }
            finally
            {
                if (correlationLockHandle != null)
                    await correlationLockHandle.DisposeAsync();
            }

            return Unit.Value;
        }

        private bool ValidatePreconditions(string? workflowDefinitionId, IWorkflowBlueprint? workflowBlueprint)
        {
            if (workflowBlueprint != null)
                return true;

            _logger.LogWarning("No workflow definition {WorkflowDefinitionId} found. Make sure the scheduled workflow definition is published and enabled", workflowDefinitionId);
            return false;
        }

        private async Task<bool> GetWorkflowIsAlreadyExecutingAsync(string? tenantId, string workflowDefinitionId)
        {
            var specification = new TenantSpecification<WorkflowInstance>(tenantId).WithWorkflowDefinition(workflowDefinitionId).And(new WorkflowIsAlreadyExecutingSpecification());
            return await _workflowInstanceStore.FindAsync(specification) != null;
        }
    }
}