using Elsa.Common.DistributedHosting;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.ActivationValidators;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.ActivationValidators;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class WorkflowActivationGate(
    IWorkflowActivationStrategyEvaluator evaluator,
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions,
    ITenantAccessor tenantAccessor) : IWorkflowActivationGate
{
    /// <inheritdoc />
    public async Task<WorkflowActivationLease> EvaluateAsync(Workflow workflow, string? correlationId, CancellationToken cancellationToken = default)
    {
        var lockKey = GetLockKey(workflow, correlationId);
        IDistributedSynchronizationHandle? lockHandle = null;

        if (lockKey != null)
            lockHandle = await distributedLockProvider.AcquireLockAsync(lockKey, distributedLockingOptions.Value.LockAcquisitionTimeout, cancellationToken);

        try
        {
            var canStart = await evaluator.CanStartWorkflowAsync(new()
            {
                Workflow = workflow,
                CorrelationId = correlationId,
                CancellationToken = cancellationToken
            });

            if (!canStart)
            {
                if (lockHandle != null)
                    await lockHandle.DisposeAsync();

                return WorkflowActivationLease.Denied;
            }

            return new(true, lockHandle);
        }
        catch
        {
            if (lockHandle != null)
                await lockHandle.DisposeAsync();

            throw;
        }
    }

    private string? GetLockKey(Workflow workflow, string? correlationId)
    {
        var strategyType = workflow.Options.ActivationStrategyType;

        if (strategyType == null || strategyType == typeof(AllowAlwaysStrategy))
            return null;

        var tenantId = tenantAccessor.TenantId;
        var definitionId = workflow.Identity.DefinitionId;

        if (strategyType == typeof(SingletonStrategy))
            return $"workflow-activation:{tenantId}:singleton:{definitionId}";

        if (strategyType == typeof(CorrelatedSingletonStrategy))
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                return null;

            return $"workflow-activation:{tenantId}:correlated-singleton:{definitionId}:{correlationId}";
        }

        if (strategyType == typeof(CorrelationStrategy))
        {
            if (string.IsNullOrWhiteSpace(correlationId))
                return null;

            return $"workflow-activation:{tenantId}:correlation:{correlationId}";
        }

        var correlationKey = string.IsNullOrWhiteSpace(correlationId) ? "_" : correlationId;
        return $"workflow-activation:{tenantId}:custom:{strategyType.FullName}:{definitionId}:{correlationKey}";
    }
}
